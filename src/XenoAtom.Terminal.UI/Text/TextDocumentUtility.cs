// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Text;

internal static class TextDocumentUtility
{
    public static string GetText(ITextDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        return GetText(document.CurrentSnapshot);
    }

    public static string GetText(ITextSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        if (snapshot is TextSnapshot concrete)
        {
            return concrete.Text;
        }

        if (snapshot.Length == 0)
        {
            return string.Empty;
        }

        return string.Create(snapshot.Length, snapshot, static (span, snap) =>
        {
            snap.CopyTo(0, span);
        });
    }
}
