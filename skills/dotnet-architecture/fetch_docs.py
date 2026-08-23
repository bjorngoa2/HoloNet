"""
One-time utility: fetches the "Architect modern web applications with
ASP.NET Core and Azure" e-book and the "Framework Design Guidelines" from
Microsoft Learn, converts each page's main content to Markdown, and saves it
under this skill's reference/ directory. Not needed at skill-usage time -
just used to (re)build the reference material if Microsoft updates the docs.
"""
import re
import time
from pathlib import Path

import requests
from bs4 import BeautifulSoup
from markdownify import markdownify as md

DEST_ROOT = Path(__file__).parent / "reference"
HEADERS = {"User-Agent": "Mozilla/5.0 (compatible; HoloNet-docs-fetch/1.0)"}

# (base_url, dest_subfolder, [(href, title, [optional sub-folder title])])
MODERN_WEB_APPS_BASE = "https://learn.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/"
DESIGN_GUIDELINES_BASE = "https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/"

MODERN_WEB_APPS_PAGES = [
    ("", "Introduction"),
    ("modern-web-applications-characteristics", "Characteristics of modern web applications"),
    ("choose-between-traditional-web-and-single-page-apps", "Choose between traditional web apps and single page apps"),
    ("architectural-principles", "Architectural principles"),
    ("common-web-application-architectures", "Common web application architectures"),
    ("common-client-side-web-technologies", "Common client side web technologies"),
    ("develop-asp-net-core-mvc-apps", "Develop ASP.NET Core MVC Apps"),
    ("work-with-data-in-asp-net-core-apps", "Work with data in ASP.NET Core"),
    ("test-asp-net-core-mvc-apps", "Test ASP.NET Core MVC Apps"),
    ("development-process-for-azure", "Development process for Azure"),
    ("azure-hosting-recommendations-for-asp-net-web-apps", "Azure hosting recommendations for ASP.NET web apps"),
]

DESIGN_GUIDELINES_PAGES = {
    "": [("", "Overview")],
    "Naming Guidelines": [
        ("naming-guidelines", "Naming guidelines"),
        ("capitalization-conventions", "Capitalization conventions"),
        ("general-naming-conventions", "General naming conventions"),
        ("names-of-assemblies-and-dlls", "Names of assemblies and DLLs"),
        ("names-of-namespaces", "Names of namespaces"),
        ("names-of-classes-structs-and-interfaces", "Names of classes, structs, and interfaces"),
        ("names-of-type-members", "Names of type members"),
        ("naming-parameters", "Naming parameters"),
        ("naming-resources", "Naming resources"),
    ],
    "Type Design Guidelines": [
        ("type", "Type design guidelines"),
        ("choosing-between-class-and-struct", "Choose between class and struct"),
        ("abstract-class", "Abstract class design"),
        ("static-class", "Static class design"),
        ("interface", "Interface design"),
        ("struct", "Struct design"),
        ("enum", "Enum design"),
        ("nested-types", "Nested types"),
    ],
    "Member Design Guidelines": [
        ("member", "Member design guidelines"),
        ("member-overloading", "Member overloading"),
        ("property", "Property design"),
        ("constructor", "Constructor design"),
        ("event", "Event design"),
        ("field", "Field design"),
        ("extension-methods", "Extension methods"),
        ("operator-overloads", "Operator overloads"),
        ("parameter-design", "Parameter design"),
    ],
    "Design for Extensibility": [
        ("designing-for-extensibility", "Design for extensibility"),
        ("unsealed-classes", "Unsealed classes"),
        ("protected-members", "Protected members"),
        ("events-and-callbacks", "Events and callbacks"),
        ("virtual-members", "Virtual members"),
        ("abstractions-abstract-types-and-interfaces", "Abstractions (abstract types and interfaces)"),
        ("base-classes-for-implementing-abstractions", "Base classes for implementing abstractions"),
        ("sealing", "Sealing"),
    ],
    "Exception Design Guidelines": [
        ("exceptions", "Exception design guidelines"),
        ("exception-throwing", "Exception throwing"),
        ("using-standard-exception-types", "Use standard exception types"),
        ("exceptions-and-performance", "Exceptions and performance"),
    ],
    "Usage Guidelines": [
        ("usage-guidelines", "Usage guidelines"),
        ("arrays", "Arrays"),
        ("attributes", "Attributes"),
        ("guidelines-for-collections", "Collections"),
        ("serialization", "Serialization"),
        ("system-xml-usage", "System.Xml usage"),
        ("equality-operators", "Equality operators"),
    ],
    "Common Design Patterns": [
        ("common-design-patterns", "Common design patterns"),
        ("dependency-properties", "Dependency properties"),
        ("dispose-pattern", "Dispose pattern"),
    ],
}


def safe_filename(title: str) -> str:
    return re.sub(r'[<>:"/\\|?*]', "-", title).strip()


def fetch_and_convert(url: str) -> str:
    resp = requests.get(url, headers=HEADERS, timeout=30)
    resp.raise_for_status()
    resp.encoding = "utf-8"
    soup = BeautifulSoup(resp.text, "html.parser")
    content_divs = soup.find_all("div", class_="content")
    if not content_divs:
        raise ValueError(f"No content divs found on {url}")
    html_fragment = "".join(str(d) for d in content_divs)
    markdown = md(html_fragment, heading_style="ATX")
    # Collapse excess blank lines.
    markdown = re.sub(r"\n{3,}", "\n\n", markdown).strip() + "\n"
    return markdown


def save_page(url: str, title: str, dest_path: Path) -> None:
    if dest_path.exists():
        print(f"SKIP (exists): {dest_path}")
        return
    try:
        markdown = fetch_and_convert(url)
    except Exception as exc:  # noqa: BLE001
        print(f"FAILED: {url} -> {exc}")
        return
    header = f"# {title}\n\nSource: {url}\n\n---\n\n"
    dest_path.parent.mkdir(parents=True, exist_ok=True)
    dest_path.write_text(header + markdown, encoding="utf-8")
    print(f"OK: {dest_path}")
    time.sleep(0.5)


def main() -> None:
    base_dir = DEST_ROOT / "Modern Web Apps (ASP.NET Core and Azure)"
    for href, title in MODERN_WEB_APPS_PAGES:
        url = MODERN_WEB_APPS_BASE + href
        dest = base_dir / (safe_filename(title) + ".md")
        save_page(url, title, dest)

    fdg_dir = DEST_ROOT / "Framework Design Guidelines"
    for subfolder, pages in DESIGN_GUIDELINES_PAGES.items():
        folder = fdg_dir / subfolder if subfolder else fdg_dir
        for href, title in pages:
            url = DESIGN_GUIDELINES_BASE + href
            dest = folder / (safe_filename(title) + ".md")
            save_page(url, title, dest)

    print("DONE")


if __name__ == "__main__":
    main()
