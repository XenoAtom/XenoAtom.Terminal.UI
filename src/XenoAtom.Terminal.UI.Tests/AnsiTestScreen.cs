// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;

namespace XenoAtom.Terminal.UI.Tests;

internal sealed class AnsiTestScreen
{
    private readonly int _width;
    private readonly int _height;
    private readonly char[] _cells;

    private int _row;
    private int _col;
    private int _savedRow;
    private int _savedCol;
    private bool _hasSaved;

    public AnsiTestScreen(int width, int height)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        _width = width;
        _height = height;
        _cells = new char[width * height];
        ClearAll();
    }

    public void Apply(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        var i = 0;
        while (i < text.Length)
        {
            var ch = text[i++];
            if (ch != '\x1b')
            {
                WriteChar(ch);
                continue;
            }

            if (i >= text.Length)
            {
                break;
            }

            if (text[i] != '[')
            {
                continue;
            }

            i++; // skip '['

            if (i < text.Length && text[i] == '?')
            {
                i++; // private mode prefix
            }

            var p0 = -1;
            var p1 = -1;
            var paramIndex = 0;
            var current = 0;
            var hasNumber = false;

            while (i < text.Length)
            {
                ch = text[i++];

                if (ch >= '0' && ch <= '9')
                {
                    current = (current * 10) + (ch - '0');
                    hasNumber = true;
                    continue;
                }

                if (ch == ';')
                {
                    SetParam(ref p0, ref p1, ref paramIndex, current, hasNumber);
                    current = 0;
                    hasNumber = false;
                    continue;
                }

                SetParam(ref p0, ref p1, ref paramIndex, current, hasNumber);
                ExecuteCsi(ch, p0, p1);
                break;
            }
        }
    }

    public string GetText()
    {
        var sb = new StringBuilder(_height * (_width + 1));
        for (var row = 0; row < _height; row++)
        {
            if (row > 0)
            {
                sb.AppendLine();
            }

            sb.Append(_cells, row * _width, _width);
        }

        return sb.ToString();
    }

    private void ExecuteCsi(char final, int p0, int p1)
    {
        switch (final)
        {
            case 'H':
            case 'f':
            {
                var row = (p0 <= 0 ? 1 : p0) - 1;
                var col = (p1 <= 0 ? 1 : p1) - 1;
                _row = Math.Clamp(row, 0, _height - 1);
                _col = Math.Clamp(col, 0, _width - 1);
                break;
            }
            case 'G':
            {
                var col = (p0 <= 0 ? 1 : p0) - 1;
                _col = Math.Clamp(col, 0, _width - 1);
                break;
            }
            case 'E':
            {
                var n = p0 <= 0 ? 1 : p0;
                _row = Math.Clamp(_row + n, 0, _height - 1);
                _col = 0;
                break;
            }
            case 'F':
            {
                var n = p0 <= 0 ? 1 : p0;
                _row = Math.Clamp(_row - n, 0, _height - 1);
                _col = 0;
                break;
            }
            case 'A':
            {
                var n = p0 <= 0 ? 1 : p0;
                _row = Math.Clamp(_row - n, 0, _height - 1);
                break;
            }
            case 'B':
            {
                var n = p0 <= 0 ? 1 : p0;
                _row = Math.Clamp(_row + n, 0, _height - 1);
                break;
            }
            case 'C':
            {
                var n = p0 <= 0 ? 1 : p0;
                _col = Math.Clamp(_col + n, 0, _width - 1);
                break;
            }
            case 'D':
            {
                var n = p0 <= 0 ? 1 : p0;
                _col = Math.Clamp(_col - n, 0, _width - 1);
                break;
            }
            case 'J':
            {
                if (p0 == 2)
                {
                    ClearAll();
                }
                break;
            }
            case 'K':
            {
                switch (p0)
                {
                    case 2:
                        ClearLine(_row);
                        break;
                    case 0:
                        ClearLineFrom(_row, _col);
                        break;
                }
                break;
            }
            case 's':
                _savedRow = _row;
                _savedCol = _col;
                _hasSaved = true;
                break;
            case 'u':
                if (_hasSaved)
                {
                    _row = _savedRow;
                    _col = _savedCol;
                }
                break;
            case 'm':
            default:
                break;
        }
    }

    private void WriteChar(char ch)
    {
        if (ch == '\r')
        {
            _col = 0;
            return;
        }

        if (ch == '\n')
        {
            _row = Math.Clamp(_row + 1, 0, _height - 1);
            _col = 0;
            return;
        }

        if ((uint)_row >= (uint)_height || (uint)_col >= (uint)_width)
        {
            return;
        }

        _cells[(_row * _width) + _col] = ch;
        _col = Math.Min(_width - 1, _col + 1);
    }

    private void ClearAll()
    {
        Array.Fill(_cells, ' ');
    }

    private void ClearLine(int row)
    {
        Array.Fill(_cells, ' ', row * _width, _width);
    }

    private void ClearLineFrom(int row, int col)
    {
        Array.Fill(_cells, ' ', (row * _width) + col, _width - col);
    }

    private static void SetParam(ref int p0, ref int p1, ref int paramIndex, int current, bool hasNumber)
    {
        if (!hasNumber)
        {
            current = -1;
        }

        if (paramIndex == 0)
        {
            p0 = current;
        }
        else if (paramIndex == 1)
        {
            p1 = current;
        }

        paramIndex++;
    }
}

