"""
One-time utility: extracts text from the SourceMaking Design Patterns /
Refactoring PDFs (downloaded to HoloNet\\practices) into Markdown files
under this skill's reference/ directory, preserving the same folder
structure. Not needed at skill-usage time - just used to (re)build the
reference material.
"""
import re
import sys
from pathlib import Path

from pypdf import PdfReader

SOURCE_ROOT = Path(r"C:\Users\Goa\HoloNet\practices")
DEST_ROOT = Path(__file__).parent / "reference"


def clean_text(text: str) -> str:
    # Collapse repeated blank lines and strip trailing whitespace per line.
    lines = [line.rstrip() for line in text.splitlines()]
    cleaned = []
    blank_run = 0
    for line in lines:
        if line == "":
            blank_run += 1
            if blank_run > 1:
                continue
        else:
            blank_run = 0
        cleaned.append(line)
    return "\n".join(cleaned).strip() + "\n"


def convert_pdf(pdf_path: Path, md_path: Path) -> None:
    reader = PdfReader(str(pdf_path))
    parts = [page.extract_text() or "" for page in reader.pages]
    text = "\n\n".join(parts)
    title = pdf_path.stem
    header = f"# {title}\n\nSource: sourcemaking.com (downloaded copy)\n\n---\n\n"
    md_path.parent.mkdir(parents=True, exist_ok=True)
    md_path.write_text(header + clean_text(text), encoding="utf-8")


def main() -> None:
    pdf_files = sorted(SOURCE_ROOT.rglob("*.pdf"))
    print(f"Found {len(pdf_files)} PDFs under {SOURCE_ROOT}")
    count = 0
    for pdf_path in pdf_files:
        rel = pdf_path.relative_to(SOURCE_ROOT).with_suffix(".md")
        md_path = DEST_ROOT / rel
        try:
            convert_pdf(pdf_path, md_path)
            count += 1
        except Exception as exc:  # noqa: BLE001
            print(f"FAILED: {pdf_path} -> {exc}", file=sys.stderr)
    print(f"Converted {count}/{len(pdf_files)} PDFs to Markdown under {DEST_ROOT}")


if __name__ == "__main__":
    main()
