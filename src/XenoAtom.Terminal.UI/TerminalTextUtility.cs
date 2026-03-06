using System.Text;

namespace XenoAtom.Terminal.UI;

internal static class TerminalTextUtility
{
    private static readonly AsyncLocal<Func<Rune, bool>?> CurrentWideRuneResolver = new();

    public const int DefaultTabWidth = global::XenoAtom.Terminal.TerminalTextUtility.DefaultTabWidth;

    public static int GetWidth(ReadOnlySpan<char> text, int tabWidth = DefaultTabWidth)
        => global::XenoAtom.Terminal.TerminalTextUtility.GetWidth(text, ResolveWideRuneResolver(), tabWidth);

    public static int GetWidth(ReadOnlySpan<char> text, Func<Rune, bool>? isWideRune, int tabWidth = DefaultTabWidth)
        => global::XenoAtom.Terminal.TerminalTextUtility.GetWidth(text, ResolveWideRuneResolver(isWideRune), tabWidth);

    public static int GetWidth(ReadOnlySpan<char> text, int start, int length, int tabWidth = DefaultTabWidth)
        => global::XenoAtom.Terminal.TerminalTextUtility.GetWidth(text, start, length, ResolveWideRuneResolver(), tabWidth);

    public static int GetWidth(ReadOnlySpan<char> text, int start, int length, Func<Rune, bool>? isWideRune, int tabWidth = DefaultTabWidth)
        => global::XenoAtom.Terminal.TerminalTextUtility.GetWidth(text, start, length, ResolveWideRuneResolver(isWideRune), tabWidth);

    public static int GetRuneWidth(Rune rune, int tabWidth = DefaultTabWidth)
        => global::XenoAtom.Terminal.TerminalTextUtility.GetRuneWidth(rune, ResolveWideRuneResolver(), tabWidth);

    public static int GetRuneWidth(Rune rune, Func<Rune, bool>? isWideRune, int tabWidth = DefaultTabWidth)
        => global::XenoAtom.Terminal.TerminalTextUtility.GetRuneWidth(rune, ResolveWideRuneResolver(isWideRune), tabWidth);

    public static int GetPreviousRuneIndex(ReadOnlySpan<char> text, int index)
        => global::XenoAtom.Terminal.TerminalTextUtility.GetPreviousRuneIndex(text, index);

    public static int GetNextRuneIndex(ReadOnlySpan<char> text, int index)
        => global::XenoAtom.Terminal.TerminalTextUtility.GetNextRuneIndex(text, index);

    public static int GetPreviousTextElementIndex(ReadOnlySpan<char> text, int index)
        => global::XenoAtom.Terminal.TerminalTextUtility.GetPreviousTextElementIndex(text, index);

    public static int GetNextTextElementIndex(ReadOnlySpan<char> text, int index)
        => global::XenoAtom.Terminal.TerminalTextUtility.GetNextTextElementIndex(text, index);

    public static bool TryGetIndexAtCell(ReadOnlySpan<char> text, int cellOffset, out int index, int tabWidth = DefaultTabWidth)
        => global::XenoAtom.Terminal.TerminalTextUtility.TryGetIndexAtCell(text, cellOffset, out index, ResolveWideRuneResolver(), tabWidth);

    public static bool TryGetIndexAtCell(ReadOnlySpan<char> text, int cellOffset, out int index, Func<Rune, bool>? isWideRune, int tabWidth = DefaultTabWidth)
        => global::XenoAtom.Terminal.TerminalTextUtility.TryGetIndexAtCell(text, cellOffset, out index, ResolveWideRuneResolver(isWideRune), tabWidth);

    public static bool IsLikelyEmojiScalar(Rune rune)
        => global::XenoAtom.Terminal.TerminalTextUtility.IsLikelyEmojiScalar(rune);

    public static bool IsWordChar(char c)
        => global::XenoAtom.Terminal.TerminalTextUtility.IsWordChar(c);

    public static bool IsWordStart(ReadOnlySpan<char> text, int index)
        => global::XenoAtom.Terminal.TerminalTextUtility.IsWordStart(text, index);

    public static bool IsWordEnd(ReadOnlySpan<char> text, int indexExclusive)
        => global::XenoAtom.Terminal.TerminalTextUtility.IsWordEnd(text, indexExclusive);

    public static int GetWordStart(ReadOnlySpan<char> text, int index)
        => global::XenoAtom.Terminal.TerminalTextUtility.GetWordStart(text, index);

    public static int GetWordEnd(ReadOnlySpan<char> text, int index)
        => global::XenoAtom.Terminal.TerminalTextUtility.GetWordEnd(text, index);

    internal static IDisposable PushWideRuneResolver(Func<Rune, bool>? wideRuneResolver)
    {
        var previous = CurrentWideRuneResolver.Value;
        CurrentWideRuneResolver.Value = wideRuneResolver;
        return new WideRuneResolverScope(previous);
    }

    internal static Func<Rune, bool> ResolveWideRuneResolver(Func<Rune, bool>? wideRuneResolver = null)
        => wideRuneResolver ?? CurrentWideRuneResolver.Value ?? TerminalWideRuneResolvers.Default;

    private sealed class WideRuneResolverScope(Func<Rune, bool>? previous) : IDisposable
    {
        private Func<Rune, bool>? _previous = previous;

        public void Dispose()
        {
            CurrentWideRuneResolver.Value = _previous;
            _previous = null;
        }
    }
}
