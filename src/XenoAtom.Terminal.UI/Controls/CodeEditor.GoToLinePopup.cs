// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Globalization;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Input;
using XenoAtom.Terminal.UI.Layout;
using XenoAtom.Terminal.UI.Styling;
using XenoAtom.Terminal.UI.Threading;

namespace XenoAtom.Terminal.UI.Controls;

internal sealed class CodeEditorGoToLinePopup
{
    private readonly CodeEditor _owner;
    private readonly CodeEditorGoToLineConfig _config;
    private readonly NumberBox<int> _lineBox;
    private readonly HStack _content;
    private Popup? _popup;
    private Rectangle _hostRect;
    private int _restoreCaretIndex;
    private bool _restoreCaretOnClose;

    public CodeEditorGoToLinePopup(CodeEditor owner, CodeEditorGoToLineConfig config)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(config);

        _owner = owner;
        _config = config;

        _lineBox = new NumberBox<int>()
            .ShowValidationMessage(false)
            .ParseStyles(NumberStyles.Integer)
            .HorizontalAlignment(Align.Stretch)
            .MinWidth(6)
            .MaxWidth(10);
        _lineBox.KeyDown(OnLineBoxKeyDown);

        _content = new HStack(new TextBlock(config.PromptText), _lineBox)
        {
            Spacing = 1,
            HorizontalAlignment = Align.Start,
            VerticalAlignment = Align.Start,
        };
    }

    public bool IsOpen => _popup is not null && _popup.Parent is not null;

    public bool Open()
    {
        _owner.VerifyAccess();

        _restoreCaretIndex = _owner.CaretIndex;
        _restoreCaretOnClose = false;
        _lineBox.Value = _owner.Line;

        var popup = EnsurePopup();
        if (popup is null)
        {
            return false;
        }

        if (popup.Parent is not null)
        {
            UpdateAnchorRect();
            FocusAndSelectLineBox();
            return true;
        }

        try
        {
            popup.Show();
            FocusAndSelectLineBox();
        }
        catch (InvalidOperationException)
        {
            return false;
        }

        return true;
    }

    public void ArrangeWithin(in Rectangle hostRect)
    {
        _hostRect = hostRect;
        UpdateAnchorRect();
    }

    private void OnLineBoxKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == TerminalKey.Enter)
        {
            if (TryGetRequestedLine(out var line))
            {
                PostAfterInput(() =>
                {
                    _restoreCaretOnClose = false;
                    _popup?.Close();
                    _owner.GoToLine(line);
                });
            }

            e.Handled = true;
            return;
        }

        if (e.Key == TerminalKey.Escape)
        {
            PostAfterInput(() =>
            {
                _restoreCaretOnClose = true;
                _popup?.Close();
            });
            e.Handled = true;
        }
    }

    private bool TryGetRequestedLine(out int line)
    {
        var text = _lineBox.Text;
        if (string.IsNullOrWhiteSpace(text))
        {
            line = default;
            return false;
        }

        return int.TryParse(
            text,
            _lineBox.ParseStyles,
            _lineBox.FormatProvider ?? CultureInfo.CurrentCulture,
            out line);
    }

    private void UpdateAnchorRect()
    {
        if (_popup is not null)
        {
            _popup.AnchorRect = ResolveAnchorRect();
        }
    }

    private Popup? EnsurePopup()
    {
        if (_popup is not null)
        {
            _popup.AnchorRect = ResolveAnchorRect();
            return _popup;
        }

        var popup = new Popup
        {
            Anchor = _owner,
            AnchorRect = ResolveAnchorRect(),
            Content = _content,
            MatchAnchorWidth = false,
            Placement = PopupPlacement.Below,
            CloseOnTab = false,
            RestoreFocusTarget = _owner,
        };

        popup.Closed((_, _) =>
        {
            if (_restoreCaretOnClose)
            {
                _owner.GoToPosition(_restoreCaretIndex);
                _restoreCaretOnClose = false;
            }
        });

        _popup = popup;
        return popup;
    }

    private Rectangle ResolveAnchorRect()
    {
        var hostRect = _hostRect;
        if (hostRect.Width <= 0 || hostRect.Height <= 0)
        {
            return new Rectangle(hostRect.X, hostRect.Y, 0, 0);
        }

        var popupSize = MeasurePopupSize(hostRect);
        var left = ResolveAlignedCoordinate(
            hostRect.X,
            hostRect.Width,
            popupSize.Width,
            _config.PopupHorizontalAlignment,
            _config.PopupOffsetX);
        var top = ResolveAlignedCoordinate(
            hostRect.Y,
            hostRect.Height,
            popupSize.Height,
            _config.PopupVerticalAlignment,
            _config.PopupOffsetY);

        left = Math.Clamp(left, hostRect.X, Math.Max(hostRect.X, hostRect.Right - popupSize.Width));
        top = Math.Clamp(top, hostRect.Y, Math.Max(hostRect.Y, hostRect.Bottom - popupSize.Height));
        return new Rectangle(left, top, 0, 0);
    }

    private Size MeasurePopupSize(in Rectangle hostRect)
    {
        var popupStyle = _owner.GetStyle<PopupStyle>();
        var padding = popupStyle.Padding;

        var maxContentWidth = Math.Max(1, hostRect.Width - padding.Horizontal);
        var maxContentHeight = Math.Max(1, hostRect.Height - padding.Vertical);
        _content.Measure(new LayoutConstraints(0, maxContentWidth, 0, maxContentHeight));

        var width = Math.Clamp(Math.Max(1, padding.Horizontal + _content.DesiredSize.Width), 1, Math.Max(1, hostRect.Width));
        var height = Math.Clamp(Math.Max(1, padding.Vertical + _content.DesiredSize.Height), 1, Math.Max(1, hostRect.Height));
        return new Size(width, height);
    }

    private static int ResolveAlignedCoordinate(int start, int available, int size, Align alignment, int offset)
    {
        var maxOffset = Math.Max(0, available - size);
        var position = alignment switch
        {
            Align.End => maxOffset,
            Align.Center => maxOffset / 2,
            _ => 0,
        };

        return start + position + offset;
    }

    private void PostAfterInput(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        var app = _owner.App ?? _owner.Dispatcher.AttachedApp;
        if (app is not null)
        {
            app.Post(action);
            return;
        }

        action();
    }

    private void FocusAndSelectLineBox()
    {
        (_owner.App ?? _owner.Dispatcher.AttachedApp)?.Focus(_lineBox);
        _lineBox.SelectAllText();
    }
}
