using LocalNetChanger.Controls;
using LocalNetChanger.Localization;
using LocalNetChanger.Models;
using LocalNetChanger.Services;

namespace LocalNetChanger.Forms;

public sealed class ProfileEditForm : Form
{
    private readonly NetworkProfile? _existing;
    private readonly ComboBox _adapterCombo = new();
    private readonly TextBox _nameText = new();
    private readonly IpAddressControl _ipInput = new();
    private readonly IpAddressControl _subnetInput = new();
    private readonly IpAddressControl _gatewayInput = new();
    private readonly IpAddressControl _dns1Input = new();
    private readonly IpAddressControl _dns2Input = new();
    private readonly Label _nameLabel = new();
    private readonly Label _adapterLabel = new();
    private readonly Label _ipLabel = new();
    private readonly Label _subnetLabel = new();
    private readonly Label _gatewayLabel = new();
    private readonly Label _dns1Label = new();
    private readonly Label _dns2Label = new();
    private readonly Button _okButton = new();
    private readonly Button _cancelButton = new();

    public NetworkProfile? ResultProfile { get; private set; }

    public ProfileEditForm(AdapterCategory category, NetworkProfile? existing = null)
    {
        _existing = existing;

        Icon = (Icon)AppIcon.GetIcon().Clone();
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(440, 330);
        Font = new Font("Segoe UI", 9F);

        _nameLabel.AutoSize = true;
        _nameLabel.Location = new Point(16, 16);
        _nameText.Location = new Point(140, 12);
        _nameText.Size = new Size(280, 23);

        _adapterLabel.AutoSize = true;
        _adapterLabel.Location = new Point(16, 52);
        _adapterCombo.Location = new Point(140, 48);
        _adapterCombo.Size = new Size(280, 23);
        _adapterCombo.DropDownStyle = ComboBoxStyle.DropDownList;

        _ipLabel.AutoSize = true;
        _ipLabel.Location = new Point(16, 88);
        _ipInput.Location = new Point(140, 84);

        _subnetLabel.AutoSize = true;
        _subnetLabel.Location = new Point(16, 124);
        _subnetInput.Location = new Point(140, 120);

        _gatewayLabel.AutoSize = true;
        _gatewayLabel.Location = new Point(16, 160);
        _gatewayInput.Location = new Point(140, 156);

        _dns1Label.AutoSize = true;
        _dns1Label.Location = new Point(16, 196);
        _dns1Input.Location = new Point(140, 192);

        _dns2Label.AutoSize = true;
        _dns2Label.Location = new Point(16, 232);
        _dns2Input.Location = new Point(140, 228);

        _okButton.DialogResult = DialogResult.None;
        _okButton.Location = new Point(244, 280);
        _okButton.Size = new Size(80, 28);
        _okButton.Click += (_, _) => TrySave(category);

        _cancelButton.DialogResult = DialogResult.Cancel;
        _cancelButton.Location = new Point(340, 280);
        _cancelButton.Size = new Size(80, 28);

        AcceptButton = _okButton;
        CancelButton = _cancelButton;

        Controls.AddRange([
            _nameLabel, _nameText, _adapterLabel, _adapterCombo,
            _ipLabel, _ipInput, _subnetLabel, _subnetInput,
            _gatewayLabel, _gatewayInput, _dns1Label, _dns1Input, _dns2Label, _dns2Input,
            _okButton, _cancelButton
        ]);

        ApplyLocalization(existing);
        LoadAdapters(category);

        if (existing != null)
        {
            _nameText.Text = existing.Name;
            _ipInput.Value = existing.IpAddress;
            _subnetInput.Value = existing.SubnetMask;
            _gatewayInput.Value = existing.DefaultGateway;
            _dns1Input.Value = existing.Dns1;
            _dns2Input.Value = existing.Dns2;

            var matchIndex = -1;
            for (var i = 0; i < _adapterCombo.Items.Count; i++)
            {
                if (_adapterCombo.Items[i] is AdapterInfo info &&
                    (string.Equals(info.Id, existing.AdapterId, StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(info.Name, existing.AdapterName, StringComparison.OrdinalIgnoreCase)))
                {
                    matchIndex = i;
                    break;
                }
            }

            if (matchIndex >= 0)
                _adapterCombo.SelectedIndex = matchIndex;
        }
    }

    private void ApplyLocalization(NetworkProfile? existing)
    {
        Text = existing == null ? Loc.NewProfile : Loc.EditProfile;
        _nameLabel.Text = Loc.ProfileName;
        _adapterLabel.Text = Loc.Adapter;
        _ipLabel.Text = Loc.ColIp + ":";
        _subnetLabel.Text = Loc.ColSubnet + ":";
        _gatewayLabel.Text = Loc.ColGateway + ":";
        _dns1Label.Text = Loc.ColDns1 + ":";
        _dns2Label.Text = Loc.ColDns2 + ":";
        _okButton.Text = Loc.Save;
        _cancelButton.Text = Loc.Cancel;
    }

    private void LoadAdapters(AdapterCategory category)
    {
        _adapterCombo.Items.Clear();
        foreach (var adapter in NetworkService.GetAvailableAdapters().Where(a => a.Category == category))
            _adapterCombo.Items.Add(adapter);

        if (_adapterCombo.Items.Count > 0 && _adapterCombo.SelectedIndex < 0)
            _adapterCombo.SelectedIndex = 0;
    }

    private void TrySave(AdapterCategory category)
    {
        if (string.IsNullOrWhiteSpace(_nameText.Text))
        {
            MessageBox.Show(this, Loc.ProfileNameRequired, Loc.Validation, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _nameText.Focus();
            return;
        }

        if (_adapterCombo.SelectedItem is not AdapterInfo adapter)
        {
            MessageBox.Show(this, Loc.AdapterRequired, Loc.Validation, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var ip = _ipInput.Value;
        if (!NetworkService.TryValidateIpAddress(ip, out var ipError))
        {
            MessageBox.Show(this, ipError, Loc.Validation, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _ipInput.FocusFirst();
            return;
        }

        var subnet = _subnetInput.Value;
        if (!NetworkService.TryValidateSubnetMask(subnet, out var maskError))
        {
            MessageBox.Show(this, maskError, Loc.Validation, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _subnetInput.FocusFirst();
            return;
        }

        var gateway = _gatewayInput.Value;
        if (!NetworkService.TryValidateOptionalIpAddress(gateway, out var gatewayError))
        {
            MessageBox.Show(this, gatewayError, Loc.ColGateway, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _gatewayInput.FocusFirst();
            return;
        }

        var dns1 = _dns1Input.Value;
        if (!NetworkService.TryValidateOptionalIpAddress(dns1, out var dns1Error))
        {
            MessageBox.Show(this, dns1Error, Loc.ColDns1, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _dns1Input.FocusFirst();
            return;
        }

        var dns2 = _dns2Input.Value;
        if (!NetworkService.TryValidateOptionalIpAddress(dns2, out var dns2Error))
        {
            MessageBox.Show(this, dns2Error, Loc.ColDns2, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _dns2Input.FocusFirst();
            return;
        }

        if (string.IsNullOrWhiteSpace(dns1) && !string.IsNullOrWhiteSpace(dns2))
        {
            MessageBox.Show(this, Loc.Dns2RequiresDns1, Loc.Validation,
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _dns1Input.FocusFirst();
            return;
        }

        ResultProfile = new NetworkProfile
        {
            Id = _existing?.Id ?? Guid.NewGuid().ToString(),
            Name = _nameText.Text.Trim(),
            Category = category,
            AdapterId = adapter.Id,
            AdapterName = adapter.Name,
            IpAddress = ip,
            SubnetMask = subnet,
            DefaultGateway = gateway,
            Dns1 = dns1,
            Dns2 = dns2
        };

        DialogResult = DialogResult.OK;
        Close();
    }
}
