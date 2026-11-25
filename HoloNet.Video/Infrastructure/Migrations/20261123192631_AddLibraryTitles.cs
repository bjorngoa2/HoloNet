using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HoloNet.Video.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLibraryTitles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LibraryTitles",
                columns: table => new
                {
                    Provider = table.Column<string>(type: "TEXT", nullable: false),
                    ProviderMediaId = table.Column<string>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    EntityType = table.Column<string>(type: "TEXT", nullable: false),
                    PosterUrl = table.Column<string>(type: "TEXT", nullable: true),
                    AddedUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LibraryTitles", x => new { x.Provider, x.ProviderMediaId });
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LibraryTitles");
        }
    }
}
