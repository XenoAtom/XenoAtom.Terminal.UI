// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Input;

namespace XenoAtom.Terminal.UI.Prompts;

/// <summary>
/// Hosts prompt content and intercepts <c>Enter</c>/<c>Escape</c> to complete/cancel the prompt.
/// </summary>
internal sealed class PromptHost : ContentVisual
{
    private readonly Func<bool> _tryConfirm;
    private readonly Action _cancel;

    public PromptHost(Visual content, Func<bool> tryConfirm, Action cancel)
    {
        ArgumentNullException.ThrowIfNull(tryConfirm);
        ArgumentNullException.ThrowIfNull(cancel);

        Focusable = false;
        Content = content;
        _tryConfirm = tryConfirm;
        _cancel = cancel;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == TerminalKey.Enter)
        {
            if (_tryConfirm())
            {
                e.Handled = true;
            }

            return;
        }

        if (e.Key == TerminalKey.Escape)
        {
            _cancel();
            e.Handled = true;
            return;
        }

        base.OnKeyDown(e);
    }
}

