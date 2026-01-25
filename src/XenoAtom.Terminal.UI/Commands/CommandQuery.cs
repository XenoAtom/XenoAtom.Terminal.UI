// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Commands;

/// <summary>
/// Provides shared command discovery helpers for UI surfaces such as the command bar, command palette, and context menus.
/// </summary>
internal static class CommandQuery
{
    /// <summary>
    /// Collects commands visible in the current focus context for a given presentation surface.
    /// </summary>
    /// <param name="app">The application hosting the visual tree.</param>
    /// <param name="focus">The focused visual used as a starting point for command discovery. When <see langword="null"/>, uses <see cref="TerminalApp.FocusedElement"/>.</param>
    /// <param name="presentation">The presentation surface to filter commands by.</param>
    /// <param name="output">The destination list. It is cleared before being populated.</param>
    public static void Collect(TerminalApp app, Visual? focus, CommandPresentation presentation, List<ResolvedCommand> output)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(output);

        output.Clear();

        // Mirror keyboard routing semantics:
        // - local commands execute against the visual that registered them
        // - global commands execute against the focused element (or root when there is no focus)
        focus ??= app.FocusedElement;

        var order = 0;

        for (var v = focus; v is not null; v = v.Parent)
        {
            AppendCommands(output, v.Commands, v, presentation, isGlobal: false, ref order);
        }

        var globalTarget = focus ?? app.Root;
        AppendCommands(output, app.GlobalCommands, globalTarget, presentation, isGlobal: true, ref order);

        // Ensure stable ordering:
        // - local commands first, then global
        // - within each group: CommandImportance then insertion order
        output.Sort(static (a, b) =>
        {
            if (a.IsGlobal != b.IsGlobal)
            {
                return a.IsGlobal ? 1 : -1;
            }

            var importance = a.Command.Importance.CompareTo(b.Command.Importance);
            if (importance != 0)
            {
                return importance;
            }

            return a.Order.CompareTo(b.Order);
        });
    }

    private static void AppendCommands(
        List<ResolvedCommand> output,
        IReadOnlyList<Command> commands,
        Visual target,
        CommandPresentation presentation,
        bool isGlobal,
        ref int order)
    {
        for (var i = 0; i < commands.Count; i++)
        {
            var cmd = commands[i];
            if ((cmd.Presentation & presentation) == 0)
            {
                continue;
            }

            if (!cmd.IsVisibleFor(target))
            {
                continue;
            }

            // Dedup by Id without extra allocations; command sets are typically small.
            var exists = false;
            for (var j = 0; j < output.Count; j++)
            {
                if (StringComparer.Ordinal.Equals(output[j].Command.Id, cmd.Id))
                {
                    exists = true;
                    break;
                }
            }

            if (exists)
            {
                continue;
            }

            output.Add(new ResolvedCommand(cmd, target, cmd.CanExecuteFor(target), isGlobal) { Order = order });
            order++;
        }
    }
}
