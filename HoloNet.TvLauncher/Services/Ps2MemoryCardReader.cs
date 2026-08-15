using System.IO;
using System.Text;

namespace HoloNet.TvLauncher.Services;

/// <summary>
/// Minimal read-only parser for PS2 memory card images (the raw <c>.ps2</c> files PCSX2 uses,
/// e.g. <c>Mcd001.ps2</c>). Understands just enough of the FAT-like on-card file system —
/// superblock, indirect FAT, directories, and file data — to locate a single named file inside
/// a single named save directory and return its raw bytes.
///
/// This intentionally does not implement ECC verification/correction, writing, or any other
/// mutation: it is used purely to read a handful of bytes out of an existing save file for
/// display purposes (see <see cref="SaveStatsService"/>), where an occasional single-bit read
/// glitch is an acceptable risk and nowhere near worth the complexity of porting the full
/// Hamming-code ECC logic that tools like mymc/mymcplus implement.
/// </summary>
public static class Ps2MemoryCardReader
{
    private const uint FatChainEnd = 0xFFFFFFFF;
    private const uint FatClusterMask = 0x7FFFFFFF;
    private static readonly byte[] Magic = Encoding.ASCII.GetBytes("Sony PS2 Memory Card Format ");

    /// <summary>
    /// Reads the raw bytes of <paramref name="fileName"/> inside the save directory
    /// <paramref name="saveDirectoryName"/> on the memory card image at <paramref name="memoryCardPath"/>.
    /// Returns <c>null</c> if the card, directory, or file can't be found/parsed.
    /// </summary>
    public static byte[]? ReadFile(string memoryCardPath, string saveDirectoryName, string fileName) =>
        ReadFileWithMetadata(memoryCardPath, saveDirectoryName, fileName)?.Data;

    /// <summary>
    /// Like <see cref="ReadFile"/>, but also returns the file's on-card "modified" timestamp
    /// (the save file's own directory entry, updated by the game every time it writes the save —
    /// i.e. the PS2-equivalent of "last played"). Returns <c>null</c> under the same conditions
    /// as <see cref="ReadFile"/>.
    /// </summary>
    public static (byte[] Data, DateTime? Modified)? ReadFileWithMetadata(string memoryCardPath,
        string saveDirectoryName, string fileName)
    {
        if (!File.Exists(memoryCardPath))
            return null;

        using var stream = new FileStream(memoryCardPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new BinaryReader(stream);

        var card = ReadSuperblock(reader);
        if (card is null)
            return null;

        // The root directory's own "." entry conveniently holds its own entry count, but a
        // subdirectory's "." entry never does (it's always 0) — a directory's entry count is
        // only recorded in its *parent's* directory entry (the "Length" field of the entry that
        // points at it), so it has to be threaded through explicitly rather than re-derived
        // from each directory's own first cluster.
        var rootEntries = ReadDirectoryEntries(reader, card, card.RootDirCluster, entryCountOverride: null);
        var saveDirEntry = rootEntries.FirstOrDefault(e =>
            e.IsDirectory && string.Equals(e.Name, saveDirectoryName, StringComparison.OrdinalIgnoreCase));
        if (saveDirEntry is null)
            return null;

        var saveEntries = ReadDirectoryEntries(reader, card, saveDirEntry.Cluster, saveDirEntry.Length);
        var fileEntry = saveEntries.FirstOrDefault(e =>
            e.IsFile && string.Equals(e.Name, fileName, StringComparison.OrdinalIgnoreCase));
        if (fileEntry is null)
            return null;

        var data = ReadFileData(reader, card, fileEntry.Cluster, fileEntry.Length);
        return (data, fileEntry.Modified);
    }

    private sealed class CardInfo
    {
        public int PageSize;
        public int SpareSize;
        public int RawPageSize;
        public int PagesPerCluster;
        public int ClusterSize;
        public int EntriesPerCluster;
        public uint AllocOffset;
        public uint RootDirCluster;
        public uint[] IndirectFatClusterList = [];
    }

    private sealed class DirEntry
    {
        public ushort Mode;
        public uint Length;
        public uint Cluster;
        public string Name = string.Empty;
        public DateTime? Modified;

        // Directory Entry Mode Flags, see ps2mc_dir.py: DF_DIR=0x0020, DF_FILE=0x0010, DF_EXISTS=0x8000.
        public bool IsDirectory => (Mode & 0x8020) == 0x8020;
        public bool IsFile => (Mode & 0x8010) == 0x8010;
    }

    private static CardInfo? ReadSuperblock(BinaryReader reader)
    {
        reader.BaseStream.Seek(0, SeekOrigin.Begin);
        var header = reader.ReadBytes(0x154);
        if (header.Length != 0x154 || !header.AsSpan(0, Magic.Length).SequenceEqual(Magic))
            return null;

        var pageSize = BitConverter.ToUInt16(header, 0x28);
        var pagesPerCluster = BitConverter.ToUInt16(header, 0x2A);
        var allocOffset = BitConverter.ToUInt32(header, 0x34);
        var rootDirCluster = BitConverter.ToUInt32(header, 0x3C);

        var ifcList = new uint[32];
        for (var i = 0; i < 32; i++)
            ifcList[i] = BitConverter.ToUInt32(header, 0x50 + i * 4);

        var spareSize = ((pageSize + 127) / 128) * 4;
        var clusterSize = pageSize * pagesPerCluster;

        return new CardInfo
        {
            PageSize = pageSize,
            SpareSize = spareSize,
            RawPageSize = pageSize + spareSize,
            PagesPerCluster = pagesPerCluster,
            ClusterSize = clusterSize,
            EntriesPerCluster = clusterSize / 4,
            AllocOffset = allocOffset,
            RootDirCluster = rootDirCluster,
            IndirectFatClusterList = ifcList
        };
    }

    /// <summary>Reads a raw (non-allocatable-relative) cluster by its absolute cluster number.</summary>
    private static byte[] ReadRawCluster(BinaryReader reader, CardInfo card, uint clusterNumber)
    {
        var result = new byte[card.ClusterSize];
        var offset = 0;
        for (var p = 0; p < card.PagesPerCluster; p++)
        {
            var pageNumber = clusterNumber * card.PagesPerCluster + p;
            reader.BaseStream.Seek((long)pageNumber * card.RawPageSize, SeekOrigin.Begin);
            var page = reader.ReadBytes(card.PageSize);
            Array.Copy(page, 0, result, offset, page.Length);
            offset += card.PageSize;
        }

        return result;
    }

    /// <summary>Reads a cluster from the allocatable (user data) area, relative to <see cref="CardInfo.AllocOffset"/>.</summary>
    private static byte[] ReadAllocatableCluster(BinaryReader reader, CardInfo card, uint relativeClusterNumber) =>
        ReadRawCluster(reader, card, relativeClusterNumber + card.AllocOffset);

    /// <summary>
    /// Follows the double-indirect FAT structure to find the next cluster in the chain that
    /// starts at allocatable-relative cluster <paramref name="clusterNumber"/>. Returns the raw
    /// FAT entry (allocated bit + next cluster index, or <see cref="FatChainEnd"/>).
    /// </summary>
    private static uint LookupFat(BinaryReader reader, CardInfo card, uint clusterNumber)
    {
        var entriesPerCluster = (uint)card.EntriesPerCluster;
        var fatOffset = clusterNumber % entriesPerCluster;
        var fatClusterIndex = clusterNumber / entriesPerCluster;

        var indirectOffset = fatClusterIndex % entriesPerCluster;
        var doubleIndirectOffset = fatClusterIndex / entriesPerCluster;

        var indirectClusterNumber = card.IndirectFatClusterList[doubleIndirectOffset];
        var indirectFat = ReadRawCluster(reader, card, indirectClusterNumber);
        var fatClusterNumber = BitConverter.ToUInt32(indirectFat, (int)indirectOffset * 4);

        var fat = ReadRawCluster(reader, card, fatClusterNumber);
        return BitConverter.ToUInt32(fat, (int)fatOffset * 4);
    }

    /// <summary>Returns the full chain of allocatable-relative cluster numbers starting at <paramref name="firstCluster"/>.</summary>
    private static List<uint> FollowFatChain(BinaryReader reader, CardInfo card, uint firstCluster)
    {
        var chain = new List<uint> { firstCluster };
        var current = firstCluster;

        while (true)
        {
            var fatValue = LookupFat(reader, card, current);
            if (fatValue == FatChainEnd)
                break;

            current = fatValue & FatClusterMask;
            chain.Add(current);

            if (chain.Count > 65536)
                break; // Safety valve against a corrupt/cyclic chain.
        }

        return chain;
    }

    private static DirEntry ParseDirEntry(byte[] cluster, int offsetInCluster)
    {
        var mode = BitConverter.ToUInt16(cluster, offsetInCluster + 0x00);
        var length = BitConverter.ToUInt32(cluster, offsetInCluster + 0x04);
        var clusterNum = BitConverter.ToUInt32(cluster, offsetInCluster + 0x10);
        var modified = ParseTimestamp(cluster, offsetInCluster + 0x18);
        var nameBytes = cluster.AsSpan(offsetInCluster + 0x40, 32);
        var nullIndex = nameBytes.IndexOf((byte)0);
        var name = Encoding.ASCII.GetString(nullIndex >= 0 ? nameBytes[..nullIndex] : nameBytes);

        return new DirEntry { Mode = mode, Length = length, Cluster = clusterNum, Name = name, Modified = modified };
    }

    /// <summary>
    /// Parses an 8-byte PS2 memory card timestamp (byte 0 unused, then sec/min/hour/day/month as
    /// plain binary bytes, then a little-endian u16 year), as used in the "created" (@0x08) and
    /// "modified" (@0x18) fields of a directory entry. The PS2 BIOS always stores these in
    /// Japan Standard Time (UTC+9), regardless of the console/game's own region — confirmed
    /// against mymcplus's own <c>tod_to_time</c>, which subtracts 9 hours to get true UTC. This
    /// converts to true UTC accordingly, so callers can safely call <c>.ToLocalTime()</c> to get
    /// the host PC's local time. Returns <c>null</c> for an all-zero/invalid stamp.
    /// </summary>
    private static DateTime? ParseTimestamp(byte[] cluster, int offset)
    {
        var second = cluster[offset + 1];
        var minute = cluster[offset + 2];
        var hour = cluster[offset + 3];
        var day = cluster[offset + 4];
        var month = cluster[offset + 5];
        var year = BitConverter.ToUInt16(cluster, offset + 6);

        try
        {
            var jst = new DateTime(year, month, day, hour, minute, second, DateTimeKind.Utc);
            return DateTime.SpecifyKind(jst - TimeSpan.FromHours(9), DateTimeKind.Utc);
        }
        catch (ArgumentOutOfRangeException)
        {
            return null;
        }
    }

    private static List<DirEntry> ReadDirectoryEntries(BinaryReader reader, CardInfo card, uint firstCluster,
        uint? entryCountOverride)
    {
        // Only the *root* directory's own "." entry happens to hold its entry count in its
        // "length" field; subdirectories' "." entries always have length 0 there, so for any
        // non-root directory the caller must supply the count from the parent's directory entry
        // instead (see entryCountOverride).
        var firstClusterData = ReadAllocatableCluster(reader, card, firstCluster);
        var entryCount = entryCountOverride ?? BitConverter.ToUInt32(firstClusterData, 0x04);

        var totalBytes = (long)entryCount * 512;
        var clustersNeeded = (int)((totalBytes + card.ClusterSize - 1) / card.ClusterSize);

        var chain = FollowFatChain(reader, card, firstCluster);
        var allBytes = new List<byte>((int)Math.Min(totalBytes, int.MaxValue));
        for (var i = 0; i < clustersNeeded && i < chain.Count; i++)
            allBytes.AddRange(i == 0 ? firstClusterData : ReadAllocatableCluster(reader, card, chain[i]));

        var data = allBytes.ToArray();
        var entries = new List<DirEntry>();
        for (uint i = 0; i < entryCount; i++)
        {
            var offset = (int)(i * 512);
            if (offset + 512 > data.Length)
                break;
            entries.Add(ParseDirEntry(data, offset));
        }

        return entries;
    }

    private static byte[] ReadFileData(BinaryReader reader, CardInfo card, uint firstCluster, uint length)
    {
        var clustersNeeded = (int)((length + card.ClusterSize - 1) / card.ClusterSize);
        var chain = FollowFatChain(reader, card, firstCluster);

        var buffer = new List<byte>((int)length);
        for (var i = 0; i < clustersNeeded && i < chain.Count; i++)
            buffer.AddRange(ReadAllocatableCluster(reader, card, chain[i]));

        var result = new byte[length];
        Array.Copy(buffer.ToArray(), result, Math.Min(length, buffer.Count));
        return result;
    }
}
