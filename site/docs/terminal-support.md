---
title: Terminal Support
---

# Terminal support & troubleshooting

XenoAtom.Terminal.UI runs on Windows, macOS, and Linux. The exact experience depends heavily on the **terminal emulator**
you run in, especially for **colors**.

Terminal.UI supports:

- 16-color palettes
- 256-color indexed palettes
- truecolor (24-bit RGB via VT/ANSI `SGR 38/48;2`)
- internal **alpha blending** (RGBA) for theming and composition

> [!IMPORTANT]
> Terminal emulators do **not** support alpha blending natively.
> Terminal.UI blends RGBA into RGB (or the closest available palette) *before* emitting output.
> For the best results, use a **truecolor** terminal.

## Quick diagnostics

### From C#

You can inspect what XenoAtom.Terminal detected:

```csharp
using XenoAtom.Terminal;

Terminal.WriteLine($"Ansi={Terminal.Capabilities.AnsiEnabled}, Color={Terminal.Capabilities.ColorLevel}");
```

If `ColorLevel` is `TrueColor`, Terminal.UI can render your theme with the highest fidelity.

### From the shell (truecolor smoke test)

This uses the standard 24-bit SGR sequences. If the word is **not** colored, you are likely not in truecolor mode:

```sh
printf "\033[38;2;255;100;0mTRUECOLOR\033[0m\n"
```

## Windows

### Recommended

- **Windows Terminal**: tested; truecolor support is on by default.

### Notes

- The classic Windows Console gained 24-bit color support (VT `38;2`/`48;2`) in Windows 10 Insider build **14931** and later.

### WSL: “only 256 colors detected”

Inside WSL, many applications rely on environment variables/terminfo hints to decide whether to use truecolor.
If you see 256-color behavior under WSL (even though Windows Terminal can do truecolor), try setting:

- `COLORTERM=truecolor` (or `COLORTERM=24bit`)

For example in your shell profile:

```sh
export COLORTERM=truecolor
```

## macOS

### Recommended

- **iTerm2**: tested and supports 24-bit color sequences (truecolor).

### Terminal.app (built-in)

Recent version of Terminal.app seems to support 24-bit SGR support; but it is using a line height that is bigger than the font size, which causes rendering issues with Terminal.UI. If you want to use Terminal.app, set a custom line height equal to the font size.

## Linux

Linux terminal support varies by emulator, but modern terminals typically support truecolor.

### Known-good examples

- **GNOME Terminal** supports “true color mode” in addition to 16/256 colors.

## Multiplexers (tmux) and remote sessions

### tmux

Even if your terminal supports truecolor, tmux may need to be told that the outer terminal supports RGB, otherwise it may
downsample colors.

From the tmux FAQ (modern tmux):

```tmux
# tmux 3.2+: advertise RGB to tmux based on your outer TERM prefix.
set -as terminal-features ",xterm*:RGB"

# Older tmux: use the Tc extension.
# set -as terminal-overrides ",xterm*:Tc"
```

> [!TIP]
> Replace `xterm*` with the prefix of your outer `$TERM` (for example `alacritty*`, `wezterm*`, `screen*`, etc.).

### SSH

With SSH, `TERM` and `COLORTERM` may not be forwarded the way you expect. If colors look wrong only over SSH or inside tmux,
start by checking:

```sh
echo "$TERM"
echo "$COLORTERM"
```

## Common environment variables

These are not strict “standards”, but they matter in practice:

- `TERM`: base terminfo capability set (commonly `xterm-256color`)
- `COLORTERM`: often used by apps to detect truecolor (`truecolor` or `24bit`)

## Why truecolor matters for Terminal.UI

Terminal.UI themes rely on:

- rich color palettes
- RGBA colors internally (alpha blending)

When truecolor is unavailable, XenoAtom.Terminal approximates colors to the closest supported palette so things still render
correctly — but subtle gradients/overlays will look flatter.

## References

- Microsoft: VT `SGR 38;2`/`48;2` (24-bit) sequences: https://learn.microsoft.com/en-us/windows/console/console-virtual-terminal-sequences
- Windows 10 build 14931 truecolor announcement: https://devblogs.microsoft.com/commandline/24-bit-color-in-the-windows-console/
- termstandard/colors truecolor compatibility list: https://github.com/termstandard/colors
- GNOME Terminal docs (mentions true color): https://help.gnome.org/users/gnome-terminal/stable/app-colors.html.en
- tmux FAQ (RGB / `Tc`): https://github.com/tmux/tmux/wiki/FAQ#how-do-i-use-rgb-colour
- iTerm2 feature reporting spec (documents `24BIT` / `38;2`): https://iterm2.com/feature-reporting/

## Next

- [Rendering](rendering.md) (CellBuffer, alpha blending)
- [Styling & Themes](styling.md)
- [XenoAtom.Terminal](terminal.md)
