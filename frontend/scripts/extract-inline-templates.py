#!/usr/bin/env python3
"""Extract inline Angular templates/styles into .html / .scss files."""
from __future__ import annotations

import re
from pathlib import Path

ROOT = Path(r"c:/Users/Fani/Documents/gestao-PCIRN/SIGAD-IC/SIGAD-IC/frontend/src")


def extract_backtick_block(text: str, start_idx: int) -> tuple[str, int, int]:
    """Given index of opening backtick, return (content, content_start, end_after_close)."""
    assert text[start_idx] == "`"
    i = start_idx + 1
    content_start = i
    while i < len(text):
        ch = text[i]
        if ch == "\\" and i + 1 < len(text):
            i += 2
            continue
        if ch == "`":
            return text[content_start:i], content_start, i + 1
        i += 1
    raise ValueError("Unclosed backtick string")


def find_prop_backtick(text: str, prop: str) -> tuple[int, int, str] | None:
    """Find `prop: \`...\`` or `prop: [\`...\`]` and return (match_start, match_end, content)."""
    # styles: [`...`] or styles: `...` or template: `...` or template: '...'
    patterns = [
        rf"{prop}\s*:\s*\[\s*`",
        rf"{prop}\s*:\s*`",
    ]
    for pat in patterns:
        m = re.search(pat, text)
        if not m:
            continue
        bt = text.find("`", m.start())
        content, _, end = extract_backtick_block(text, bt)
        # consume trailing whitespace and optional ]
        j = end
        while j < len(text) and text[j] in " \t\r\n":
            j += 1
        if j < len(text) and text[j] == "]":
            j += 1
        # trailing comma stays in source for replacement boundary
        return m.start(), j, content
    return None


def find_prop_single_quote(text: str, prop: str) -> tuple[int, int, str] | None:
    m = re.search(rf"{prop}\s*:\s*'", text)
    if not m:
        return None
    start = m.end()
    i = start
    while i < len(text):
        if text[i] == "\\" and i + 1 < len(text):
            i += 2
            continue
        if text[i] == "'":
            return m.start(), i + 1, text[start:i]
        i += 1
    raise ValueError("Unclosed single-quoted template")


def dedent_like(content: str) -> str:
    """Trim outer blank lines; keep internal indentation relative to min indent of non-empty lines."""
    lines = content.replace("\r\n", "\n").split("\n")
    # drop leading/trailing empty lines
    while lines and lines[0].strip() == "":
        lines.pop(0)
    while lines and lines[-1].strip() == "":
        lines.pop()
    if not lines:
        return ""
    indents = [len(l) - len(l.lstrip(" ")) for l in lines if l.strip()]
    min_indent = min(indents) if indents else 0
    return "\n".join(l[min_indent:] if len(l) >= min_indent else l for l in lines) + "\n"


def process_file(path: Path) -> list[str]:
    text = path.read_text(encoding="utf-8")
    original = text
    actions: list[str] = []

    # Determine base name for companion files
    if path.name.endswith(".component.ts"):
        base = path.with_suffix("")  # foo.component
        html_path = Path(str(base) + ".html")
        scss_path = Path(str(base) + ".scss")
    elif path.name == "form-layout.ts":
        html_path = path.with_name("form-section.component.html")
        scss_path = path.with_name("form-section.component.scss")
    elif path.name == "app.component.ts":
        html_path = path.with_name("app.component.html")
        scss_path = path.with_name("app.component.scss")
    else:
        stem = path.stem
        html_path = path.with_name(f"{stem}.component.html")
        scss_path = path.with_name(f"{stem}.component.scss")

    # template
    tpl = find_prop_backtick(text, "template")
    if not tpl:
        tpl_sq = find_prop_single_quote(text, "template")
        if tpl_sq:
            tpl = tpl_sq
    if tpl:
        start, end, content = tpl
        html = dedent_like(content)
        html_path.write_text(html, encoding="utf-8")
        rel = html_path.name
        replacement = f"templateUrl: './{rel}'"
        text = text[:start] + replacement + text[end:]
        actions.append(f"HTML -> {html_path.relative_to(ROOT)}")

    # styles (re-find after template edit)
    sty = find_prop_backtick(text, "styles")
    if sty:
        start, end, content = sty
        scss = dedent_like(content)
        scss_path.write_text(scss, encoding="utf-8")
        rel = scss_path.name
        replacement = f"styleUrl: './{rel}'"
        text = text[:start] + replacement + text[end:]
        actions.append(f"SCSS -> {scss_path.relative_to(ROOT)}")

    if text != original:
        # Clean double commas / trailing commas before } that look odd
        text = re.sub(r",\s*,", ",", text)
        path.write_text(text, encoding="utf-8")
        actions.append(f"updated {path.relative_to(ROOT)}")
    return actions


def main() -> None:
    targets: list[Path] = []
    for p in ROOT.rglob("*.ts"):
        text = p.read_text(encoding="utf-8")
        if re.search(r"template\s*:\s*[`']", text) or re.search(r"styles\s*:\s*(\[)?\s*`", text):
            targets.append(p)

    # always include app.component if inline
    app = ROOT / "app" / "app.component.ts"
    if app.exists() and app not in targets:
        t = app.read_text(encoding="utf-8")
        if "template:" in t and "templateUrl:" not in t:
            targets.append(app)

    all_actions = []
    for p in sorted(set(targets)):
        try:
            acts = process_file(p)
            all_actions.extend(acts)
            print(f"OK {p.relative_to(ROOT)}")
            for a in acts:
                print(f"  - {a}")
        except Exception as e:
            print(f"FAIL {p.relative_to(ROOT)}: {e}")
            raise

    print(f"\nDone. {len(set(targets))} files processed.")


if __name__ == "__main__":
    main()
