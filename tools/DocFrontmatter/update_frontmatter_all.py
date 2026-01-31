#!/usr/bin/env python3
"""
Repository helper to apply standard frontmatter to all docs.

Intended usage:
- run once after adding new docs
- or run whenever global frontmatter conventions change
"""

from __future__ import annotations

import argparse
import sys
from pathlib import Path

from frontmatter import apply


def _gather_doc_markdown_files() -> list[Path]:
    repo_root = Path(__file__).resolve().parents[2]
    doc_root = repo_root / "site"
    paths: list[Path] = []
    for p in doc_root.rglob("*.md"):
        if not p.is_file():
            continue

        # Never touch generated content or vendored resource caches.
        rel_parts = p.relative_to(doc_root).parts
        if len(rel_parts) >= 2 and rel_parts[0] == ".lunet" and rel_parts[1] == "build":
            continue
        if p.name.upper() == "AGENTS.MD":
            continue

        paths.append(p)

    return sorted(paths)


def main(argv: list[str]) -> int:
    parser = argparse.ArgumentParser(description="Apply standard frontmatter to all site docs.")
    parser.add_argument(
        "--title",
        default="auto",
        choices=["auto", "from-h1", "from-filename", "none"],
        help="How to infer the 'title' property (default: auto).",
    )
    parser.add_argument(
        "--force-title",
        action="store_true",
        help="Overwrite existing 'title' values (default: only set when missing).",
    )
    parser.add_argument("--dry-run", action="store_true", help="Show what would change without writing files.")
    args = parser.parse_args(argv)

    paths = _gather_doc_markdown_files()
    processed, changed = apply(
        paths,
        title_mode=args.title,
        force_title=args.force_title,
        set_values={},
        unset_keys=set(),
        dry_run=args.dry_run,
    )

    suffix = " (dry-run)" if args.dry_run else ""
    print(f"frontmatter-all: processed={processed} changed={changed}{suffix}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
