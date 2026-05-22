// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Controls;

/// <summary>
/// Represents a tab page with a header and a content visual.
/// </summary>
public sealed partial record class TabPage : IVisualElement
{
    private TabControl? _owner;
    private Visual? _oldHeader;
    private Visual? _oldContent;

    /// <summary>
    /// Initializes a new instance of the <see cref="TabPage"/> class.
    /// </summary>
    /// <param name="header">The tab header visual.</param>
    /// <param name="content">The tab content visual.</param>
    public TabPage(Visual header, Visual content)
    {
        Header = header ?? throw new ArgumentNullException(nameof(header));
        Content = content ?? throw new ArgumentNullException(nameof(content));
        IsEnabled = true;
    }

    /// <summary>
    /// Raised when the owning tab control requests to close this tab.
    /// </summary>
    public event EventHandler<TabPageClosingEventArgs>? RequestClosing;

    /// <summary>
    /// Raised after this tab has been closed and removed from its owning tab control.
    /// </summary>
    public event EventHandler<TabPageClosedEventArgs>? Closed;

    /// <summary>
    /// Gets the owning <see cref="TerminalApp"/> if attached.
    /// </summary>
    /// <remarks>
    /// This property must not depend on bindable properties such as <see cref="Header"/> to avoid recursive binding reads.
    /// </remarks>
    public TerminalApp? App => _owner?.App;

    /// <summary>
    /// Gets or sets the tab header visual.
    /// </summary>
    [Bindable]
    public partial Visual Header { get; set; }

    /// <summary>
    /// Gets or sets the tab content visual.
    /// </summary>
    [Bindable]
    public partial Visual Content { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this tab can be selected or closed through the UI.
    /// </summary>
    [Bindable]
    public partial bool IsEnabled { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether a close button is rendered for this tab.
    /// </summary>
    [Bindable]
    public partial bool ShowCloseButton { get; set; }

    /// <summary>
    /// Gets or sets arbitrary data associated with this tab.
    /// </summary>
    [Bindable]
    public partial object? Data { get; set; }

    partial void OnHeaderChanging(ref Visual value)
    {
        ArgumentNullException.ThrowIfNull(value);
        _owner?.ValidateReplacementVisual(value, "tab header");
        _oldHeader = _header;
    }

    partial void OnHeaderChanged(Visual value)
    {
        _owner?.OnPageHeaderChanged(this, _oldHeader, value);
        _oldHeader = null;
    }

    partial void OnContentChanging(ref Visual value)
    {
        ArgumentNullException.ThrowIfNull(value);
        _owner?.ValidateReplacementVisual(value, "tab content");
        _oldContent = _content;
    }

    partial void OnContentChanged(Visual value)
    {
        _owner?.OnPageContentChanged(this, _oldContent, value);
        _oldContent = null;
    }

    internal void Attach(TabControl owner)
    {
        ArgumentNullException.ThrowIfNull(owner);
        if (_owner is not null && !ReferenceEquals(_owner, owner))
        {
            throw new InvalidOperationException("The tab page already belongs to another tab control.");
        }

        _owner = owner;
    }

    internal void Detach(TabControl owner)
    {
        if (ReferenceEquals(_owner, owner))
        {
            _owner = null;
        }
    }

    internal bool RaiseRequestClosing(TabControl owner, int index, TabCloseReason reason)
    {
        ArgumentNullException.ThrowIfNull(owner);

        var args = new TabPageClosingEventArgs(owner, this, index, reason);
        RequestClosing?.Invoke(this, args);
        return !args.Cancel;
    }

    internal void RaiseClosed(TabControl owner, int index, TabCloseReason reason)
    {
        ArgumentNullException.ThrowIfNull(owner);
        Closed?.Invoke(this, new TabPageClosedEventArgs(owner, this, index, reason));
    }
}

/// <summary>
/// Specifies why a tab page is being closed.
/// </summary>
public enum TabCloseReason
{
    /// <summary>
    /// The close request was initiated programmatically.
    /// </summary>
    Programmatic,

    /// <summary>
    /// The close request originated from the tab header close button.
    /// </summary>
    CloseButton,

    /// <summary>
    /// The close request originated from a middle-click on the tab header.
    /// </summary>
    MiddleClick,
}

/// <summary>
/// Provides data for a tab page close request.
/// </summary>
public sealed class TabPageClosingEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TabPageClosingEventArgs"/> class.
    /// </summary>
    /// <param name="owner">The owning tab control.</param>
    /// <param name="page">The page being closed.</param>
    /// <param name="index">The page index at the time of the request.</param>
    /// <param name="reason">The reason for the close request.</param>
    public TabPageClosingEventArgs(TabControl owner, TabPage page, int index, TabCloseReason reason)
    {
        Owner = owner ?? throw new ArgumentNullException(nameof(owner));
        Page = page ?? throw new ArgumentNullException(nameof(page));
        Index = index;
        Reason = reason;
    }

    /// <summary>
    /// Gets the owning tab control.
    /// </summary>
    public TabControl Owner { get; }

    /// <summary>
    /// Gets the tab page being closed.
    /// </summary>
    public TabPage Page { get; }

    /// <summary>
    /// Gets the page index at the time of the close request.
    /// </summary>
    public int Index { get; }

    /// <summary>
    /// Gets the reason for the close request.
    /// </summary>
    public TabCloseReason Reason { get; }

    /// <summary>
    /// Gets or sets a value indicating whether the close request should be canceled.
    /// </summary>
    public bool Cancel { get; set; }
}

/// <summary>
/// Provides data for a completed tab page close operation.
/// </summary>
public sealed class TabPageClosedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TabPageClosedEventArgs"/> class.
    /// </summary>
    /// <param name="owner">The owning tab control.</param>
    /// <param name="page">The page that was closed.</param>
    /// <param name="index">The page index before it was removed.</param>
    /// <param name="reason">The reason the page was closed.</param>
    public TabPageClosedEventArgs(TabControl owner, TabPage page, int index, TabCloseReason reason)
    {
        Owner = owner ?? throw new ArgumentNullException(nameof(owner));
        Page = page ?? throw new ArgumentNullException(nameof(page));
        Index = index;
        Reason = reason;
    }

    /// <summary>
    /// Gets the owning tab control.
    /// </summary>
    public TabControl Owner { get; }

    /// <summary>
    /// Gets the closed tab page.
    /// </summary>
    public TabPage Page { get; }

    /// <summary>
    /// Gets the page index before it was removed.
    /// </summary>
    public int Index { get; }

    /// <summary>
    /// Gets the reason the page was closed.
    /// </summary>
    public TabCloseReason Reason { get; }
}
