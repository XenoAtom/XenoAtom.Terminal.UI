// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;
using XenoAtom.Terminal;

namespace XenoAtom.Terminal.UI;

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
    public partial string? Text { get; set; }

    [Bindable]
    public partial bool IsChecked { get; set; }

    [Bindable]
    public partial object? Group { get; set; }

    protected override CellSize MeasureOverride(CellSize availableSize)
    {
        var text = Text ?? string.Empty;
        var width = Math.Min(availableSize.Width, TerminalTextUtility.GetWidth(text.AsSpan()) + 4);
        return new CellSize(width, 1);
    }

    protected override void ArrangeOverride(CellRect finalRect)
    {
        Bounds = finalRect;
    }

    protected override void RenderOverride(CellBuffer buffer)
    {
        var isFocused = ReferenceEquals(App?.FocusedElement, this);
        var theme = GetTheme();
        var radioStyle = GetEnvironmentValue(RadioButtonStyle.Key);
        var style = radioStyle.Resolve(theme, IsEnabled, isFocused, IsHovered);

        var rect = Bounds;
        var text = Text ?? string.Empty;

        for (var x = rect.X; x < rect.X + rect.Width; x++)
        {
            buffer.SetCell(x, rect.Y, new Rune(' '), style);
        }

        var glyph = IsChecked ? radioStyle.CheckedGlyph : radioStyle.UncheckedGlyph;
        buffer.SetCell(rect.X, rect.Y, new Rune(glyph), style | CellStyle.Bold);
        buffer.WriteText(rect.X + 2, rect.Y, text.AsSpan(), style);
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
        App?.RequestRender();
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
            App?.RequestRender();

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

