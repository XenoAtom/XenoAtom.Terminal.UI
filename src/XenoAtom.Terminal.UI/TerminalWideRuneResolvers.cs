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
    /// Gets a resolver that widens emoji-like scalars only.
    /// </summary>
    public static Func<Rune, bool> EmojiOnly { get; } = global::XenoAtom.Terminal.TerminalTextUtility.IsLikelyEmojiScalar;

    /// <summary>
    /// Gets the default resolver used by Terminal.UI.
    /// </summary>
    /// <remarks>
    /// This safe default widens emoji-like scalars only.
    /// Private-use Nerd Font glyphs are left narrow unless explicitly opted into
    /// <see cref="NerdFontDoubleWidth"/>, because most terminals still treat them as single-cell glyphs.
    /// </remarks>
    public static Func<Rune, bool> Default { get; } = EmojiOnly;

    /// <summary>
    /// Gets a resolver that never widens additional runes.
    /// </summary>
    public static Func<Rune, bool> None { get; } = static _ => false;

    /// <summary>
    /// Gets a resolver for standard Nerd Fonts where Nerd Font glyphs render as double-width.
    /// </summary>
    /// <remarks>
    /// Use this only when your terminal/font combination actually renders Nerd Font glyphs as two cells.
    /// </remarks>
    public static Func<Rune, bool> NerdFontDoubleWidth { get; } = IsEmojiOrNerdFontWide;

    /// <summary>
    /// Gets a resolver for Nerd Font Mono where Nerd Font glyphs stay single-width.
    /// </summary>
    /// <remarks>
    /// This is functionally equivalent to <see cref="EmojiOnly"/>.
    /// </remarks>
    public static Func<Rune, bool> NerdFontMono { get; } = global::XenoAtom.Terminal.TerminalTextUtility.IsLikelyEmojiScalar;

    private static bool IsEmojiOrNerdFontWide(Rune rune)
        => global::XenoAtom.Terminal.TerminalTextUtility.IsLikelyEmojiScalar(rune) || NerdFont.IsWideRuneCandidate(rune);
}
