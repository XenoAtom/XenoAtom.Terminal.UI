// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal;
using XenoAtom.Terminal.UI.Hosting;
using XenoAtom.Terminal.UI.Rendering;

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
        TerminalHostKind hostKind,
        Func<TerminalRunningContext, TerminalLoopResult> onUpdate,
        TerminalRunOptions runOptions,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(terminal);
        EnsureVisualNotAttached(root, hostKind == TerminalHostKind.Inline ? "a live region" : "a fullscreen app");
        ArgumentNullException.ThrowIfNull(onUpdate);

        var appOptions = new TerminalAppOptions { HostKind = hostKind };
        if (hostKind == TerminalHostKind.Fullscreen)
        {
            appOptions = new TerminalAppOptions
            {
                HostKind = TerminalHostKind.Fullscreen,
                ExitGesture = runOptions.ExitGesture,
            };
        }

        var app = new TerminalApp(root, terminal, appOptions);
        app.SetUpdateCallback(onUpdate);

        try
        {
            app.Run(cancellationToken);
            if (hostKind == TerminalHostKind.Inline)
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
            RunHostedAsync(instance, visual, TerminalHostKind.Inline, onUpdate, runOptions: default, CancellationToken.None)
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
            await RunHostedAsync(instance, visual, TerminalHostKind.Inline, onUpdate, runOptions: default, cancellationToken).ConfigureAwait(false);

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
            RunHostedAsync(instance, visual, TerminalHostKind.Fullscreen, onUpdate, options, CancellationToken.None)
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
            await RunHostedAsync(instance, visual, TerminalHostKind.Fullscreen, onUpdate, options, cancellationToken).ConfigureAwait(false);

            return instance;
        }
    }
}
