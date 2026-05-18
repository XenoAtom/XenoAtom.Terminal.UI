using System.Linq;

namespace XenoAtom.Terminal.UI.ControlsDemo.Demos;

public sealed partial class CodeEditorDemo
{
    private static readonly CodeEditorLanguageSample[] Samples = CreateSamples();
    private static readonly string[] LanguageDisplayNames = Samples.Select(static sample => sample.DisplayName).ToArray();
    private static readonly int DefaultSampleIndex = Array.FindIndex(Samples, static sample => string.Equals(sample.LanguageId, "csharp", StringComparison.Ordinal));
    private static readonly CodeEditorLanguageSample DefaultSample = GetSampleByIndex(DefaultSampleIndex);

    private static CodeEditorLanguageSample GetSampleByIndex(int index)
        => Samples[Math.Clamp(index, 0, Samples.Length - 1)];

    private static CodeEditorLanguageSample[] CreateSamples()
    {
        return
        [
            new(
                LanguageId: "csharp",
                DisplayName: "C#",
                FindText: "CodeEditorDemo",
                ReplaceText: "RenderSummaryAsync",
                CaretText: "CodeEditorDemo",
                GoToLine: 12,
                GoToColumn: 17,
                ScreenshotScrollOffset: 7,
                Source:
"""
using System.Text;

namespace Demo.EditorSamples;

// TextMate should color comments, keywords, strings, and interpolations here.
public sealed class CodeEditorDemo
{
    public async Task<string> RenderSummaryAsync(IReadOnlyList<int> values, CancellationToken cancellationToken = default)
    {
        if (values.Count == 0)
        {
            return "empty";
        }

        var sb = new StringBuilder();
        foreach (var value in values.Where(static value => value > 0))
        {
            cancellationToken.ThrowIfCancellationRequested();
            sb.AppendLine($"CodeEditor sample value: {value:000}");
        }

        await Task.Delay(5, cancellationToken);
        return sb.ToString().TrimEnd();
    }
}

file static class Palette
{
    public static readonly string Accent = "DeepSkyBlue";
}
"""),
            new(
                LanguageId: "cpp",
                DisplayName: "C++",
                FindText: "render_summary",
                ReplaceText: "values",
                CaretText: "render_summary",
                GoToLine: 11,
                GoToColumn: 8,
                ScreenshotScrollOffset: 0,
                Source:
"""
#include <algorithm>
#include <format>
#include <iostream>
#include <ranges>
#include <string>
#include <vector>

namespace demo
{
// Filter positive values and render a compact summary.
std::string render_summary(const std::vector<int>& values)
{
    if (values.empty())
    {
        return "empty";
    }

    std::string buffer;
    for (const auto value : values | std::views::filter([](int v) { return v > 0; }))
    {
        buffer += std::format("value: {:03}\n", value);
    }

    return buffer;
}
} // namespace demo
"""),
            new(
                LanguageId: "css",
                DisplayName: "CSS",
                FindText: ".editor-shell",
                ReplaceText: "var(--accent)",
                CaretText: ".editor-shell",
                GoToLine: 8,
                GoToColumn: 1,
                ScreenshotScrollOffset: 0,
                Source:
"""
/* Theme variables for the CodeEditor card. */
:root {
  --accent: #4cc2ff;
  --panel: #0f172a;
  --panel-border: color-mix(in srgb, var(--accent) 30%, white);
}

.editor-shell {
  display: grid;
  grid-template-columns: minmax(14rem, 20rem) 1fr;
  gap: 1rem;
  padding: 1.25rem;
  background: linear-gradient(180deg, #111827, #020617);
  color: #e5f3ff;
}

.editor-shell__gutter {
  border-inline-end: 1px solid var(--panel-border);
  padding-inline-end: 0.75rem;
}

.editor-shell__status:hover {
  color: var(--accent); /* Accent color on hover. */
}
"""),
            new(
                LanguageId: "diff",
                DisplayName: "Diff",
                FindText: "@@",
                ReplaceText: "+    languageSelect,",
                CaretText: "@@",
                GoToLine: 7,
                GoToColumn: 1,
                ScreenshotScrollOffset: 0,
                Source:
"""
diff --git a/samples/ControlsDemo/Demos/CodeEditorDemo.cs b/samples/ControlsDemo/Demos/CodeEditorDemo.cs
index 4b825dc..8f2d31a 100644
--- a/samples/ControlsDemo/Demos/CodeEditorDemo.cs
+++ b/samples/ControlsDemo/Demos/CodeEditorDemo.cs
@@ -44,7 +44,10 @@ public sealed partial class CodeEditorDemo : ControlsDemoBase
         var controls = new HStack(
             new CheckBox("Wrap").IsChecked(wordWrap),
             new CheckBox("Line numbers").IsChecked(showLineNumbers),
-            new Button("Find").Click(() => editor.OpenFind("CodeEditor")));
+            "Language",
+            languageSelect,
+            new Button("Find").Click(() => editor.OpenFind(findText.Value)));

         return new DockLayout()
             .Top(topPanel)
             .Content(editor);
"""),
            new(
                LanguageId: "go",
                DisplayName: "Go",
                FindText: "renderSummary",
                ReplaceText: "highlight",
                CaretText: "renderSummary",
                GoToLine: 10,
                GoToColumn: 6,
                ScreenshotScrollOffset: 0,
                Source:
"""
package main

import (
	"fmt"
	"strings"
)

// sample holds the label and values rendered into the summary.
type sample struct {
	name   string
	values []int
}

func renderSummary(item sample) string {
	if len(item.values) == 0 {
		return "empty"
	}

	var builder strings.Builder
	for _, value := range item.values {
		builder.WriteString(fmt.Sprintf("%s => %03d\n", item.name, value))
	}

	return strings.TrimSpace(builder.String())
}
"""),
            new(
                LanguageId: "html",
                DisplayName: "HTML",
                FindText: "<section",
                ReplaceText: "CodeEditor",
                CaretText: "<section",
                GoToLine: 9,
                GoToColumn: 1,
                ScreenshotScrollOffset: 0,
                Source:
"""
<!doctype html>
<html lang="en">
  <head>
    <meta charset="utf-8" />
    <title>CodeEditor sample</title>
    <meta name="viewport" content="width=device-width, initial-scale=1" />
  </head>
  <body>
    <main class="shell">
      <!-- TextMate should highlight HTML comments, tags, and attributes. -->
      <section class="editor-card">
        <h1>CodeEditor Demo</h1>
        <p data-role="summary">Swap the language selector to see another TextMate grammar.</p>
        <button type="button">Open replace</button>
      </section>
    </main>
  </body>
</html>
"""),
            new(
                LanguageId: "javascript",
                DisplayName: "JavaScript",
                FindText: "renderSummary",
                ReplaceText: "CodeEditor",
                CaretText: "renderSummary",
                GoToLine: 10,
                GoToColumn: 10,
                ScreenshotScrollOffset: 0,
                Source:
"""
const state = {
  language: "javascript",
  values: [2, 5, 8, 13],
};

// Render a formatted preview for the active sample values.
export function renderSummary(values = state.values) {
  if (values.length === 0) {
    return "empty";
  }

  return values
    .filter((value) => value > 0)
    .map((value, index) => `CodeEditor sample ${index + 1}: ${value}`)
    .join("\n");
}

console.log(renderSummary());
"""),
            new(
                LanguageId: "json",
                DisplayName: "JSON",
                FindText: "\"language\"",
                ReplaceText: "\"theme\"",
                CaretText: "\"language\"",
                GoToLine: 4,
                GoToColumn: 3,
                ScreenshotScrollOffset: 0,
                Source:
"""
{
  "name": "controls-demo",
  "version": 1,
  "language": "json",
  "theme": "dark-plus",
  "features": [
    "code-editor",
    "language-picker",
    "textmate"
  ],
  "layout": {
    "wrap": true,
    "showLineNumbers": true,
    "highlightCurrentLine": true
  }
}
"""),
            new(
                LanguageId: "toml",
                DisplayName: "TOML",
                FindText: "ui.theme",
                ReplaceText: "code-editor",
                CaretText: "ui.theme",
                GoToLine: 7,
                GoToColumn: 1,
                ScreenshotScrollOffset: 0,
                Source:
"""
# TOML 1.1 sample for the CodeEditor TextMate grammar.
title = "Controls Demo"
launched_at = 2026-05-18 09:30Z

[ui]
theme = "dark-plus"
accent = "\e[38;2;76;194;255m"
features = ["code-editor", "textmate", "toml"]

[ui.shortcuts]
find = "Ctrl+F"
replace = "Ctrl+H"

[[ui.samples]]
language = "toml"
score = +1_024.5
metadata = {
  owner = "XenoAtom",
  enabled = true,
  times = [07:32, 1979-05-27T07:32-07:00],
}
"""),
            new(
                LanguageId: "python",
                DisplayName: "Python",
                FindText: "render_summary",
                ReplaceText: "sample",
                CaretText: "render_summary",
                GoToLine: 9,
                GoToColumn: 5,
                ScreenshotScrollOffset: 0,
                Source:
"""
from __future__ import annotations

from dataclasses import dataclass


# Render a compact summary for the sample values.
@dataclass(slots=True)
class Sample:
    name: str
    values: list[int]


def render_summary(sample: Sample) -> str:
    if not sample.values:
        return "empty"

    lines = [f"{sample.name}: {value:03d}" for value in sample.values if value > 0]
    return "\n".join(lines)


print(render_summary(Sample("CodeEditor", [3, 5, 8])))
"""),
            new(
                LanguageId: "rust",
                DisplayName: "Rust",
                FindText: "render_summary",
                ReplaceText: "language",
                CaretText: "render_summary",
                GoToLine: 10,
                GoToColumn: 4,
                ScreenshotScrollOffset: 0,
                Source:
"""
use std::fmt::Write;

// Highlight Rust comments, lifetimes, strings, and formatting macros.
fn render_summary(language: &str, values: &[i32]) -> String {
    if values.is_empty() {
        return "empty".to_string();
    }

    let mut buffer = String::new();
    for value in values.iter().copied().filter(|value| *value > 0) {
        let _ = writeln!(&mut buffer, "{language} => {value:03}");
    }

    buffer.trim_end().to_string()
}

fn main() {
    println!("{}", render_summary("rust", &[1, 2, 3, 5, 8]));
}
"""),
            new(
                LanguageId: "swift",
                DisplayName: "Swift",
                FindText: "renderSummary",
                ReplaceText: "CodeEditor",
                CaretText: "renderSummary",
                GoToLine: 9,
                GoToColumn: 6,
                ScreenshotScrollOffset: 0,
                Source:
"""
import Foundation

// Swift comments should be easy to spot in the demo sample.
struct Sample {
    let name: String
    let values: [Int]
}

func renderSummary(for sample: Sample) -> String {
    guard !sample.values.isEmpty else {
        return "empty"
    }

    return sample.values
        .filter { $0 > 0 }
        .map { "\(sample.name): \($0)" }
        .joined(separator: "\n")
}

print(renderSummary(for: Sample(name: "CodeEditor", values: [1, 2, 3, 5, 8])))
"""),
            new(
                LanguageId: "xml",
                DisplayName: "XML",
                FindText: "<codeEditor",
                ReplaceText: "<language",
                CaretText: "<codeEditor",
                GoToLine: 2,
                GoToColumn: 1,
                ScreenshotScrollOffset: 0,
                Source:
"""
<?xml version="1.0" encoding="utf-8"?>
<codeEditor language="xml" theme="dark-plus">
  <!-- XML comments help show TextMate token colors here too. -->
  <features>
    <feature name="lineNumbers" enabled="true" />
    <feature name="search" enabled="true" />
    <feature name="textmate" enabled="true" />
  </features>
  <samples>
    <sample id="primary">CodeEditor demo</sample>
    <sample id="secondary">Language switcher</sample>
  </samples>
</codeEditor>
"""),
            new(
                LanguageId: "yaml",
                DisplayName: "YAML",
                FindText: "language:",
                ReplaceText: "textmate",
                CaretText: "language:",
                GoToLine: 2,
                GoToColumn: 1,
                ScreenshotScrollOffset: 0,
                Source:
"""
# YAML comments are common in config files and should be highlighted.
name: controls-demo
language: yaml
theme: dark-plus
editor:
  wrap: true
  showLineNumbers: true
  highlightCurrentLine: true
  search:
    find: CodeEditor
    replace: textmate
  samples:
    - csharp
    - python
    - rust
""")
        ];
    }

    private sealed record CodeEditorLanguageSample(
        string LanguageId,
        string DisplayName,
        string FindText,
        string ReplaceText,
        string CaretText,
        int GoToLine,
        int GoToColumn,
        int ScreenshotScrollOffset,
        string Source)
    {
        public int GetCaretIndex()
            => Math.Max(0, Source.IndexOf(CaretText, StringComparison.Ordinal));
    }
}
