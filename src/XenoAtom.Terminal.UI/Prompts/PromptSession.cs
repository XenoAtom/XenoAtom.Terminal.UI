// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Prompts;

/// <summary>
/// Represents a running prompt instance (internal) used by <see cref="TerminalPrompts"/>.
/// </summary>
/// <typeparam name="T">The prompt result type.</typeparam>
internal sealed class PromptSession<T>
{
    private readonly Func<(bool Ok, T Value)> _tryGetValue;
    private readonly Func<T, string?>? _validator;
    private readonly bool _keepOnSuccess;
    private bool _initialized;
    private Visual? _focusTarget;

    public PromptSession(Func<(bool Ok, T Value)> tryGetValue, Func<T, string?>? validator, bool keepOnSuccess)
    {
        _tryGetValue = tryGetValue;
        _validator = validator;
        _keepOnSuccess = keepOnSuccess;
        Root = null!;
    }

    public Visual Root { get; private set; }

    public bool IsCompleted { get; private set; }

    public bool IsCanceled { get; private set; }

    public T? Result { get; private set; }

    public void SetRoot(Visual root, Visual? focusTarget)
    {
        ArgumentNullException.ThrowIfNull(root);
        Root = root;
        _focusTarget = focusTarget;
    }

    public TerminalLoopResult Update(TerminalRunningContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!_initialized)
        {
            _initialized = true;
            if (_focusTarget is not null)
            {
                context.App.Focus(_focusTarget);
            }
        }

        if (IsCompleted)
        {
            return _keepOnSuccess ? TerminalLoopResult.StopAndKeepVisual : TerminalLoopResult.Stop;
        }

        if (IsCanceled)
        {
            return TerminalLoopResult.Stop;
        }

        return TerminalLoopResult.Continue;
    }

    public bool TryConfirm()
    {
        if (IsCompleted || IsCanceled)
        {
            return true;
        }

        var (ok, value) = _tryGetValue();
        if (!ok)
        {
            return false;
        }

        var message = _validator?.Invoke(value);
        if (!string.IsNullOrEmpty(message))
        {
            return false;
        }

        Result = value;
        IsCompleted = true;
        return true;
    }

    public void Cancel()
    {
        if (IsCompleted || IsCanceled)
        {
            return;
        }

        IsCanceled = true;
    }
}
