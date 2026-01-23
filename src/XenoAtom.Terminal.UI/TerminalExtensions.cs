// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal;
using XenoAtom.Terminal.UI.Hosting;
using XenoAtom.Terminal.UI.Prompts;
using XenoAtom.Terminal.UI.Rendering;
using System.Globalization;

namespace XenoAtom.Terminal.UI;

/// <summary>
/// Extension members integrating <see cref="XenoAtom.Terminal.UI"/> with <see cref="XenoAtom.Terminal"/>.
/// </summary>
public static partial class TerminalExtensions
{
    private static void EnsureVisualNotAttached(Visual visual, string scenario)
    {
        ArgumentNullException.ThrowIfNull(visual);

        if (visual.Parent is not null)
        {
            throw new InvalidOperationException($"A visual that is already in the UI tree cannot be used as a root for {scenario}.");
        }
    }

    private static async ValueTask RunHostedAsync(
        TerminalInstance terminal,
        Visual root,
        TerminalAppOptions appOptions,
        Func<TerminalRunningContext, TerminalLoopResult> onUpdate,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(terminal);
        ArgumentNullException.ThrowIfNull(appOptions);
        EnsureVisualNotAttached(root, appOptions.HostKind == TerminalHostKind.Inline ? "a live region" : "a fullscreen app");
        ArgumentNullException.ThrowIfNull(onUpdate);

        var app = new TerminalApp(root, terminal, appOptions);
        app.SetUpdateCallback(onUpdate);

        try
        {
            app.Run(cancellationToken);
            if (appOptions.HostKind == TerminalHostKind.Inline)
            {
                if (app.InlineRemoveOnEnd)
                {
                    app.ClearInlineLiveRegion();
                }
                else
                {
                    app.FinalizeInlineLiveRegion();
                }
            }
        }
        finally
        {
            await app.DisposeAsync().ConfigureAwait(false);
        }
    }

    extension(XenoAtom.Terminal.Terminal)
    {
        /// <summary>
        /// Writes a visual once to the default terminal instance.
        /// </summary>
        public static TerminalInstance Write(Visual visual) => XenoAtom.Terminal.Terminal.Instance.Write(visual);

        /// <summary>
        /// Runs an inline live region on the default terminal instance.
        /// </summary>
        public static TerminalInstance Live(Visual visual, Func<TerminalLoopResult> onUpdate) => XenoAtom.Terminal.Terminal.Instance.Live(visual, onUpdate);

        /// <summary>
        /// Runs an inline live region on the default terminal instance.
        /// </summary>
        public static TerminalInstance Live(Visual visual, Func<TerminalRunningContext, TerminalLoopResult> onUpdate) => XenoAtom.Terminal.Terminal.Instance.Live(visual, onUpdate);

        /// <summary>
        /// Runs an inline live region on the default terminal instance with options.
        /// </summary>
        public static TerminalInstance Live(Visual visual, Func<TerminalLoopResult> onUpdate, TerminalLiveOptions options)
            => XenoAtom.Terminal.Terminal.Instance.Live(visual, _ => onUpdate(), options);

        /// <summary>
        /// Runs an inline live region on the default terminal instance with options.
        /// </summary>
        public static TerminalInstance Live(Visual visual, Func<TerminalRunningContext, TerminalLoopResult> onUpdate, TerminalLiveOptions options)
            => XenoAtom.Terminal.Terminal.Instance.Live(visual, onUpdate, options);

        /// <summary>
        /// Runs an inline live region on the default terminal instance.
        /// </summary>
        public static ValueTask<TerminalInstance> LiveAsync(Visual visual, Func<TerminalLoopResult> onUpdate, CancellationToken cancellationToken = default)
            => XenoAtom.Terminal.Terminal.Instance.LiveAsync(visual, onUpdate, cancellationToken);

        /// <summary>
        /// Runs an inline live region on the default terminal instance.
        /// </summary>
        public static ValueTask<TerminalInstance> LiveAsync(Visual visual, Func<TerminalRunningContext, TerminalLoopResult> onUpdate, CancellationToken cancellationToken = default)
            => XenoAtom.Terminal.Terminal.Instance.LiveAsync(visual, onUpdate, cancellationToken);

        /// <summary>
        /// Runs an inline live region on the default terminal instance with options.
        /// </summary>
        public static ValueTask<TerminalInstance> LiveAsync(Visual visual, Func<TerminalLoopResult> onUpdate, TerminalLiveOptions options, CancellationToken cancellationToken = default)
            => XenoAtom.Terminal.Terminal.Instance.LiveAsync(visual, _ => onUpdate(), options, cancellationToken);

        /// <summary>
        /// Runs an inline live region on the default terminal instance with options.
        /// </summary>
        public static ValueTask<TerminalInstance> LiveAsync(Visual visual, Func<TerminalRunningContext, TerminalLoopResult> onUpdate, TerminalLiveOptions options, CancellationToken cancellationToken = default)
            => XenoAtom.Terminal.Terminal.Instance.LiveAsync(visual, onUpdate, options, cancellationToken);

        /// <summary>
        /// Runs a fullscreen terminal UI application on the default terminal instance.
        /// </summary>
        public static TerminalInstance Run(Visual visual, Func<TerminalLoopResult> onUpdate) => XenoAtom.Terminal.Terminal.Instance.Run(visual, onUpdate);

        /// <summary>
        /// Runs a fullscreen terminal UI application on the default terminal instance.
        /// </summary>
        public static ValueTask<TerminalInstance> RunAsync(Visual visual, Func<TerminalLoopResult> onUpdate, CancellationToken cancellationToken = default)
            => XenoAtom.Terminal.Terminal.Instance.RunAsync(visual, onUpdate, cancellationToken);

        /// <summary>
        /// Runs a fullscreen terminal UI application on the default terminal instance.
        /// </summary>
        public static TerminalInstance Run(Visual visual, Func<TerminalRunningContext, TerminalLoopResult> onUpdate, TerminalRunOptions options)
            => XenoAtom.Terminal.Terminal.Instance.Run(visual, onUpdate, options);

        /// <summary>
        /// Runs a fullscreen terminal UI application on the default terminal instance.
        /// </summary>
        public static ValueTask<TerminalInstance> RunAsync(Visual visual, Func<TerminalRunningContext, TerminalLoopResult> onUpdate, TerminalRunOptions options, CancellationToken cancellationToken = default)
            => XenoAtom.Terminal.Terminal.Instance.RunAsync(visual, onUpdate, options, cancellationToken);

        /// <summary>
        /// Runs an inline prompt on the default terminal instance and returns the result.
        /// </summary>
        /// <typeparam name="T">The prompt result type.</typeparam>
        /// <param name="prompt">The prompt to run.</param>
        public static T Prompt<T>(TerminalPrompt<T> prompt) => TerminalPrompts.Prompt(prompt);

        /// <summary>
        /// Runs an inline prompt on the default terminal instance and returns the result.
        /// </summary>
        /// <typeparam name="T">The prompt result type.</typeparam>
        /// <param name="prompt">The prompt to run.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        public static ValueTask<T> PromptAsync<T>(TerminalPrompt<T> prompt, CancellationToken cancellationToken = default)
            => TerminalPrompts.PromptAsync(prompt, cancellationToken);
    }

    extension(TerminalInstance instance)
    {
        /// <summary>
        /// Writes a visual once to this terminal instance.
        /// </summary>
        public TerminalInstance Write(Visual visual)
        {
            ArgumentNullException.ThrowIfNull(instance);
            ArgumentNullException.ThrowIfNull(visual);

            TerminalVisualWriter.Write(instance, visual);
            return instance;
        }

        /// <summary>
        /// Runs an inline live region on this terminal instance.
        /// </summary>
        public TerminalInstance Live(Visual visual, Func<TerminalLoopResult> onUpdate)
            => Live(visual, _ => onUpdate());

        /// <summary>
        /// Runs an inline live region on this terminal instance.
        /// </summary>
        public TerminalInstance Live(Visual visual, Func<TerminalRunningContext, TerminalLoopResult> onUpdate)
        {
            ArgumentNullException.ThrowIfNull(instance);
            var appOptions = new TerminalAppOptions { HostKind = TerminalHostKind.Inline, Culture = CultureInfo.InvariantCulture };
            RunHostedAsync(instance, visual, appOptions, onUpdate, CancellationToken.None)
                .AsTask()
                .GetAwaiter()
                .GetResult();

            return instance;
        }

        /// <summary>
        /// Runs an inline live region on this terminal instance with options.
        /// </summary>
        public TerminalInstance Live(Visual visual, Func<TerminalRunningContext, TerminalLoopResult> onUpdate, TerminalLiveOptions options)
        {
            ArgumentNullException.ThrowIfNull(instance);
            var appOptions = new TerminalAppOptions { HostKind = TerminalHostKind.Inline, Culture = options.Culture ?? CultureInfo.InvariantCulture };
            RunHostedAsync(instance, visual, appOptions, onUpdate, CancellationToken.None)
                .AsTask()
                .GetAwaiter()
                .GetResult();

            return instance;
        }

        /// <summary>
        /// Runs an inline live region on this terminal instance.
        /// </summary>
        public async ValueTask<TerminalInstance> LiveAsync(Visual visual, Func<TerminalLoopResult> onUpdate, CancellationToken cancellationToken = default)
            => await LiveAsync(visual, _ => onUpdate(), cancellationToken).ConfigureAwait(false);

        /// <summary>
        /// Runs an inline live region on this terminal instance.
        /// </summary>
        public async ValueTask<TerminalInstance> LiveAsync(Visual visual, Func<TerminalRunningContext, TerminalLoopResult> onUpdate, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instance);
            var appOptions = new TerminalAppOptions { HostKind = TerminalHostKind.Inline, Culture = CultureInfo.InvariantCulture };
            await RunHostedAsync(instance, visual, appOptions, onUpdate, cancellationToken).ConfigureAwait(false);

            return instance;
        }

        /// <summary>
        /// Runs an inline live region on this terminal instance with options.
        /// </summary>
        public async ValueTask<TerminalInstance> LiveAsync(Visual visual, Func<TerminalRunningContext, TerminalLoopResult> onUpdate, TerminalLiveOptions options, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instance);
            var appOptions = new TerminalAppOptions { HostKind = TerminalHostKind.Inline, Culture = options.Culture ?? CultureInfo.InvariantCulture };
            await RunHostedAsync(instance, visual, appOptions, onUpdate, cancellationToken).ConfigureAwait(false);

            return instance;
        }

        /// <summary>
        /// Runs a fullscreen terminal UI application on this terminal instance.
        /// </summary>
        public TerminalInstance Run(Visual visual, Func<TerminalLoopResult> onUpdate)
            => Run(visual, _ => onUpdate(), options: default);

        /// <summary>
        /// Runs a fullscreen terminal UI application on this terminal instance.
        /// </summary>
        public TerminalInstance Run(Visual visual, Func<TerminalRunningContext, TerminalLoopResult> onUpdate, TerminalRunOptions options)
        {
            ArgumentNullException.ThrowIfNull(instance);
            var appOptions = new TerminalAppOptions
            {
                HostKind = TerminalHostKind.Fullscreen,
                ExitGesture = options.ExitGesture,
                Culture = options.Culture ?? CultureInfo.InvariantCulture,
            };
            RunHostedAsync(instance, visual, appOptions, onUpdate, CancellationToken.None)
                .AsTask()
                .GetAwaiter()
                .GetResult();

            return instance;
        }

        /// <summary>
        /// Runs a fullscreen terminal UI application on this terminal instance.
        /// </summary>
        public async ValueTask<TerminalInstance> RunAsync(Visual visual, Func<TerminalLoopResult> onUpdate, CancellationToken cancellationToken = default)
            => await RunAsync(visual, _ => onUpdate(), options: default, cancellationToken).ConfigureAwait(false);

        /// <summary>
        /// Runs a fullscreen terminal UI application on this terminal instance.
        /// </summary>
        public async ValueTask<TerminalInstance> RunAsync(Visual visual, Func<TerminalRunningContext, TerminalLoopResult> onUpdate, TerminalRunOptions options, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instance);
            var appOptions = new TerminalAppOptions
            {
                HostKind = TerminalHostKind.Fullscreen,
                ExitGesture = options.ExitGesture,
                Culture = options.Culture ?? CultureInfo.InvariantCulture,
            };
            await RunHostedAsync(instance, visual, appOptions, onUpdate, cancellationToken).ConfigureAwait(false);

            return instance;
        }

        /// <summary>
        /// Runs an inline prompt on this terminal instance and returns the result.
        /// </summary>
        /// <typeparam name="T">The prompt result type.</typeparam>
        /// <param name="prompt">The prompt to run.</param>
        public T Prompt<T>(TerminalPrompt<T> prompt) => TerminalPrompts.Prompt(instance, prompt);

        /// <summary>
        /// Runs an inline prompt on this terminal instance and returns the result.
        /// </summary>
        /// <typeparam name="T">The prompt result type.</typeparam>
        /// <param name="prompt">The prompt to run.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        public ValueTask<T> PromptAsync<T>(TerminalPrompt<T> prompt, CancellationToken cancellationToken = default)
            => TerminalPrompts.PromptAsync(instance, prompt, cancellationToken);
    }
}
