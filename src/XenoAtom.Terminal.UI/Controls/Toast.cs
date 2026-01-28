// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Input;
using XenoAtom.Terminal.UI.Layout;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Controls;

/// <summary>
/// Defines the semantic importance of a toast notification.
/// </summary>
public enum ToastSeverity
{
    /// <summary>Neutral informational message.</summary>
    Info,
    /// <summary>Successful operation feedback.</summary>
    Success,
    /// <summary>Warning that doesn't block operation.</summary>
    Warning,
    /// <summary>Error notification (non-fatal).</summary>
    Error,
}

/// <summary>
/// Specifies the screen corner used to anchor toast notifications.
/// </summary>
public enum ToastPosition
{
    /// <summary>Top-right corner.</summary>
    TopRight,
    /// <summary>Top-left corner.</summary>
    TopLeft,
    /// <summary>Top-center.</summary>
    TopCenter,
    /// <summary>Bottom-right corner.</summary>
    BottomRight,
    /// <summary>Bottom-left corner.</summary>
    BottomLeft,
    /// <summary>Bottom-center.</summary>
    BottomCenter,
}

/// <summary>
/// Represents an individual toast notification.
/// </summary>
public partial class Toast : Visual
{
    private readonly Border _border;
    private readonly VStack _stack;
    private readonly Grid _header;
    private readonly Padder _iconPad;
    private readonly TextBlock _iconBlock;
    private readonly Padder _titleHost;
    private readonly GridCell _iconCell;
    private readonly GridCell _titleCell;
    private readonly GridCell _closeCell;
    private readonly Button _closeButton;
    private readonly HStack _actionRow;
    private readonly ProgressBar _progressBar;

    private bool _durationOverride;
    private ToastHost? _host;
    private Visual? _lastActionContent;
    private Button? _actionButton;
    private EventHandler<ClickEventArgs>? _actionHandler;
    private EventHandler<ClickEventArgs>? _closeHandler;
    private TextBlockStyle? _iconTextStyle;
    private TextBlockStyle? _titleTextStyle;
    private Style? _lastContainerStyle;

    /// <summary>
    /// Initializes a new instance of the <see cref="Toast"/> class.
    /// </summary>
    public Toast()
    {
        Focusable = false;
        ShowIcon = true;
        ShowCloseButton = true;

        _iconBlock = new TextBlock();
        _iconPad = new Padder(_iconBlock);

        _titleHost = new Padder();

        _closeButton = new ToastButton();
        _actionRow = new HStack().Spacing(1);
        _progressBar = new ProgressBar().HorizontalAlignment(Align.Stretch);

        _header = new Grid();
        _header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        _header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star(1) });
        _header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        _header.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        _iconCell = new GridCell { Row = 0, Column = 0 };
        _titleCell = new GridCell { Row = 0, Column = 1 };
        _closeCell = new GridCell { Row = 0, Column = 2 };
        _header.Cells.Add(_iconCell);
        _header.Cells.Add(_titleCell);
        _header.Cells.Add(_closeCell);

        _stack = new VStack().Spacing(1).HorizontalAlignment(Align.Stretch);

        _border = new Border(_stack);
        AttachChild(_border);
    }

    /// <summary>
    /// Gets or sets the optional title visual.
    /// </summary>
    [Bindable(NoVisualAttach = true)]
    public partial Visual? Title { get; set; }

    /// <summary>
    /// Gets or sets the main content visual.
    /// </summary>
    [Bindable(NoVisualAttach = true)]
    public partial Visual? Content { get; set; }

    /// <summary>
    /// Gets or sets the optional action content.
    /// </summary>
    [Bindable(NoVisualAttach = true)]
    public partial Visual? Action { get; set; }

    /// <summary>
    /// Gets or sets the toast severity.
    /// </summary>
    [Bindable]
    public partial ToastSeverity Severity { get; set; }

    /// <summary>
    /// Gets or sets the auto-dismiss duration, or <c>null</c> for persistent toasts.
    /// </summary>
    [Bindable]
    public partial TimeSpan? Duration { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to show the severity icon.
    /// </summary>
    [Bindable]
    public partial bool ShowIcon { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to show a close button.
    /// </summary>
    [Bindable]
    public partial bool ShowCloseButton { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to show the countdown progress bar.
    /// </summary>
    [Bindable]
    public partial bool ShowProgress { get; set; }

    [Bindable]
    internal partial double CountdownProgress { get; set; }

    internal bool HasDurationOverride => _durationOverride;

    /// <summary>
    /// Dismisses the toast programmatically.
    /// </summary>
    public void Dismiss()
    {
        _host?.RequestDismiss(this, ToastDismissReason.Programmatic);
    }

    /// <summary>
    /// Resets the auto-dismiss timer to its full duration.
    /// </summary>
    public void ResetTimer() => _host?.ResetTimer(this);

    /// <summary>
    /// Pauses the auto-dismiss timer.
    /// </summary>
    public void PauseTimer() => _host?.PauseTimer(this);

    /// <summary>
    /// Resumes the auto-dismiss timer.
    /// </summary>
    public void ResumeTimer() => _host?.ResumeTimer(this);

    internal void AttachHost(ToastHost? host) => _host = host;

    internal void RaiseDismissed(ToastDismissReason reason)
        => RaiseEvent(DismissedEvent, new ToastDismissedEventArgs(reason));

    internal void InvokeAction()
    {
        var args = new ToastActionEventArgs();
        RaiseEvent(ActionInvokedEvent, args);
        if (!args.KeepOpen)
        {
            _host?.RequestDismiss(this, ToastDismissReason.ActionInvoked);
        }
    }

    /// <inheritdoc />
    protected override int ChildrenCount => 1;

    /// <inheritdoc />
    protected override Visual GetChild(int index)
        => index == 0 ? _border : throw new ArgumentOutOfRangeException(nameof(index));

    /// <inheritdoc />
    protected override SizeHints MeasureCore(in LayoutConstraints constraints)
    {
        var style = GetStyle<ToastStyle>();

        var minWidth = Math.Max(0, style.MinWidth);
        var maxWidth = Math.Max(minWidth, style.MaxWidth);
        if (maxWidth <= 0)
        {
            maxWidth = minWidth;
        }

        var maxConstraint = constraints.MaxWidth == LayoutConstants.Infinite
            ? maxWidth
            : Math.Min(constraints.MaxWidth, maxWidth);

        var minConstraint = Math.Max(constraints.MinWidth, minWidth);
        if (maxConstraint < minConstraint)
        {
            minConstraint = maxConstraint;
        }

        var innerConstraints = new LayoutConstraints(minConstraint, maxConstraint, constraints.MinHeight, constraints.MaxHeight);
        var hints = _border.Measure(innerConstraints);

        var min = hints.Min;
        if (min.Width < minConstraint)
        {
            min = min with { Width = minConstraint };
        }

        var naturalWidth = Math.Max(hints.Natural.Width, min.Width);
        if (maxConstraint != LayoutConstants.Infinite)
        {
            naturalWidth = Math.Min(naturalWidth, maxConstraint);
        }

        var maxWidthHint = hints.Max.Width;
        if (maxConstraint != LayoutConstants.Infinite)
        {
            maxWidthHint = maxWidthHint == LayoutConstants.Infinite
                ? maxConstraint
                : Math.Min(maxWidthHint, maxConstraint);
        }

        var max = hints.Max with { Width = maxWidthHint };
        var natural = hints.Natural with { Width = naturalWidth };

        return hints with { Min = min, Natural = natural, Max = max };
    }

    /// <inheritdoc />
    protected override void PrepareChildren()
    {
        base.PrepareChildren();

        var title = Title;
        var content = Content;
        var action = Action;
        var showIcon = ShowIcon;
        var showClose = ShowCloseButton;
        var showProgress = ShowProgress;
        var severity = Severity;
        var progress = CountdownProgress;

        var theme = GetTheme();
        var style = GetStyle<ToastStyle>();

        _border.Padding = style.Padding;

        var baseStyle = style.ResolveStyle(theme, severity);
        if (_lastContainerStyle != baseStyle)
        {
            StyleEnvironment ??= new Dictionary<object, object?>();
            StyleEnvironment[TextBlockStyle.Key] = CreateTextBlockStyle(baseStyle);
            _lastContainerStyle = baseStyle;
        }

        var borderStyle = style.BorderStyle;
        if (borderStyle.BackgroundStyle is null)
        {
            borderStyle = borderStyle with { BackgroundStyle = baseStyle };
        }

        var resolvedBorder = style.ResolveBorderStyle(theme, severity);
        if (borderStyle.BorderCellStyle is null)
        {
            borderStyle = borderStyle with { BorderCellStyle = resolvedBorder };
        }

        if (borderStyle.FocusedBorderCellStyle is null)
        {
            borderStyle = borderStyle with { FocusedBorderCellStyle = resolvedBorder };
        }

        _border.Style(borderStyle);

        _progressBar.Style(style.ProgressStyle);
        _progressBar.Value(progress);

        var iconRune = severity switch
        {
            ToastSeverity.Success => style.SuccessIcon,
            ToastSeverity.Warning => style.WarningIcon,
            ToastSeverity.Error => style.ErrorIcon,
            _ => style.InfoIcon,
        };

        _iconBlock.Text = iconRune.ToString();
        _iconPad.Padding = new Thickness(0, 0, Math.Max(0, style.IconSpacing), 0);

        var iconStyle = CreateTextBlockStyle(style.ResolveIconStyle(theme, severity));
        if (_iconTextStyle != iconStyle)
        {
            _iconBlock.Style(iconStyle);
            _iconTextStyle = iconStyle;
        }

        var titleStyle = CreateTextBlockStyle(style.ResolveTitleStyle(theme, severity));
        if (_titleTextStyle != titleStyle)
        {
            _titleHost.StyleEnvironment ??= new Dictionary<object, object?>();
            _titleHost.StyleEnvironment[TextBlockStyle.Key] = titleStyle;
            _titleTextStyle = titleStyle;
        }

        _closeButton.Content = style.CloseIcon.ToString();
        _closeButton.Style(ButtonStyle.Default with
        {
            Padding = Thickness.Zero,
            ShowBorder = false,
            Normal = Style.None,
            Hovered = Style.None,
            Pressed = Style.None,
            Focused = Style.None,
        });

        _actionRow.Children.Clear();
        var actionVisual = PrepareActionVisual(action);
        if (actionVisual is not null)
        {
            _actionRow.Children.Add(actionVisual);
        }

        _iconCell.Content = showIcon ? _iconPad : null;

        _titleHost.Content = title;
        _titleCell.Content = title is null ? null : _titleHost;

        if (showClose)
        {
            EnsureCloseHandler();
            _closeCell.Content = _closeButton;
        }
        else
        {
            RemoveCloseHandler();
            _closeCell.Content = null;
        }

        _stack.Children.Clear();

        if (_iconCell.Content is not null || _titleCell.Content is not null || _closeCell.Content is not null)
        {
            _stack.Children.Add(_header);
        }

        if (content is not null)
        {
            _stack.Children.Add(content);
        }

        if (actionVisual is not null)
        {
            _stack.Children.Add(_actionRow);
        }

        if (showProgress)
        {
            _stack.Children.Add(_progressBar);
        }
    }

    private Visual? PrepareActionVisual(Visual? action)
    {
        if (ReferenceEquals(_lastActionContent, action))
        {
            return _actionButton;
        }

        if (_actionButton is not null)
        {
            RemoveActionHandler(_actionButton);
        }

        _lastActionContent = action;
        if (action is null)
        {
            _actionButton = null;
            return null;
        }

        if (action is Button button)
        {
            _actionButton = button;
            EnsureActionHandler(button);
            return button;
        }

        _actionButton = new ToastButton(action);
        EnsureActionHandler(_actionButton);
        return _actionButton;
    }

    private sealed class ToastButton : Button
    {
        public ToastButton()
        {
            Focusable = false;
        }

        public ToastButton(Visual content) : base(content)
        {
            Focusable = false;
        }
    }

    private void EnsureActionHandler(Button button)
    {
        _actionHandler ??= (_, _) => InvokeAction();
        button.ClickRouted += _actionHandler;
    }

    private void RemoveActionHandler(Button button)
    {
        if (_actionHandler is not null)
        {
            button.ClickRouted -= _actionHandler;
        }
    }

    private void EnsureCloseHandler()
    {
        _closeHandler ??= (_, _) => _host?.RequestDismiss(this, ToastDismissReason.UserClosed);
        _closeButton.ClickRouted += _closeHandler;
    }

    private void RemoveCloseHandler()
    {
        if (_closeHandler is not null)
        {
            _closeButton.ClickRouted -= _closeHandler;
        }
    }

    private static TextBlockStyle CreateTextBlockStyle(Style style)
    {
        var textStyle = style.TextStyle;
        var hasForeground = style.TryGetForeground(out var fg);
        var hasBackground = style.TryGetBackground(out var bg);

        return new TextBlockStyle
        {
            Foreground = hasForeground ? fg : null,
            Background = hasBackground ? bg : null,
            TextStyle = textStyle,
        };
    }

    partial void OnDurationChanging(ref TimeSpan? value) => _durationOverride = true;

    /// <summary>
    /// Called when the toast is dismissed.
    /// </summary>
    /// <param name="e">The dismissal event data.</param>
    [RoutedEvent(RoutingStrategy.Bubble)]
    protected virtual void OnDismissed(ToastDismissedEventArgs e) { }

    /// <summary>
    /// Called when the toast action is invoked.
    /// </summary>
    /// <param name="e">The action event data.</param>
    [RoutedEvent(RoutingStrategy.Bubble)]
    protected virtual void OnActionInvoked(ToastActionEventArgs e) { }
}

/// <summary>
/// Describes why a toast was dismissed.
/// </summary>
public enum ToastDismissReason
{
    /// <summary>Auto-dismissed after duration elapsed.</summary>
    Timeout,
    /// <summary>User clicked close button.</summary>
    UserClosed,
    /// <summary>Dismissed programmatically.</summary>
    Programmatic,
    /// <summary>Dismissed to make room for newer toasts.</summary>
    Overflow,
    /// <summary>Dismissed after invoking the action.</summary>
    ActionInvoked,
}

/// <summary>
/// Provides data for the <see cref="Toast.DismissedEvent"/> event.
/// </summary>
public sealed class ToastDismissedEventArgs : RoutedEventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ToastDismissedEventArgs"/> class.
    /// </summary>
    /// <param name="reason">The dismissal reason.</param>
    public ToastDismissedEventArgs(ToastDismissReason reason)
    {
        Reason = reason;
    }

    /// <summary>
    /// Gets the reason the toast was dismissed.
    /// </summary>
    public ToastDismissReason Reason { get; }
}

/// <summary>
/// Provides data for the <see cref="Toast.ActionInvokedEvent"/> event.
/// </summary>
public sealed class ToastActionEventArgs : RoutedEventArgs
{
    /// <summary>
    /// Gets or sets a value indicating whether the toast should remain open after the action runs.
    /// </summary>
    public bool KeepOpen { get; set; }
}
