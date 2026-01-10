// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Input;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Controls;

public sealed partial class RadioButton : Visual
{
    private bool _isPressed;

    public RadioButton()
    {
        Focusable = true;
    }

    public RadioButton(string text, object? group = null, bool isChecked = false) : this()
    {
        Text = text;
        Group = group;
        IsChecked = isChecked;
    }

    [Bindable]
    public partial Visual? Text { get; set; }

    [Bindable]
    public partial bool IsChecked { get; set; }

    [Bindable]
    public partial object? Group { get; set; }

    protected override int ChildrenCount => _text is null ? 0 : 1;

    protected override Visual GetChild(int index)
        => index == 0 && _text is not null ? _text : throw new ArgumentOutOfRangeException(nameof(index));

    protected override Size MeasureOverride(Size availableSize)
    {
        var textWidth = 0;
        var textVisual = Text;
        if (textVisual is not null)
        {
            textVisual.Measure(new Size(LayoutConstants.Infinite, 1));
            textWidth = textVisual.DesiredSize.Width;
        }

        var width = Math.Min(availableSize.Width, textWidth + 4);
        return new Size(width, 1);
    }

    protected override void ArrangeOverride(Rectangle finalRect)
    {
        Bounds = finalRect;

        var textVisual = Text;
        if (textVisual is null)
        {
            return;
        }

        var textX = finalRect.X + 2;
        var available = Math.Max(0, finalRect.Width - 2);
        var desired = Math.Min(available, textVisual.DesiredSize.Width);
        textVisual.Arrange(new Rectangle(textX, finalRect.Y, desired, 1));
    }

    protected override void RenderOverride(CellBuffer buffer)
    {
        var isFocused = ReferenceEquals(App?.FocusedElement, this);
        var theme = GetTheme();
        var radioStyle = Get<RadioButtonStyle>();
        var style = radioStyle.Resolve(theme, IsEnabled, isFocused, IsHovered);

        var rect = Bounds;

        for (var x = rect.X; x < rect.X + rect.Width; x++)
        {
            buffer.SetCell(x, rect.Y, new Rune(' '), style);
        }

        var glyph = IsChecked ? radioStyle.CheckedGlyph : radioStyle.UncheckedGlyph;
        buffer.SetCell(rect.X, rect.Y, glyph, style | TextStyle.Bold);

        if (_text is not null && rect.Width > 1)
        {
            buffer.SetCell(rect.X + 1, rect.Y, new Rune(' '), style);
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key is TerminalKey.Space or TerminalKey.Enter)
        {
            SetCheckedFromUser();
            e.Handled = true;
        }
    }

    protected override void OnPointerPressed(PointerEventArgs e)
    {
        if (e.Button != TerminalMouseButton.Left)
        {
            return;
        }

        _isPressed = true;
        e.Handled = true;
    }

    protected override void OnPointerReleased(PointerEventArgs e)
    {
        if (e.Button != TerminalMouseButton.Left)
        {
            return;
        }

        if (_isPressed)
        {
            _isPressed = false;

            if (e.LocalX >= 0 && e.LocalX < Bounds.Width && e.LocalY >= 0 && e.LocalY < Bounds.Height)
            {
                SetCheckedFromUser();
            }

            e.Handled = true;
        }
    }

    private void SetCheckedFromUser()
    {
        if (IsChecked)
        {
            return;
        }

        IsChecked = true;
        UncheckOthersInGroup();
    }

    private void UncheckOthersInGroup()
    {
        var group = Group;
        if (group is null)
        {
            return;
        }

        Visual root = this;
        while (root.Parent is not null)
        {
            root = root.Parent;
        }

        foreach (var v in root.EnumerateVisualsDepthFirst())
        {
            if (!ReferenceEquals(v, this) && v is RadioButton { IsChecked: true } radio && Equals(radio.Group, group))
            {
                radio.IsChecked = false;
            }
        }
    }
}
