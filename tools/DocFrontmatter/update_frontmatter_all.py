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
    doc_root = repo_root / "doc"
    return sorted(p for p in doc_root.rglob("*.md") if p.is_file())


def main(argv: list[str]) -> int:
    parser = argparse.ArgumentParser(description="Apply standard frontmatter to all doc/*.md files.")
    parser.add_argument(
        "--title",
        default="auto",
        choices=["auto", "from-h1", "from-filename", "none"],
        help="How to infer the 'title' property (default: auto).",
    )
    parser.add_argument("--dry-run", action="store_true", help="Show what would change without writing files.")
    args = parser.parse_args(argv)

    paths = _gather_doc_markdown_files()
    processed, changed = apply(
        paths,
        title_mode=args.title,
        set_values={},
        unset_keys=set(),
        dry_run=args.dry_run,
    )

    suffix = " (dry-run)" if args.dry_run else ""
    print(f"frontmatter-all: processed={processed} changed={changed}{suffix}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
