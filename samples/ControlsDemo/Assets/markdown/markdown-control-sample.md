# MarkdownControl Demo Document

This document is intentionally long and feature-dense so the demo can validate CommonMark rendering plus selected extensions.
It starts with a quick tour so the first viewport shows mixed content instead of heading-only blocks.

## 1. Quick Feature Tour

This paragraph includes *emphasis*, **strong emphasis**, and ***combined emphasis*** in one sentence.
Inline code appears as `Console.WriteLine("Hello")` and links appear as [project repository](https://github.com/XenoAtom/XenoAtom.Terminal.UI).

Reference style links are also covered: [Terminal UI Docs][docs-link] and [Markdown Guide][guide-link].
Automatic URL support should remain readable: <https://xenoatom.github.io/terminal/docs/>.

Escaped characters: \*not italic\*, \_not emphasis\_, and a literal bracket \[value\].
Entity support: ampersand is &amp; and angle brackets are &lt;tag&gt;.

Hard line break with two trailing spaces.  
This line should start immediately below.

Soft line break
continues as a normal space in the same paragraph.

> A simple quote with **strong text** and a [quoted link](https://example.com/quoted).
>
> > Nested quote with `inline code`.

- Unordered item one
- Unordered item two with `code`
- Unordered item three with a [nested link](https://example.com/nested)

1. Ordered item one
2. Ordered item two
3. Ordered item three

---

## 2. Tables Extension

| Feature | Status | Notes |
|:--------|:------:|------:|
| Headings | Done | 100 |
| Lists | Done | 95 |
| Tables | Done | 90 |
| Alerts | Done | 85 |

| Escaped Pipe | Value |
|--------------|-------|
| `a \| b` | Literal pipe in code span |

---

## 3. Alert Blocks Extension

> [!NOTE]
> Note alert content with **bold** text and a [link](https://example.com/note).

> [!TIP]
> Tip alert content with `inline code`.
>
> - Alert list item one
> - Alert list item two

> [!IMPORTANT]
> Important alert paragraph with additional context.

> [!WARNING]
> Warning alert with cautionary details.

> [!CAUTION]
> Caution alert with potential risk description.

---

## 4. Code Blocks

```csharp
public static class DemoProgram
{
    public static void Main()
    {
        for (var i = 0; i < 3; i++)
        {
            Console.WriteLine($"Line {i}");
        }
    }
}
```

```json
{
  "name": "markdown-demo",
  "kind": "controls",
  "enabled": true,
  "values": [1, 2, 3, 5, 8, 13]
}
```

Indented code block:

    SELECT id, name, status
    FROM tasks
    WHERE status IN ('queued', 'running')
    ORDER BY id DESC;

---

## 5. Heading Reference

# Primary Heading Example

## Secondary Heading Example

Setext Secondary Heading
------------------------

---

## 6. HTML Blocks

<div class="callout">
  <p>This is raw HTML block content used as fallback text in terminal rendering.</p>
</div>

<table>
  <tr><th>HTML</th><th>Fallback</th></tr>
  <tr><td>Yes</td><td>Text only</td></tr>
</table>

---

## 7. Images

![Project Logo](https://raw.githubusercontent.com/XenoAtom/XenoAtom.Terminal.UI/main/img/XenoAtom.Terminal.UI.png)

Image with title:
![Sample Image](https://example.com/image.png "Image Title")

---

## 8. Long Scrolling Section

Paragraph 01: Lorem ipsum dolor sit amet, consectetur adipiscing elit. Integer posuere feugiat sem, at cursus lectus viverra et.

Paragraph 02: Sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation.

Paragraph 03: Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur.

Paragraph 04: Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.

Paragraph 05: Vestibulum ante ipsum primis in faucibus orci luctus et ultrices posuere cubilia curae; Integer consequat turpis at eros.

Paragraph 06: Curabitur fringilla, eros non finibus ullamcorper, lectus libero scelerisque massa, sit amet luctus est ipsum sed odio.

Paragraph 07: Donec pharetra dignissim sem, sed luctus metus ultrices in. Integer tempor augue et ipsum tincidunt, sed malesuada arcu ultrices.

Paragraph 08: Quisque non mi in erat dictum efficitur. Nullam at turpis in metus scelerisque ullamcorper.

Paragraph 09: Aliquam erat volutpat. Praesent ac suscipit nunc, a cursus est. Nam vel tortor ac erat convallis efficitur.

Paragraph 10: Morbi nec velit vitae nisi vulputate porta. Integer vitae turpis ac leo pharetra lacinia.

Paragraph 11: Pellentesque habitant morbi tristique senectus et netus et malesuada fames ac turpis egestas.

Paragraph 12: Nunc volutpat faucibus justo, sed iaculis ante luctus vel. Suspendisse potenti.

Paragraph 13: Mauris gravida, neque ac malesuada mattis, est nunc semper justo, sit amet lobortis ligula risus in nulla.

Paragraph 14: Phasellus convallis lectus a nibh placerat, non tristique lorem hendrerit.

Paragraph 15: Aenean at neque non massa faucibus sodales. Cras id risus non risus dictum tincidunt.

Paragraph 16: In ac nulla non justo varius consectetur. Sed viverra sem in lacus facilisis, ut pulvinar leo bibendum.

Paragraph 17: Integer non neque sed metus dictum aliquet a quis tortor. Nam aliquet justo et ex euismod luctus.

Paragraph 18: Nunc posuere quam vitae metus suscipit, eu sodales felis finibus. Suspendisse ornare luctus faucibus.

Paragraph 19: Vestibulum elementum erat in dui elementum, non tincidunt ligula tincidunt. Maecenas varius sem nibh.

Paragraph 20: End of long scrolling section.

---

[docs-link]: https://xenoatom.github.io/terminal/docs/
[guide-link]: https://commonmark.org/help/
