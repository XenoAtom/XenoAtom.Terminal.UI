// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal;
using XenoAtom.Terminal.UI;

namespace XenoAtom.Terminal.UI.Prompts;

/// <summary>
/// Provides helper methods for running inline prompts using the terminal UI hosting infrastructure.
/// </summary>
public static class TerminalPrompts
{
    /// <summary>
    /// Runs a prompt on the default terminal instance and returns the result.
    /// </summary>
    /// <typeparam name="T">The prompt result type.</typeparam>
    /// <param name="prompt">The prompt to run.</param>
    public static T Prompt<T>(TerminalPrompt<T> prompt)
        => PromptAsync(prompt).GetAwaiter().GetResult();

    /// <summary>
    /// Runs a prompt on the default terminal instance and returns the result.
    /// </summary>
    /// <typeparam name="T">The prompt result type.</typeparam>
    /// <param name="prompt">The prompt to run.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    public static ValueTask<T> PromptAsync<T>(TerminalPrompt<T> prompt, CancellationToken cancellationToken = default)
        => PromptAsync(Terminal.Instance, prompt, cancellationToken);

    /// <summary>
    /// Runs a prompt on the specified terminal instance and returns the result.
    /// </summary>
    /// <typeparam name="T">The prompt result type.</typeparam>
    /// <param name="terminal">The terminal instance.</param>
    /// <param name="prompt">The prompt to run.</param>
    public static T Prompt<T>(TerminalInstance terminal, TerminalPrompt<T> prompt)
        => PromptAsync(terminal, prompt).GetAwaiter().GetResult();

    /// <summary>
    /// Runs a prompt on the specified terminal instance and returns the result.
    /// </summary>
    /// <typeparam name="T">The prompt result type.</typeparam>
    /// <param name="terminal">The terminal instance.</param>
    /// <param name="prompt">The prompt to run.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    public static async ValueTask<T> PromptAsync<T>(TerminalInstance terminal, TerminalPrompt<T> prompt, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(terminal);
        ArgumentNullException.ThrowIfNull(prompt);

        var session = prompt.CreateSession();

        await terminal.LiveAsync(session.Root, session.Update, cancellationToken).ConfigureAwait(false);

        if (cancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException(cancellationToken);
        }

        if (session.IsCanceled)
        {
            throw new OperationCanceledException("The prompt was canceled.");
        }

        if (!session.IsCompleted)
        {
            throw new InvalidOperationException("The prompt terminated without producing a result.");
        }

        return session.Result!;
    }
}
