// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal;
using XenoAtom.Terminal.UI.Graphics;

namespace XenoAtom.Terminal.UI;

/// <summary>
/// Provides convenience methods for writing terminal image visuals as one-shot flow output.
/// </summary>
public static class TerminalImageVisualExtensions
{
    extension(global::XenoAtom.Terminal.Terminal)
    {
        /// <summary>
        /// Writes an image visual once to the default terminal instance using a temporary terminal image graphics presenter.
        /// </summary>
        /// <param name="image">The image visual to write.</param>
        /// <returns>The default terminal instance.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="image"/> is <see langword="null"/>.</exception>
        public static TerminalInstance Write(Image image) => global::XenoAtom.Terminal.Terminal.Instance.Write(image);
    }

    extension(TerminalInstance terminal)
    {
        /// <summary>
        /// Writes an image visual once to this terminal instance using a temporary terminal image graphics presenter.
        /// </summary>
        /// <param name="image">The image visual to write.</param>
        /// <returns>The terminal instance.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="terminal"/> or <paramref name="image"/> is <see langword="null"/>.</exception>
        public TerminalInstance Write(Image image)
        {
            ArgumentNullException.ThrowIfNull(terminal);
            ArgumentNullException.ThrowIfNull(image);

            using var presenter = new TerminalImageGraphicsPresenter(new TerminalImageGraphicsPresenterOptions
            {
                DeleteRetainedImagesOnDispose = false,
            });
            return terminal.Write((Visual)image, new TerminalWriteOptions { GraphicsPresenter = presenter });
        }
    }
}
