#!/usr/bin/env python3
"""
Minimal Markdown frontmatter updater.

Why this exists:
- A static site generator typically needs YAML frontmatter.
- We want to mass-add/update frontmatter across many docs without manual edits.
- Keep it dependency-free (no PyYAML).

Supported frontmatter syntax (subset):
---
key: value
---

Values are written as YAML scalars (bool/int/float/string). For simplicity, this tool focuses on the
common documentation use-cases (title, tags, sidebar metadata, ...).
"""

from __future__ import annotations

import argparse
import dataclasses
import re
import sys
from pathlib import Path
from typing import Any, Iterable


_BOM = "\ufeff"


@dataclasses.dataclass(frozen=True)
class MarkdownDocument:
    path: Path
    newline: str
    bom: str
    frontmatter: dict[str, Any]
    body: str
    had_frontmatter: bool


def _detect_newline(text: str) -> str:
    if "\r\n" in text:
        return "\r\n"
    return "\n"


def _normalize_newlines(text: str) -> str:
    return text.replace("\r\n", "\n")


def _split_frontmatter(normalized_text: str) -> tuple[dict[str, Any], str, bool]:
    if not normalized_text.startswith("---\n"):
        return {}, normalized_text, False

    lines = normalized_text.split("\n")
    if len(lines) < 2:
        return {}, normalized_text, False

    # Find the closing --- on its own line.
    end_index = None
    for i in range(1, len(lines)):
        if lines[i].strip() == "---":
            end_index = i
            break

    if end_index is None:
        return {}, normalized_text, False

    fm_lines = lines[1:end_index]
    body_lines = lines[end_index + 1 :]
    fm = _parse_frontmatter_lines(fm_lines)
    body = "\n".join(body_lines)
    return fm, body, True


_KEY_VALUE_RE = re.compile(r"^\s*([A-Za-z0-9_.-]+)\s*:\s*(.*?)\s*$")


def _parse_frontmatter_lines(lines: list[str]) -> dict[str, Any]:
    result: dict[str, Any] = {}
    for raw in lines:
        line = raw.strip()
        if not line or line.startswith("#"):
            continue

        m = _KEY_VALUE_RE.match(raw)
        if not m:
            # Keep parsing permissive: ignore lines we don't understand.
            continue

        key = m.group(1)
        value_text = m.group(2)
        result[key] = _parse_scalar(value_text)
    return result


def _parse_scalar(value_text: str) -> Any:
    if value_text == "":
        return ""

    lower = value_text.lower()
    if lower == "true":
        return True
    if lower == "false":
        return False
    if lower in ("null", "~"):
        return None

    if re.fullmatch(r"-?\d+", value_text):
        try:
            return int(value_text)
        except ValueError:
            return value_text

    if re.fullmatch(r"-?\d+\.\d+", value_text):
        try:
            return float(value_text)
        except ValueError:
            return value_text

    # Strip simple single/double quotes.
    if (value_text.startswith('"') and value_text.endswith('"')) or (value_text.startswith("'") and value_text.endswith("'")):
        return value_text[1:-1]

    return value_text


def _is_simple_unquoted_yaml_string(value: str) -> bool:
    # Conservative: avoid YAML syntax characters that can change meaning.
    if value == "":
        return False
    if value[0] in "-?:,[]{}#&*!|>'\"%@`":
        return False
    if any(ch in value for ch in ":#[]{}&*!|>'\"%@`"):
        return False
    return re.fullmatch(r"[A-Za-z0-9][A-Za-z0-9 _./()+-]*", value) is not None


def _format_scalar(value: Any) -> str:
    if value is True:
        return "true"
    if value is False:
        return "false"
    if value is None:
        return "null"
    if isinstance(value, (int, float)):
        return str(value)

    text = str(value)
    if _is_simple_unquoted_yaml_string(text):
        return text

    escaped = text.replace("\\", "\\\\").replace('"', '\\"')
    return f"\"{escaped}\""


def _serialize_frontmatter(frontmatter: dict[str, Any]) -> str:
    if not frontmatter:
        return ""

    # Keep output stable: title first, then alphabetic.
    keys = list(frontmatter.keys())
    keys_sorted = sorted((k for k in keys if k != "title"))
    if "title" in frontmatter:
        keys_sorted.insert(0, "title")

    lines: list[str] = ["---"]
    for key in keys_sorted:
        lines.append(f"{key}: {_format_scalar(frontmatter[key])}")
    lines.append("---")
    return "\n".join(lines)


_H1_RE = re.compile(r"^\s*#\s+(.*?)\s*$")


def infer_title_from_h1(normalized_body: str) -> str | None:
    for line in normalized_body.split("\n"):
        if not line.strip():
            continue

        m = _H1_RE.match(line)
        if not m:
            continue

        title = m.group(1).strip()
        title = title.strip("#").strip()
        return title or None

    return None


_ACRONYMS = {
    "ui": "UI",
    "api": "API",
    "fps": "FPS",
    "cli": "CLI",
    "id": "Id",
    "url": "Url",
    "yaml": "YAML",
    "json": "JSON",
}


def _title_case_from_filename(path: Path) -> str:
    stem = path.stem
    if stem.lower() in ("readme", "index"):
        stem = path.parent.name

    stem = re.sub(r"[_-]+", " ", stem).strip()
    if not stem:
        return "Document"

    words: list[str] = []
    for raw in stem.split():
        key = raw.lower()
        if key in _ACRONYMS:
            words.append(_ACRONYMS[key])
        else:
            words.append(raw[:1].upper() + raw[1:])
    return " ".join(words)


def infer_title(path: Path, normalized_body: str, mode: str) -> str | None:
    mode = mode.lower()
    if mode == "none":
        return None
    if mode == "from-h1":
        return infer_title_from_h1(normalized_body) or _title_case_from_filename(path)
    if mode == "from-filename":
        return _title_case_from_filename(path)
    if mode == "auto":
        return infer_title_from_h1(normalized_body) or _title_case_from_filename(path)
    raise ValueError(f"Unknown title mode: {mode}")


def read_markdown(path: Path) -> MarkdownDocument:
    raw = path.read_text(encoding="utf-8")
    newline = _detect_newline(raw)
    bom = _BOM if raw.startswith(_BOM) else ""
    text = raw[len(bom) :]
    normalized = _normalize_newlines(text)
    frontmatter, body, had = _split_frontmatter(normalized)
    return MarkdownDocument(path=path, newline=newline, bom=bom, frontmatter=frontmatter, body=body, had_frontmatter=had)


def write_markdown(doc: MarkdownDocument, *, frontmatter: dict[str, Any], body: str, dry_run: bool) -> bool:
    normalized_fm = _serialize_frontmatter(frontmatter)
    normalized_body = body.lstrip("\n")

    if normalized_fm:
        normalized_out = f"{normalized_fm}\n\n{normalized_body}"
    else:
        normalized_out = normalized_body

    out = normalized_out.replace("\n", doc.newline)
    out = f"{doc.bom}{out}"

    before = doc.path.read_text(encoding="utf-8")
    changed = before != out
    if changed and not dry_run:
        doc.path.write_text(out, encoding="utf-8")
    return changed


def _parse_set_args(pairs: list[str]) -> dict[str, Any]:
    result: dict[str, Any] = {}
    for pair in pairs:
        if "=" not in pair:
            raise ValueError(f"Invalid --set value '{pair}'. Expected key=value.")
        key, raw_value = pair.split("=", 1)
        key = key.strip()
        if not key:
            raise ValueError(f"Invalid --set value '{pair}'. Key is empty.")
        result[key] = _parse_scalar(raw_value.strip())
    return result


def expand_paths(inputs: list[str]) -> list[Path]:
    paths: list[Path] = []
    for raw in inputs:
        # Accept explicit files or glob patterns.
        if any(ch in raw for ch in "*?[]"):
            for p in sorted(Path().glob(raw)):
                if p.is_file():
                    paths.append(p)
            continue
        p = Path(raw)
        if p.is_file():
            paths.append(p)
            continue
        raise FileNotFoundError(f"Path not found: {raw}")
    return paths


def apply(
    paths: Iterable[Path],
    *,
    title_mode: str,
    force_title: bool,
    set_values: dict[str, Any],
    unset_keys: set[str],
    dry_run: bool,
) -> tuple[int, int]:
    changed = 0
    processed = 0

    for path in paths:
        if path.suffix.lower() != ".md":
            continue

        processed += 1
        doc = read_markdown(path)
        fm = dict(doc.frontmatter)
        did_mutate = False

        inferred_title = infer_title(path, doc.body, title_mode)
        if inferred_title is not None and (force_title or "title" not in fm):
            fm["title"] = inferred_title
            did_mutate = True

        for key, value in set_values.items():
            if key not in fm or fm[key] != value:
                fm[key] = value
                did_mutate = True

        for key in unset_keys:
            if key in fm:
                fm.pop(key, None)
                did_mutate = True

        if not did_mutate:
            continue

        if write_markdown(doc, frontmatter=fm, body=doc.body, dry_run=dry_run):
            changed += 1

    return processed, changed


def main(argv: list[str]) -> int:
    parser = argparse.ArgumentParser(
        description="Add/update YAML frontmatter in Markdown files.",
        epilog=(
            "Examples:\n"
            "  python tools/DocFrontmatter/frontmatter.py site/**/*.md --title auto\n"
            "  python tools/DocFrontmatter/frontmatter.py site/**/*.md --set sidebar=Controls --set draft=false\n"
            "  python tools/DocFrontmatter/frontmatter.py site/**/*.md --unset draft --dry-run\n"
        ),
        formatter_class=argparse.RawTextHelpFormatter,
    )
    parser.add_argument("paths", nargs="+", help="Markdown files and/or glob patterns (e.g. site/docs/**/*.md)")
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
    parser.add_argument("--set", action="append", default=[], help="Set/update a frontmatter key (key=value). Can be repeated.")
    parser.add_argument("--unset", action="append", default=[], help="Remove a frontmatter key. Can be repeated.")
    parser.add_argument("--dry-run", action="store_true", help="Show what would change without writing files.")

    args = parser.parse_args(argv)

    try:
        set_values = _parse_set_args(args.set)
        unset_keys = {k.strip() for k in args.unset if k.strip()}
        paths = expand_paths(args.paths)
        processed, changed = apply(
            paths,
            title_mode=args.title,
            force_title=args.force_title,
            set_values=set_values,
            unset_keys=unset_keys,
            dry_run=args.dry_run,
        )
    except Exception as ex:  # noqa: BLE001 - CLI tool: print a helpful message.
        print(f"error: {ex}", file=sys.stderr)
        return 2

    suffix = " (dry-run)" if args.dry_run else ""
    print(f"frontmatter: processed={processed} changed={changed}{suffix}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
