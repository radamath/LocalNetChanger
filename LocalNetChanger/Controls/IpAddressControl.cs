namespace LocalNetChanger.Controls;

public sealed class IpAddressControl : UserControl
{
    private readonly TextBox[] _octets = new TextBox[4];
    private bool _updating;

    public IpAddressControl()
    {
        Height = 23;
        Width = 204;

        for (var i = 0; i < 4; i++)
        {
            var octet = new TextBox
            {
                BorderStyle = BorderStyle.FixedSingle,
                MaxLength = 3,
                Size = new Size(44, 23),
                Location = new Point(i * 52, 0),
                TextAlign = HorizontalAlignment.Center,
                TabIndex = i
            };

            octet.KeyPress += Octet_KeyPress;
            octet.KeyDown += Octet_KeyDown;
            octet.TextChanged += Octet_TextChanged;

            _octets[i] = octet;
            Controls.Add(octet);

            if (i < 3)
            {
                Controls.Add(new Label
                {
                    Text = ".",
                    AutoSize = false,
                    Size = new Size(8, 23),
                    Location = new Point(i * 52 + 44, 2),
                    TextAlign = ContentAlignment.MiddleCenter
                });
            }
        }
    }

    public string Value
    {
        get
        {
            if (IsEmpty)
                return string.Empty;

            return string.Join('.', _octets.Select(o => o.Text.Trim()));
        }
        set => SetValue(value);
    }

    public bool IsEmpty => _octets.All(o => string.IsNullOrWhiteSpace(o.Text));

    public void FocusFirst() => FocusOctet(0);

    public void FocusLastEmpty()
    {
        var index = Array.FindIndex(_octets, o => string.IsNullOrWhiteSpace(o.Text));
        FocusOctet(index >= 0 ? index : 3);
    }

    private void SetValue(string? value)
    {
        _updating = true;
        try
        {
            for (var i = 0; i < 4; i++)
                _octets[i].Text = string.Empty;

            if (string.IsNullOrWhiteSpace(value))
                return;

            ApplyParts(SplitIpParts(value));
        }
        finally
        {
            _updating = false;
        }
    }

    private void Octet_KeyPress(object? sender, KeyPressEventArgs e)
    {
        if (char.IsControl(e.KeyChar))
            return;

        if (!char.IsDigit(e.KeyChar))
        {
            if (e.KeyChar is '.' or ' ')
            {
                e.Handled = true;
                if (sender is TextBox box)
                    FocusNext(Array.IndexOf(_octets, box));
            }
            else
            {
                e.Handled = true;
            }

            return;
        }

        if (sender is not TextBox current)
            return;

        var index = Array.IndexOf(_octets, current);
        var proposed = current.Text.Length == current.SelectionLength
            ? e.KeyChar.ToString()
            : current.Text.Remove(current.SelectionStart, current.SelectionLength)
                .Insert(current.SelectionStart, e.KeyChar.ToString());

        if (proposed.Length > 3 || !byte.TryParse(proposed, out var octet))
        {
            e.Handled = true;
            if (byte.TryParse(proposed.AsSpan(0, Math.Min(3, proposed.Length)), out _) && index < 3)
                FocusNext(index);
        }
        else if (octet > 255)
        {
            e.Handled = true;
        }
    }

    private void Octet_KeyDown(object? sender, KeyEventArgs e)
    {
        if (sender is not TextBox current)
            return;

        var index = Array.IndexOf(_octets, current);

        switch (e.KeyCode)
        {
            case Keys.Space:
            case Keys.OemPeriod:
            case Keys.Decimal:
                e.SuppressKeyPress = true;
                e.Handled = true;
                FocusNext(index);
                break;

            case Keys.Left when current.SelectionStart == 0 && current.SelectionLength == 0:
                FocusPrevious(index);
                e.Handled = true;
                break;

            case Keys.Right when current.SelectionStart == current.Text.Length:
                FocusNext(index);
                e.Handled = true;
                break;

            case Keys.Back when current.Text.Length == 0 && index > 0:
                FocusPrevious(index);
                e.Handled = true;
                break;

            case Keys.V when e.Control:
                e.Handled = true;
                PasteFromClipboard(index);
                break;
        }
    }

    private void Octet_TextChanged(object? sender, EventArgs e)
    {
        if (_updating || sender is not TextBox current)
            return;

        var index = Array.IndexOf(_octets, current);
        if (current.Text.Contains('.') || current.Text.Contains(' '))
        {
            var segments = new List<string>();
            for (var i = 0; i < index; i++)
            {
                if (!string.IsNullOrWhiteSpace(_octets[i].Text))
                    segments.Add(_octets[i].Text.Trim());
            }

            segments.AddRange(SplitIpParts(current.Text));
            ApplyParts(segments);
            FocusOctet(Math.Min(3, segments.Count));
            return;
        }

        if (!string.IsNullOrEmpty(current.Text))
        {
            if (!byte.TryParse(current.Text, out var value))
            {
                _updating = true;
                current.Text = current.Text[..^1];
                current.SelectionStart = current.Text.Length;
                _updating = false;
                return;
            }

            if (value > 255)
            {
                _updating = true;
                current.Text = "255";
                current.SelectionStart = current.Text.Length;
                _updating = false;
            }
        }

        if (current.Text.Length == 3 && index < 3)
            FocusNext(index);
    }

    private void PasteFromClipboard(int startIndex)
    {
        var text = Clipboard.GetText()?.Trim();
        if (string.IsNullOrWhiteSpace(text))
            return;

        var parts = SplitIpParts(text);
        if (parts.Count == 0)
            return;

        _updating = true;
        try
        {
            for (var i = startIndex; i < 4; i++)
                _octets[i].Text = string.Empty;

            for (var i = 0; i < parts.Count && startIndex + i < 4; i++)
                _octets[startIndex + i].Text = parts[i];
        }
        finally
        {
            _updating = false;
        }

        FocusOctet(Math.Min(3, startIndex + parts.Count));
    }

    private void ApplyParts(IReadOnlyList<string> parts)
    {
        _updating = true;
        try
        {
            for (var i = 0; i < 4; i++)
                _octets[i].Text = i < parts.Count ? parts[i] : string.Empty;
        }
        finally
        {
            _updating = false;
        }
    }

    private static List<string> SplitIpParts(string value)
    {
        return value
            .Replace(" ", ".")
            .Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(part =>
            {
                if (!byte.TryParse(part, out var octet))
                    return null;

                return octet.ToString();
            })
            .Where(part => part != null)
            .Cast<string>()
            .Take(4)
            .ToList();
    }

    private void FocusNext(int index)
    {
        if (index < 3)
            FocusOctet(index + 1);
    }

    private void FocusPrevious(int index)
    {
        if (index > 0)
            FocusOctet(index - 1);
    }

    private void FocusOctet(int index)
    {
        if (index < 0 || index >= 4)
            return;

        _octets[index].Focus();
        _octets[index].SelectionStart = _octets[index].Text.Length;
    }
}
