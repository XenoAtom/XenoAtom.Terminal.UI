using System.Text;

namespace XenoAtom.Terminal.UI;

/// <summary>
/// Provides predefined predicates used to widen additional rune ranges to two terminal cells.
/// </summary>
/// <remarks>
/// These resolvers are intended for use with <see cref="TerminalAppOptions.WideRuneResolver"/>,
/// <see cref="TerminalLiveOptions.WideRuneResolver"/>, and <see cref="TerminalRunOptions.WideRuneResolver"/>.
/// </remarks>
public static class TerminalWideRuneResolvers
{
    /// <summary>
    /// Gets the default resolver used by Terminal.UI.
    /// </summary>
    /// <remarks>
    /// This resolver treats emoji-like scalars and standard Nerd Font glyphs as wide.
    /// </remarks>
    public static Func<Rune, bool> Default { get; } = IsEmojiOrNerdFontWide;

    /// <summary>
    /// Gets a resolver that never widens additional runes.
    /// </summary>
    public static Func<Rune, bool> None { get; } = static _ => false;

    /// <summary>
    /// Gets a resolver that widens emoji-like scalars only.
    /// </summary>
    public static Func<Rune, bool> EmojiOnly { get; } = global::XenoAtom.Terminal.TerminalTextUtility.IsLikelyEmojiScalar;

    /// <summary>
    /// Gets a resolver for standard Nerd Fonts where Nerd Font glyphs render as double-width.
    /// </summary>
    /// <remarks>
    /// This is equivalent to <see cref="Default"/>.
    /// </remarks>
    public static Func<Rune, bool> NerdFontDoubleWidth { get; } = IsEmojiOrNerdFontWide;

    /// <summary>
    /// Gets a resolver for Nerd Font Mono where Nerd Font glyphs stay single-width.
    /// </summary>
    /// <remarks>
    /// Emoji-like scalars are still widened because terminals commonly render them as wide glyphs.
    /// </remarks>
    public static Func<Rune, bool> NerdFontMono { get; } = EmojiOnly;

    private static bool IsEmojiOrNerdFontWide(Rune rune)
        => global::XenoAtom.Terminal.TerminalTextUtility.IsLikelyEmojiScalar(rune) || NerdFont.IsWideRuneCandidate(rune);
}
