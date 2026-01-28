// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Diagnostics;
using System.Text;
using XenoAtom.Terminal.UI.Animation;
using XenoAtom.Terminal.UI.Collections;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Layout;
using XenoAtom.Terminal.UI.Threading;

namespace XenoAtom.Terminal.UI.Controls;

/// <summary>
/// Hosts toast notifications and overlays them above a wrapped content visual.
/// </summary>
public sealed partial class ToastHost : ContentVisual, IAnimatedVisual
{
    private readonly ToastLayer _layer;
    private readonly List<ToastEntry> _entries;
    private long _lastAnimationTick;

    /// <summary>
    /// Initializes a new instance of the <see cref="ToastHost"/> class.
    /// </summary>
    public ToastHost()
    {
        _entries = new List<ToastEntry>();
        _layer = new ToastLayer();
        AttachChild(_layer);
        this.HorizontalAlignment = Align.Stretch;
        this.VerticalAlignment = Align.Stretch;

        Focusable = false;

        this.Position(ToastPosition.TopRight);
        this.MaxVisible(5);
        this.Spacing(1);
        this.Inset(new Thickness(1));
        this.DefaultDuration(TimeSpan.FromSeconds(3));
        this.PauseOnHover(true);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ToastHost"/> class with a content visual.
    /// </summary>
    /// <param name="content">The wrapped content.</param>
    public ToastHost(Visual content)
        : this()
    {
        Content = content;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ToastHost"/> class with a content factory.
    /// </summary>
    /// <param name="contentFactory">The factory that provides the wrapped content.</param>
    public ToastHost(Func<Visual> contentFactory)
        : this()
    {
        this.Content(contentFactory);
    }

    /// <summary>
    /// Gets or sets the toast placement within the viewport.
    /// </summary>
    [Bindable]
    public partial ToastPosition Position { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of simultaneously visible toasts.
    /// </summary>
    [Bindable]
    public partial int MaxVisible { get; set; }

    /// <summary>
    /// Gets or sets the spacing between stacked toasts.
    /// </summary>
    [Bindable]
    public partial int Spacing { get; set; }

    /// <summary>
    /// Gets or sets the inset from the viewport edges used when positioning the toast overlay.
    /// </summary>
    [Bindable]
    public partial Thickness Inset { get; set; }

    /// <summary>
    /// Gets or sets the default auto-dismiss duration for toasts that don't specify one.
    /// </summary>
    [Bindable]
    public partial TimeSpan DefaultDuration { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether timers pause while a toast is hovered.
    /// </summary>
    [Bindable]
    public partial bool PauseOnHover { get; set; }

    internal IReadOnlyList<Toast> VisibleToasts => _layer.Toasts;

    long IAnimatedVisual.NextAnimationTick => _entries.Count == 0 ? long.MaxValue : 0;

    bool IAnimatedVisual.AdvanceAnimation(long timestamp) => AdvanceAnimation(timestamp);

    /// <summary>
    /// Shows a toast with the provided content.
    /// </summary>
    /// <param name="content">The toast content.</param>
    /// <param name="severity">The toast severity.</param>
    /// <returns>The created toast.</returns>
    public Toast Show(Visual content, ToastSeverity severity = ToastSeverity.Info)
    {
        ArgumentNullException.ThrowIfNull(content);

        var toast = new Toast
        {
            Content = content,
            Severity = severity,
        };

        if (!toast.HasDurationOverride)
        {
            toast.Duration = DefaultDuration;
        }

        return Show(() => toast);
    }

    /// <summary>
    /// Shows a toast using a factory callback.
    /// </summary>
    /// <param name="build">The factory that creates the toast.</param>
    /// <returns>The created toast.</returns>
    public Toast Show(Func<Toast> build)
    {
        ArgumentNullException.ThrowIfNull(build);

        var toast = build();
        ArgumentNullException.ThrowIfNull(toast);

        if (toast.Content is null)
        {
            throw new InvalidOperationException("Toast content must be provided.");
        }

        if (!toast.HasDurationOverride && toast.Duration is null)
        {
            toast.Duration = DefaultDuration;
        }

        AddToast(toast);
        return toast;
    }

    /// <summary>
    /// Dismisses all visible toasts.
    /// </summary>
    public void DismissAll()
    {
        for (var i = _entries.Count - 1; i >= 0; i--)
        {
            RemoveToast(_entries[i], ToastDismissReason.Programmatic);
        }
    }

    /// <summary>
    /// Dismisses toasts that match the given predicate.
    /// </summary>
    /// <param name="predicate">The predicate used to select toasts.</param>
    public void Dismiss(Func<Toast, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        for (var i = _entries.Count - 1; i >= 0; i--)
        {
            var entry = _entries[i];
            if (predicate(entry.Toast))
            {
                RemoveToast(entry, ToastDismissReason.Programmatic);
            }
        }
    }

    internal void RequestDismiss(Toast toast, ToastDismissReason reason)
    {
        var entry = FindEntry(toast);
        if (entry is null)
        {
            return;
        }

        RemoveToast(entry, reason);
    }

    internal void ResetTimer(Toast toast)
    {
        var entry = FindEntry(toast);
        if (entry is null || entry.DurationTicks <= 0)
        {
            return;
        }
        entry.RemainingTicks = entry.DurationTicks;
    }

    internal void PauseTimer(Toast toast)
    {
        var entry = FindEntry(toast);
        if (entry is null)
        {
            return;
        }

        entry.ManualPaused = true;
    }

    internal void ResumeTimer(Toast toast)
    {
        var entry = FindEntry(toast);
        if (entry is null)
        {
            return;
        }

        entry.ManualPaused = false;
    }

    /// <inheritdoc />
    protected override void PrepareChildren()
    {
        base.PrepareChildren();
        _layer.Position = Position;
        _layer.Spacing = Spacing;
    }

    /// <inheritdoc />
    protected override int ChildrenCount => Content is null ? 1 : 2;

    /// <inheritdoc />
    protected override Visual GetChild(int index)
    {
        var content = Content;
        if (content is null)
        {
            return index == 0 ? _layer : throw new ArgumentOutOfRangeException(nameof(index));
        }

        return index switch
        {
            0 => content,
            1 => _layer,
            _ => throw new ArgumentOutOfRangeException(nameof(index)),
        };
    }

    /// <inheritdoc />
    protected override SizeHints MeasureCore(in LayoutConstraints constraints)
    {
        var content = Content;
        var contentHints = content?.Measure(constraints) ?? SizeHints.Fixed(Size.Zero);

        var inset = Inset;
        var maxWidth = constraints.MaxWidth == LayoutConstants.Infinite
            ? LayoutConstants.Infinite
            : Math.Max(0, constraints.MaxWidth - inset.Horizontal);

        var maxHeight = LayoutConstants.Infinite;
        var layerConstraints = new LayoutConstraints(0, maxWidth, 0, maxHeight);
        _layer.Measure(layerConstraints);

        return contentHints;
    }

    /// <inheritdoc />
    protected override void ArrangeCore(in Rectangle finalRect)
    {
        Content?.Arrange(finalRect);

        var layerSize = _layer.DesiredSize;
        if (layerSize.Width <= 0 || layerSize.Height <= 0)
        {
            _layer.Arrange(new Rectangle(finalRect.X, finalRect.Y, 0, 0));
            return;
        }

        var inset = Inset;
        var available = new Rectangle(
            finalRect.X + inset.Left,
            finalRect.Y + inset.Top,
            Math.Max(0, finalRect.Width - inset.Horizontal),
            Math.Max(0, finalRect.Height - inset.Vertical));

        if (available.Width <= 0 || available.Height <= 0)
        {
            _layer.Arrange(new Rectangle(finalRect.X, finalRect.Y, 0, 0));
            return;
        }

        var width = Math.Min(available.Width, layerSize.Width);
        var height = Math.Min(available.Height, layerSize.Height);
        var x = available.X;
        var y = available.Y;

        switch (Position)
        {
            case ToastPosition.TopCenter:
            case ToastPosition.BottomCenter:
                x = available.X + (available.Width - width) / 2;
                break;
            case ToastPosition.TopRight:
            case ToastPosition.BottomRight:
                x = available.Right - width;
                break;
        }

        switch (Position)
        {
            case ToastPosition.BottomLeft:
            case ToastPosition.BottomCenter:
            case ToastPosition.BottomRight:
                y = available.Bottom - height;
                break;
        }

        _layer.Arrange(new Rectangle(x, y, width, height));
    }

    /// <inheritdoc />
    protected override void OnDetachedFromApp(TerminalApp app)
    {
        DismissAll();
        base.OnDetachedFromApp(app);
    }

    private void AddToast(Toast toast)
    {
        if (toast.Parent is not null)
        {
            throw new InvalidOperationException("The toast is already attached to a visual tree.");
        }

        var maxVisible = Math.Max(1, MaxVisible);
        if (_entries.Count >= maxVisible)
        {
            RemoveToast(_entries[^1], ToastDismissReason.Overflow);
        }

        toast.AttachHost(this);

        _layer.Toasts.Insert(0, toast);

        var duration = toast.Duration;
        var durationTicks = duration is { } span ? ToStopwatchTicks(span) : 0;

        _entries.Insert(0, new ToastEntry(toast, durationTicks));
        App?.RegisterAnimatedVisual(this);
    }

    private void RemoveToast(ToastEntry entry, ToastDismissReason reason)
    {
        if (!_entries.Remove(entry))
        {
            return;
        }

        _layer.Toasts.Remove(entry.Toast);
        entry.Toast.AttachHost(null);
        entry.Toast.RaiseDismissed(reason);
    }

    private ToastEntry? FindEntry(Toast toast)
    {
        for (var i = 0; i < _entries.Count; i++)
        {
            if (ReferenceEquals(_entries[i].Toast, toast))
            {
                return _entries[i];
            }
        }

        return null;
    }

    private bool AdvanceAnimation(long timestamp)
    {
        var changed = false;
        var delta = _lastAnimationTick == 0 ? 0 : Math.Max(0, timestamp - _lastAnimationTick);
        _lastAnimationTick = timestamp;

        for (var i = _entries.Count - 1; i >= 0; i--)
        {
            var entry = _entries[i];
            if (PauseOnHover)
            {
                entry.HoverPaused = entry.Toast.IsHovered;
            }

            if (entry.DurationTicks > 0 && !entry.IsPaused && delta > 0)
            {
                entry.RemainingTicks = Math.Max(0, entry.RemainingTicks - delta);
                if (entry.RemainingTicks == 0)
                {
                    RemoveToast(entry, ToastDismissReason.Timeout);
                    changed = true;
                    continue;
                }
            }

            if (entry.DurationTicks > 0 && entry.Toast.ShowProgress)
            {
                var progress = 1.0 - (double)entry.RemainingTicks / entry.DurationTicks;
                progress = Math.Clamp(progress, 0, 1);
                if (Math.Abs(entry.Toast.CountdownProgress - progress) > 0.0001)
                {
                    entry.Toast.CountdownProgress = progress;
                    changed = true;
                }
            }
        }

        return changed;
    }

    private static long ToStopwatchTicks(TimeSpan interval)
    {
        if (interval <= TimeSpan.Zero)
        {
            return 1;
        }

        var ticks = interval.TotalSeconds * Stopwatch.Frequency;
        if (ticks < 1)
        {
            return 1;
        }

        return (long)ticks;
    }

    private sealed class ToastEntry
    {
        public ToastEntry(Toast toast, long durationTicks)
        {
            Toast = toast;
            DurationTicks = durationTicks;
            RemainingTicks = durationTicks;
        }

        public Toast Toast { get; }

        public long DurationTicks { get; }

        public long RemainingTicks { get; set; }

        public bool ManualPaused { get; set; }

        public bool HoverPaused { get; set; }

        public bool IsPaused => ManualPaused || HoverPaused;
    }

    private sealed partial class ToastLayer : Visual
    {
        private readonly VisualList<Toast> _toasts;

        public ToastLayer()
        {
            _toasts = new VisualList<Toast>(this, "ToastLayer.Toasts");
            Focusable = false;
        }

        [Bindable]
        public partial ToastPosition Position { get; set; }

        [Bindable]
        public partial int Spacing { get; set; }

        public VisualList<Toast> Toasts => _toasts;

        protected override int ChildrenCount => _toasts.Count;

        protected override Visual GetChild(int index) => _toasts[index];

        protected override SizeHints MeasureCore(in LayoutConstraints constraints)
        {
            var width = 0;
            var height = 0;
            var spacing = Math.Max(0, Spacing);

            for (var i = 0; i < _toasts.Count; i++)
            {
                var toast = _toasts[i];
                var hints = toast.Measure(constraints);

                width = Math.Max(width, hints.Natural.Width);
                height += hints.Natural.Height;
                if (i > 0)
                {
                    height += spacing;
                }
            }

            var natural = constraints.Clamp(new Size(width, height));
            return SizeHints.Fixed(natural);
        }

        protected override void ArrangeCore(in Rectangle finalRect)
        {
            var spacing = Math.Max(0, Spacing);
            if (_toasts.Count == 0 || finalRect.Width <= 0 || finalRect.Height <= 0)
            {
                return;
            }

            var stackDown = Position is ToastPosition.TopLeft or ToastPosition.TopCenter or ToastPosition.TopRight;

            if (stackDown)
            {
                var y = finalRect.Y;
                for (var i = 0; i < _toasts.Count; i++)
                {
                    var toast = _toasts[i];
                    var height = toast.DesiredSize.Height;
                    toast.Arrange(new Rectangle(finalRect.X, y, finalRect.Width, height));
                    y += height + spacing;
                }
            }
            else
            {
                var y = finalRect.Bottom;
                for (var i = 0; i < _toasts.Count; i++)
                {
                    var toast = _toasts[i];
                    var height = toast.DesiredSize.Height;
                    y -= height;
                    toast.Arrange(new Rectangle(finalRect.X, y, finalRect.Width, height));
                    y -= spacing;
                }
            }
        }
    }
}
