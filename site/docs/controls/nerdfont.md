---
title: NerdFont
---

# NerdFont

`NerdFont` is a generated helper class that exposes official Nerd Fonts glyph names as `Rune` properties.

The API lives in the main `XenoAtom.Terminal.UI` namespace, so the glyphs are easy to use from `TextBlock`, `Markup`,
`Paragraph`, and string interpolation.

![NerdFont](../../img/controls/nerdfont.png){.terminal}

> [!NOTE]
> To see the expected glyphs in your terminal, use a font that includes Nerd Fonts symbols.
> Without a compatible font, the runes may appear as fallback squares or replacement glyphs.

## Basic usage

```csharp
using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;

var title = new TextBlock($"{NerdFont.CodAccount}  Account settings");
var branch = new TextBlock($"{NerdFont.PlBranch} main");
```

If you have the original Nerd Fonts glyph name at runtime, use `TryGetRune`:

```csharp
if (NerdFont.TryGetRune("cod-account", out var accountIcon))
{
    var account = new TextBlock($"{accountIcon} Account settings");
}

foreach (var name in NerdFont.Names)
{
    // Original Nerd Fonts glyph names such as "cod-account".
}
```

## With Markup

`Rune` properties also work naturally inside markup strings:

```csharp
var status = new Markup(
    $"[primary]{NerdFont.MdHome}[/] Home  " +
    $"[accent]{NerdFont.FaGithub}[/] GitHub  " +
    $"[success]{NerdFont.WeatherDaySunny}[/] Ready");
```

## Width profiles

Terminal.UI does **not** widen Nerd Font glyphs by default. The default resolver only widens emoji-like scalars, because
most terminals still measure private-use Nerd Font glyphs as single-cell even when a standard Nerd Font is installed.

If your terminal/font combination actually renders Nerd Font glyphs as double-width, opt into
`TerminalWideRuneResolvers.NerdFontDoubleWidth`:

```csharp
Terminal.Run(
    root,
    onUpdate,
    new TerminalRunOptions
    {
        WideRuneResolver = TerminalWideRuneResolvers.NerdFontDoubleWidth,
    });
```

If you use **Nerd Font Mono** (`NFM`), or if your terminal already treats Nerd Font glyphs as single-cell, keep the
default resolver or use `TerminalWideRuneResolvers.NerdFontMono`.

For inline/live hosting, use the same property on `TerminalLiveOptions`.

> [!NOTE]
> Proportional Nerd Font setups (`NFP`) are not supported by Terminal.UI layout, because the framework assumes a
> fixed cell grid.

## Naming

Property names follow the family prefix from the official Nerd Fonts metadata:

- `CodAccount`
- `MdHome`
- `OctMarkGithub`
- `WeatherDaySunny`

Internally the generated code is split across multiple partial files for maintainability, but the public API is a single
`NerdFont` class.

For runtime lookup, pass the original metadata name (including the family prefix, dashes, underscores, and casing) to
`NerdFont.TryGetRune`, for example `cod-account` or `md-language_csharp`. Use `NerdFont.Names` to enumerate all names
available for lookup.

## Source and coverage

The generated properties come from the official `glyphnames.json` file published by Nerd Fonts. The current generated set
covers the full upstream catalog exposed by that file, including families such as Codicons, Material Design, Octicons,
Powerline, Weather Icons, Seti, and more.

## Related

- [Markup](markup.md)
- [TextBlock](textblock.md)
- [Paragraph](paragraph.md)
