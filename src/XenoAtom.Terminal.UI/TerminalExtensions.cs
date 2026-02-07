// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal;
using XenoAtom.Terminal.UI.Hosting;
using XenoAtom.Terminal.UI.Prompts;
using XenoAtom.Terminal.UI.Rendering;
using System.Globalization;
using System.Numerics;

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
        Func<TerminalRunningContext, ValueTask<TerminalLoopResult>> onUpdate,
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

    private static ValueTask RunHostedAsync(
        TerminalInstance terminal,
        Visual root,
        TerminalAppOptions appOptions,
        Func<TerminalRunningContext, TerminalLoopResult> onUpdate,
        CancellationToken cancellationToken)
        => RunHostedAsync(
            terminal,
            root,
            appOptions,
            ctx => new ValueTask<TerminalLoopResult>(onUpdate(ctx)),
            cancellationToken);


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
        /// Runs an inline live region on the default terminal instance with an asynchronous update callback.
        /// </summary>
        public static ValueTask<TerminalInstance> LiveAsync(Visual visual, Func<ValueTask<TerminalLoopResult>> onUpdate, CancellationToken cancellationToken = default)
            => XenoAtom.Terminal.Terminal.Instance.LiveAsync(visual, _ => onUpdate(), cancellationToken);

        /// <summary>
        /// Runs an inline live region on the default terminal instance with an asynchronous update callback.
        /// </summary>
        public static ValueTask<TerminalInstance> LiveAsync(Visual visual, Func<TerminalRunningContext, ValueTask<TerminalLoopResult>> onUpdate, CancellationToken cancellationToken = default)
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
        /// Runs an inline live region on the default terminal instance with options and an asynchronous update callback.
        /// </summary>
        public static ValueTask<TerminalInstance> LiveAsync(Visual visual, Func<TerminalRunningContext, ValueTask<TerminalLoopResult>> onUpdate, TerminalLiveOptions options, CancellationToken cancellationToken = default)
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
        /// Runs a fullscreen terminal UI application on the default terminal instance with an asynchronous update callback.
        /// </summary>
        public static ValueTask<TerminalInstance> RunAsync(Visual visual, Func<ValueTask<TerminalLoopResult>> onUpdate, CancellationToken cancellationToken = default)
            => XenoAtom.Terminal.Terminal.Instance.RunAsync(visual, _ => onUpdate(), options: new(), cancellationToken);

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
        /// Runs a fullscreen terminal UI application on the default terminal instance with an asynchronous update callback.
        /// </summary>
        public static ValueTask<TerminalInstance> RunAsync(Visual visual, Func<TerminalRunningContext, ValueTask<TerminalLoopResult>> onUpdate, TerminalRunOptions options, CancellationToken cancellationToken = default)
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

        /// <summary>
        /// Runs a text prompt on the default terminal instance and returns the captured string.
        /// </summary>
        /// <param name="message">The prompt message.</param>
        /// <param name="configure">An optional action used to configure the prompt.</param>
        /// <returns>The captured string.</returns>
        public static string Ask(string message, Action<TextPrompt>? configure = null)
            => Ask((Visual)message, configure);

        /// <summary>
        /// Runs a text prompt on the default terminal instance and returns the captured string.
        /// </summary>
        /// <param name="message">The prompt message.</param>
        /// <param name="configure">An optional action used to configure the prompt.</param>
        /// <returns>The captured string.</returns>
        public static string Ask(Visual message, Action<TextPrompt>? configure = null)
        {
            var prompt = new TextPrompt(message);
            configure?.Invoke(prompt);
            return Prompt(prompt);
        }

        /// <summary>
        /// Runs a text prompt asynchronously on the default terminal instance and returns the captured string.
        /// </summary>
        /// <param name="message">The prompt message.</param>
        /// <param name="configure">An optional action used to configure the prompt.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The captured string.</returns>
        public static ValueTask<string> AskAsync(string message, Action<TextPrompt>? configure = null, CancellationToken cancellationToken = default)
            => AskAsync((Visual)message, configure, cancellationToken);

        /// <summary>
        /// Runs a text prompt asynchronously on the default terminal instance and returns the captured string.
        /// </summary>
        /// <param name="message">The prompt message.</param>
        /// <param name="configure">An optional action used to configure the prompt.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The captured string.</returns>
        public static ValueTask<string> AskAsync(Visual message, Action<TextPrompt>? configure = null, CancellationToken cancellationToken = default)
        {
            var prompt = new TextPrompt(message);
            configure?.Invoke(prompt);
            return PromptAsync(prompt, cancellationToken);
        }

        /// <summary>
        /// Runs a number prompt on the default terminal instance and returns the captured value.
        /// </summary>
        /// <typeparam name="T">The numeric type.</typeparam>
        /// <param name="message">The prompt message.</param>
        /// <param name="configure">An optional action used to configure the prompt.</param>
        /// <returns>The captured value.</returns>
        public static T AskNumber<T>(string message, Action<NumberPrompt<T>>? configure = null)
            where T : struct, INumber<T>
            => AskNumber((Visual)message, configure);

        /// <summary>
        /// Runs a number prompt on the default terminal instance and returns the captured value.
        /// </summary>
        /// <typeparam name="T">The numeric type.</typeparam>
        /// <param name="message">The prompt message.</param>
        /// <param name="configure">An optional action used to configure the prompt.</param>
        /// <returns>The captured value.</returns>
        public static T AskNumber<T>(Visual message, Action<NumberPrompt<T>>? configure = null)
            where T : struct, INumber<T>
        {
            var prompt = new NumberPrompt<T>(message);
            configure?.Invoke(prompt);
            return Prompt(prompt);
        }

        /// <summary>
        /// Runs a number prompt asynchronously on the default terminal instance and returns the captured value.
        /// </summary>
        /// <typeparam name="T">The numeric type.</typeparam>
        /// <param name="message">The prompt message.</param>
        /// <param name="configure">An optional action used to configure the prompt.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The captured value.</returns>
        public static ValueTask<T> AskNumberAsync<T>(string message, Action<NumberPrompt<T>>? configure = null, CancellationToken cancellationToken = default)
            where T : struct, INumber<T>
            => AskNumberAsync((Visual)message, configure, cancellationToken);

        /// <summary>
        /// Runs a number prompt asynchronously on the default terminal instance and returns the captured value.
        /// </summary>
        /// <typeparam name="T">The numeric type.</typeparam>
        /// <param name="message">The prompt message.</param>
        /// <param name="configure">An optional action used to configure the prompt.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The captured value.</returns>
        public static ValueTask<T> AskNumberAsync<T>(Visual message, Action<NumberPrompt<T>>? configure = null, CancellationToken cancellationToken = default)
            where T : struct, INumber<T>
        {
            var prompt = new NumberPrompt<T>(message);
            configure?.Invoke(prompt);
            return PromptAsync(prompt, cancellationToken);
        }
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
            var appOptions = new TerminalAppOptions
            {
                HostKind = TerminalHostKind.Inline,
                Culture = CultureInfo.InvariantCulture,
                EnableMouse = false,
            };
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
            var appOptions = new TerminalAppOptions
            {
                HostKind = TerminalHostKind.Inline,
                Culture = options.Culture ?? CultureInfo.InvariantCulture,
                EnableMouse = options.EnableMouse,
                MouseMode = options.MouseMode,
                UpdateWaitDuration = options.UpdateWaitDuration,
            };
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
            var appOptions = new TerminalAppOptions
            {
                HostKind = TerminalHostKind.Inline,
                Culture = CultureInfo.InvariantCulture,
                EnableMouse = false,
            };
            await RunHostedAsync(instance, visual, appOptions, onUpdate, cancellationToken).ConfigureAwait(false);

            return instance;
        }

        /// <summary>
        /// Runs an inline live region on this terminal instance with an asynchronous update callback.
        /// </summary>
        public async ValueTask<TerminalInstance> LiveAsync(Visual visual, Func<ValueTask<TerminalLoopResult>> onUpdate, CancellationToken cancellationToken = default)
            => await LiveAsync(visual, _ => onUpdate(), cancellationToken).ConfigureAwait(false);

        /// <summary>
        /// Runs an inline live region on this terminal instance with an asynchronous update callback.
        /// </summary>
        public async ValueTask<TerminalInstance> LiveAsync(Visual visual, Func<TerminalRunningContext, ValueTask<TerminalLoopResult>> onUpdate, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instance);
            var appOptions = new TerminalAppOptions
            {
                HostKind = TerminalHostKind.Inline,
                Culture = CultureInfo.InvariantCulture,
                EnableMouse = false,
            };
            await RunHostedAsync(instance, visual, appOptions, onUpdate, cancellationToken).ConfigureAwait(false);

            return instance;
        }

        /// <summary>
        /// Runs an inline live region on this terminal instance with options.
        /// </summary>
        public async ValueTask<TerminalInstance> LiveAsync(Visual visual, Func<TerminalRunningContext, TerminalLoopResult> onUpdate, TerminalLiveOptions options, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instance);
            var appOptions = new TerminalAppOptions
            {
                HostKind = TerminalHostKind.Inline,
                Culture = options.Culture ?? CultureInfo.InvariantCulture,
                EnableMouse = options.EnableMouse,
                MouseMode = options.MouseMode,
                UpdateWaitDuration = options.UpdateWaitDuration,
            };
            await RunHostedAsync(instance, visual, appOptions, onUpdate, cancellationToken).ConfigureAwait(false);

            return instance;
        }

        /// <summary>
        /// Runs an inline live region on this terminal instance with options and an asynchronous update callback.
        /// </summary>
        public async ValueTask<TerminalInstance> LiveAsync(Visual visual, Func<TerminalRunningContext, ValueTask<TerminalLoopResult>> onUpdate, TerminalLiveOptions options, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instance);
            var appOptions = new TerminalAppOptions
            {
                HostKind = TerminalHostKind.Inline,
                Culture = options.Culture ?? CultureInfo.InvariantCulture,
                EnableMouse = options.EnableMouse,
                MouseMode = options.MouseMode,
                UpdateWaitDuration = options.UpdateWaitDuration,
            };
            await RunHostedAsync(instance, visual, appOptions, onUpdate, cancellationToken).ConfigureAwait(false);

            return instance;
        }

        /// <summary>
        /// Runs a fullscreen terminal UI application on this terminal instance.
        /// </summary>
        public TerminalInstance Run(Visual visual, Func<TerminalLoopResult> onUpdate)
            => Run(visual, _ => onUpdate(), options: new());

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
                UpdateWaitDuration = options.UpdateWaitDuration,
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
            => await RunAsync(visual, _ => onUpdate(), options: new(), cancellationToken).ConfigureAwait(false);

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
                UpdateWaitDuration = options.UpdateWaitDuration,
            };
            await RunHostedAsync(instance, visual, appOptions, onUpdate, cancellationToken).ConfigureAwait(false);

            return instance;
        }

        /// <summary>
        /// Runs a fullscreen terminal UI application on this terminal instance with an asynchronous update callback.
        /// </summary>
        public async ValueTask<TerminalInstance> RunAsync(Visual visual, Func<ValueTask<TerminalLoopResult>> onUpdate, CancellationToken cancellationToken = default)
            => await RunAsync(visual, _ => onUpdate(), options: new(), cancellationToken).ConfigureAwait(false);

        /// <summary>
        /// Runs a fullscreen terminal UI application on this terminal instance with an asynchronous update callback.
        /// </summary>
        public async ValueTask<TerminalInstance> RunAsync(Visual visual, Func<TerminalRunningContext, ValueTask<TerminalLoopResult>> onUpdate, TerminalRunOptions options, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instance);
            var appOptions = new TerminalAppOptions
            {
                HostKind = TerminalHostKind.Fullscreen,
                ExitGesture = options.ExitGesture,
                Culture = options.Culture ?? CultureInfo.InvariantCulture,
                UpdateWaitDuration = options.UpdateWaitDuration,
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

        /// <summary>
        /// Runs a text prompt on this terminal instance and returns the captured string.
        /// </summary>
        /// <param name="message">The prompt message.</param>
        /// <param name="configure">An optional action used to configure the prompt.</param>
        /// <returns>The captured string.</returns>
        public string Ask(string message, Action<TextPrompt>? configure = null)
            => Ask((Visual)message, configure);

        /// <summary>
        /// Runs a text prompt on this terminal instance and returns the captured string.
        /// </summary>
        /// <param name="message">The prompt message.</param>
        /// <param name="configure">An optional action used to configure the prompt.</param>
        /// <returns>The captured string.</returns>
        public string Ask(Visual message, Action<TextPrompt>? configure = null)
        {
            var prompt = new TextPrompt(message);
            configure?.Invoke(prompt);
            return Prompt(prompt);
        }

        /// <summary>
        /// Runs a text prompt asynchronously on this terminal instance and returns the captured string.
        /// </summary>
        /// <param name="message">The prompt message.</param>
        /// <param name="configure">An optional action used to configure the prompt.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The captured string.</returns>
        public ValueTask<string> AskAsync(string message, Action<TextPrompt>? configure = null, CancellationToken cancellationToken = default)
            => AskAsync((Visual)message, configure, cancellationToken);

        /// <summary>
        /// Runs a text prompt asynchronously on this terminal instance and returns the captured string.
        /// </summary>
        /// <param name="message">The prompt message.</param>
        /// <param name="configure">An optional action used to configure the prompt.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The captured string.</returns>
        public ValueTask<string> AskAsync(Visual message, Action<TextPrompt>? configure = null, CancellationToken cancellationToken = default)
        {
            var prompt = new TextPrompt(message);
            configure?.Invoke(prompt);
            return PromptAsync(prompt, cancellationToken);
        }

        /// <summary>
        /// Runs a number prompt on this terminal instance and returns the captured value.
        /// </summary>
        /// <typeparam name="T">The numeric type.</typeparam>
        /// <param name="message">The prompt message.</param>
        /// <param name="configure">An optional action used to configure the prompt.</param>
        /// <returns>The captured value.</returns>
        public T AskNumber<T>(string message, Action<NumberPrompt<T>>? configure = null)
            where T : struct, INumber<T>
            => AskNumber((Visual)message, configure);

        /// <summary>
        /// Runs a number prompt on this terminal instance and returns the captured value.
        /// </summary>
        /// <typeparam name="T">The numeric type.</typeparam>
        /// <param name="message">The prompt message.</param>
        /// <param name="configure">An optional action used to configure the prompt.</param>
        /// <returns>The captured value.</returns>
        public T AskNumber<T>(Visual message, Action<NumberPrompt<T>>? configure = null)
            where T : struct, INumber<T>
        {
            var prompt = new NumberPrompt<T>(message);
            configure?.Invoke(prompt);
            return Prompt(prompt);
        }

        /// <summary>
        /// Runs a number prompt asynchronously on this terminal instance and returns the captured value.
        /// </summary>
        /// <typeparam name="T">The numeric type.</typeparam>
        /// <param name="message">The prompt message.</param>
        /// <param name="configure">An optional action used to configure the prompt.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The captured value.</returns>
        public ValueTask<T> AskNumberAsync<T>(string message, Action<NumberPrompt<T>>? configure = null, CancellationToken cancellationToken = default)
            where T : struct, INumber<T>
            => AskNumberAsync((Visual)message, configure, cancellationToken);

        /// <summary>
        /// Runs a number prompt asynchronously on this terminal instance and returns the captured value.
        /// </summary>
        /// <typeparam name="T">The numeric type.</typeparam>
        /// <param name="message">The prompt message.</param>
        /// <param name="configure">An optional action used to configure the prompt.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The captured value.</returns>
        public ValueTask<T> AskNumberAsync<T>(Visual message, Action<NumberPrompt<T>>? configure = null, CancellationToken cancellationToken = default)
            where T : struct, INumber<T>
        {
            var prompt = new NumberPrompt<T>(message);
            configure?.Invoke(prompt);
            return PromptAsync(prompt, cancellationToken);
        }
    }
}
