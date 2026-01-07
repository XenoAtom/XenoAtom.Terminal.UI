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
    extension(XenoAtom.Terminal.Terminal)
    {
        public static TerminalInstance Write(Visual visual) => XenoAtom.Terminal.Terminal.Instance.Write(visual);

        public static TerminalInstance Live(Visual visual, Func<bool> onUpdate) => XenoAtom.Terminal.Terminal.Instance.Live(visual, onUpdate);

        public static ValueTask<TerminalInstance> LiveAsync(Visual visual, Func<bool> onUpdate, CancellationToken cancellationToken = default)
            => XenoAtom.Terminal.Terminal.Instance.LiveAsync(visual, onUpdate, cancellationToken);

        public static TerminalInstance Run(Visual visual) => XenoAtom.Terminal.Terminal.Instance.Run(visual);

        public static ValueTask<TerminalInstance> RunAsync(Visual visual, CancellationToken cancellationToken = default)
            => XenoAtom.Terminal.Terminal.Instance.RunAsync(visual, cancellationToken);
    }

    extension(TerminalInstance instance)
    {
        public TerminalInstance Write(Visual visual)
        {
            ArgumentNullException.ThrowIfNull(instance);
            ArgumentNullException.ThrowIfNull(visual);

            TerminalVisualWriter.Write(instance, visual);
            return instance;
        }

        public TerminalInstance Live(Visual visual, Func<bool> onUpdate)
        {
            ArgumentNullException.ThrowIfNull(instance);
            ArgumentNullException.ThrowIfNull(visual);
            ArgumentNullException.ThrowIfNull(onUpdate);

            if (visual.Parent is not null)
            {
                throw new InvalidOperationException("A visual that is already in the UI tree cannot be used as a root for a live region.");
            }

            var options = new TerminalAppOptions { HostKind = TerminalHostKind.Inline };
            var app = new TerminalApp(visual, instance, options);
            app.SetUpdateCallback(onUpdate);

            try
            {
                app.Run();
            }
            finally
            {
                app.DisposeAsync().AsTask().GetAwaiter().GetResult();
            }

            return instance;
        }

        public async ValueTask<TerminalInstance> LiveAsync(Visual visual, Func<bool> onUpdate, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instance);
            ArgumentNullException.ThrowIfNull(visual);
            ArgumentNullException.ThrowIfNull(onUpdate);

            if (visual.Parent is not null)
            {
                throw new InvalidOperationException("A visual that is already in the UI tree cannot be used as a root for a live region.");
            }

            var options = new TerminalAppOptions { HostKind = TerminalHostKind.Inline };
            var app = new TerminalApp(visual, instance, options);
            app.SetUpdateCallback(onUpdate);

            try
            {
                app.Run(cancellationToken);
            }
            finally
            {
                await app.DisposeAsync().ConfigureAwait(false);
            }

            return instance;
        }

        public TerminalInstance Run(Visual visual)
        {
            ArgumentNullException.ThrowIfNull(instance);
            ArgumentNullException.ThrowIfNull(visual);

            if (visual.Parent is not null)
            {
                throw new InvalidOperationException("A visual that is already in the UI tree cannot be used as a root for a fullscreen app.");
            }

            var options = new TerminalAppOptions { HostKind = TerminalHostKind.Fullscreen };
            var app = new TerminalApp(visual, instance, options);

            try
            {
                app.Run();
            }
            finally
            {
                app.DisposeAsync().AsTask().GetAwaiter().GetResult();
            }

            return instance;
        }

        public TerminalInstance Run(Visual visual, Func<bool> onUpdate)
        {
            ArgumentNullException.ThrowIfNull(instance);
            ArgumentNullException.ThrowIfNull(visual);
            ArgumentNullException.ThrowIfNull(onUpdate);

            if (visual.Parent is not null)
            {
                throw new InvalidOperationException("A visual that is already in the UI tree cannot be used as a root for a fullscreen app.");
            }

            var options = new TerminalAppOptions { HostKind = TerminalHostKind.Fullscreen };
            var app = new TerminalApp(visual, instance, options);
            app.SetUpdateCallback(onUpdate);

            try
            {
                app.Run();
            }
            finally
            {
                app.DisposeAsync().AsTask().GetAwaiter().GetResult();
            }

            return instance;
        }

        public async ValueTask<TerminalInstance> RunAsync(Visual visual, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instance);
            ArgumentNullException.ThrowIfNull(visual);

            if (visual.Parent is not null)
            {
                throw new InvalidOperationException("A visual that is already in the UI tree cannot be used as a root for a fullscreen app.");
            }

            var options = new TerminalAppOptions { HostKind = TerminalHostKind.Fullscreen };
            var app = new TerminalApp(visual, instance, options);

            try
            {
                app.Run(cancellationToken);
            }
            finally
            {
                await app.DisposeAsync().ConfigureAwait(false);
            }

            return instance;
        }

        public async ValueTask<TerminalInstance> RunAsync(Visual visual, Func<bool> onUpdate, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instance);
            ArgumentNullException.ThrowIfNull(visual);
            ArgumentNullException.ThrowIfNull(onUpdate);

            if (visual.Parent is not null)
            {
                throw new InvalidOperationException("A visual that is already in the UI tree cannot be used as a root for a fullscreen app.");
            }

            var options = new TerminalAppOptions { HostKind = TerminalHostKind.Fullscreen };
            var app = new TerminalApp(visual, instance, options);
            app.SetUpdateCallback(onUpdate);

            try
            {
                app.Run(cancellationToken);
            }
            finally
            {
                await app.DisposeAsync().ConfigureAwait(false);
            }

            return instance;
        }
    }
}
