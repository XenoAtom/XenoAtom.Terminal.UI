// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Controls;

internal static class OverlayFocusRestore
{
    public static Visual? Capture(TerminalApp app, Visual? target)
    {
        ArgumentNullException.ThrowIfNull(app);
        return Resolve(app, target);
    }

    public static void Restore(TerminalApp app, ref Visual? target)
    {
        ArgumentNullException.ThrowIfNull(app);

        var candidate = Resolve(app, target);
        target = null;

        if (candidate is not null)
        {
            app.Focus(candidate);
        }
    }

    private static Visual? Resolve(TerminalApp app, Visual? candidate)
    {
        for (var visual = candidate; visual is not null; visual = visual.Parent)
        {
            if (ReferenceEquals(visual.App, app)
                && visual.Focusable
                && visual.IsEnabled
                && visual.IsVisible)
            {
                return visual;
            }
        }

        return null;
    }
}
