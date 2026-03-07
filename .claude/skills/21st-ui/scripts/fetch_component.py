#!/usr/bin/env python3
"""
21st.dev Component Browser & SVGL Logo Search

Usage:
    python3 fetch_component.py inspect <author> <component>   # Inspect component metadata
    python3 fetch_component.py code <author> <component>      # Get full component source code
    python3 fetch_component.py install-cmd <author> <component> # Print install command
    python3 fetch_component.py logo <search-query>            # Search SVGL logos
    python3 fetch_component.py logo-svg <filename>            # Get raw SVG code for a logo
    python3 fetch_component.py logo-categories                # List all SVGL categories
    python3 fetch_component.py batch <author> <comp1,comp2>   # Inspect multiple components

Examples:
    python3 fetch_component.py inspect shadcn button
    python3 fetch_component.py code magicui bento-grid
    python3 fetch_component.py logo stripe
    python3 fetch_component.py logo-svg stripe.svg
    python3 fetch_component.py batch shadcn button,card,dialog,input
"""

import json
import sys
import urllib.request
import urllib.error
import urllib.parse
import textwrap

REGISTRY_BASE = "https://21st.dev/r"
SVGL_BASE = "https://api.svgl.app"


def fetch_json(url: str) -> dict | list | None:
    """Fetch JSON from a URL, return parsed data or None on error."""
    try:
        req = urllib.request.Request(url, headers={"User-Agent": "21st-ui-skill/1.0"})
        with urllib.request.urlopen(req, timeout=15) as resp:
            return json.loads(resp.read().decode("utf-8"))
    except urllib.error.HTTPError as e:
        print(f"❌ HTTP {e.code}: {url}")
        if e.code == 404:
            print("   Component not found. Check author/slug (case-sensitive, kebab-case).")
        return None
    except Exception as e:
        print(f"❌ Error fetching {url}: {e}")
        return None


def fetch_text(url: str) -> str | None:
    """Fetch raw text from a URL."""
    try:
        req = urllib.request.Request(url, headers={"User-Agent": "21st-ui-skill/1.0"})
        with urllib.request.urlopen(req, timeout=15) as resp:
            return resp.read().decode("utf-8")
    except Exception as e:
        print(f"❌ Error fetching {url}: {e}")
        return None


def inspect_component(author: str, slug: str) -> None:
    """Fetch and display component metadata."""
    url = f"{REGISTRY_BASE}/{author}/{slug}"
    data = fetch_json(url)
    if not data:
        return

    print(f"📦 Component: {data.get('name', slug)}")
    print(f"   Author: {author}")
    print(f"   Type: {data.get('type', 'unknown')}")
    print(f"   Registry URL: {url}")
    print(f"   Install: npx shadcn@latest add \"{url}\"")

    deps = data.get("dependencies", [])
    if deps:
        print(f"\n   📚 NPM Dependencies: {', '.join(deps)}")

    reg_deps = data.get("registryDependencies", [])
    if reg_deps:
        print(f"   🔗 Registry Dependencies: {', '.join(reg_deps)}")

    css_vars = data.get("cssVars", {})
    if css_vars:
        theme_vars = css_vars.get("theme", {})
        if theme_vars:
            print(f"   🎨 CSS Variables: {', '.join(theme_vars.keys())}")

    files = data.get("files", [])
    if files:
        print(f"\n   📄 Files ({len(files)}):")
        for f in files:
            path = f.get("path", "unknown")
            ftype = f.get("type", "")
            content = f.get("content", "")
            lines = content.count("\n") + 1 if content else 0
            print(f"      {path} ({ftype}, ~{lines} lines)")

    # Show first 15 lines of the main file as preview
    if files and files[0].get("content"):
        content = files[0]["content"]
        preview_lines = content.split("\n")[:15]
        print(f"\n   📝 Code Preview ({files[0].get('path', '')}):")
        for line in preview_lines:
            print(f"      {line}")
        if content.count("\n") > 15:
            print(f"      ... ({content.count(chr(10)) + 1} lines total)")


def show_full_code(author: str, slug: str) -> None:
    """Fetch and display the full component source code."""
    url = f"{REGISTRY_BASE}/{author}/{slug}"
    data = fetch_json(url)
    if not data:
        return

    files = data.get("files", [])
    if not files:
        print("❌ No files found in component.")
        return

    for f in files:
        path = f.get("path", "unknown")
        content = f.get("content", "")
        ftype = f.get("type", "")
        print(f"\n{'='*70}")
        print(f"📄 {path} ({ftype})")
        print(f"{'='*70}")
        print(content)

    # Also show install info
    print(f"\n{'='*70}")
    print(f"📦 Install command:")
    print(f"   npx shadcn@latest add \"{url}\"")
    deps = data.get("dependencies", [])
    if deps:
        print(f"\n   NPM deps (auto-installed): {', '.join(deps)}")
    reg_deps = data.get("registryDependencies", [])
    if reg_deps:
        print(f"   Registry deps (auto-resolved): {', '.join(reg_deps)}")


def print_install_cmd(author: str, slug: str) -> None:
    """Print the install command."""
    url = f"{REGISTRY_BASE}/{author}/{slug}"
    print(f'npx shadcn@latest add "{url}"')


def search_logos(query: str) -> None:
    """Search SVGL for logos matching a query."""
    encoded = urllib.parse.quote(query)
    url = f"{SVGL_BASE}?search={encoded}"
    data = fetch_json(url)
    if not data:
        return

    if not data:
        print(f"🔍 No logos found for '{query}'")
        return

    print(f"🔍 SVGL Logo Search: '{query}' ({len(data)} results)\n")
    for item in data[:15]:  # Limit to 15 results
        title = item.get("title", "?")
        cat = item.get("category", "?")
        if isinstance(cat, list):
            cat = ", ".join(cat)
        route = item.get("route", "")
        url_link = item.get("url", "")
        wordmark = item.get("wordmark")
        brand_url = item.get("brandUrl")

        print(f"   🏷️  {title} [{cat}]")
        if isinstance(route, dict):
            print(f"      Light: {route.get('light', '')}")
            print(f"      Dark:  {route.get('dark', '')}")
        else:
            print(f"      SVG:   {route}")
        if wordmark:
            if isinstance(wordmark, dict):
                print(f"      Wordmark light: {wordmark.get('light', '')}")
                print(f"      Wordmark dark:  {wordmark.get('dark', '')}")
            else:
                print(f"      Wordmark: {wordmark}")
        if brand_url:
            print(f"      Brand guidelines: {brand_url}")
        if url_link:
            print(f"      Website: {url_link}")
        print()


def get_logo_svg(filename: str) -> None:
    """Fetch raw SVG code for a specific logo file."""
    url = f"{SVGL_BASE}/svg/{filename}"
    svg = fetch_text(url)
    if svg:
        print(svg)


def list_categories() -> None:
    """List all SVGL categories."""
    url = f"{SVGL_BASE}/categories"
    data = fetch_json(url)
    if not data:
        return

    print(f"📂 SVGL Categories ({len(data)} total):\n")
    # Sort by count descending
    sorted_cats = sorted(data, key=lambda x: x.get("total", 0), reverse=True)
    for cat in sorted_cats:
        name = cat.get("category", "?")
        total = cat.get("total", 0)
        print(f"   {name}: {total} logos")


def batch_inspect(author: str, components: str) -> None:
    """Inspect multiple components from the same author."""
    slugs = [s.strip() for s in components.split(",") if s.strip()]
    for slug in slugs:
        print(f"\n{'─'*60}")
        inspect_component(author, slug)


def main():
    if len(sys.argv) < 2:
        print(__doc__)
        sys.exit(1)

    cmd = sys.argv[1]

    if cmd == "inspect" and len(sys.argv) >= 4:
        inspect_component(sys.argv[2], sys.argv[3])
    elif cmd == "code" and len(sys.argv) >= 4:
        show_full_code(sys.argv[2], sys.argv[3])
    elif cmd == "install-cmd" and len(sys.argv) >= 4:
        print_install_cmd(sys.argv[2], sys.argv[3])
    elif cmd == "logo" and len(sys.argv) >= 3:
        search_logos(" ".join(sys.argv[2:]))
    elif cmd == "logo-svg" and len(sys.argv) >= 3:
        get_logo_svg(sys.argv[2])
    elif cmd == "logo-categories":
        list_categories()
    elif cmd == "batch" and len(sys.argv) >= 4:
        batch_inspect(sys.argv[2], sys.argv[3])
    else:
        print(__doc__)
        sys.exit(1)


if __name__ == "__main__":
    main()
