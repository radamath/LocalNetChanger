using LocalNetChanger.Localization;
using LocalNetChanger.Models;
using LocalNetChanger.Services;

namespace LocalNetChanger.Forms;

public sealed class SettingsForm : Form
{
    private readonly ProfileStorage _storage;
    private readonly AppSettingsStorage _appSettings;
    private readonly TabControl _tabs = new();
    private readonly TabPage _profilesTab = new();
    private readonly TabPage _appTab = new();
    private readonly ListView _profileList = new();
    private readonly Button _addEthernetButton = new();
    private readonly Button _addWirelessButton = new();
    private readonly Button _editButton = new();
    private readonly Button _deleteButton = new();
    private readonly Button _hideButton = new();
    private readonly Button _closeAppButton = new();
    private readonly Label _languageLabel = new();
    private readonly ComboBox _languageCombo = new();
    private readonly Label _startupLabel = new();
    private readonly RadioButton _startupYes = new();
    private readonly RadioButton _startupNo = new();
    private bool _forceClose;
    private bool _suppressLanguageChange;
    private bool _suppressStartupChange;

    public event EventHandler? HiddenToTray;
    public event EventHandler? ExitRequested;

    public SettingsForm(ProfileStorage storage, AppSettingsStorage appSettings)
    {
        _storage = storage;
        _appSettings = appSettings;

        Icon = (Icon)AppIcon.GetIcon().Clone();
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = true;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(900, 430);
        Font = new Font("Segoe UI", 9F);

        _tabs.Location = new Point(12, 12);
        _tabs.Size = new Size(876, 368);
        _tabs.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;

        _hideButton.Size = new Size(200, 28);
        _hideButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        _hideButton.Location = new Point(488, 390);
        _hideButton.Click += (_, _) => HideToTray();

        _closeAppButton.Size = new Size(170, 28);
        _closeAppButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        _closeAppButton.Location = new Point(698, 390);
        _closeAppButton.Click += (_, _) => ExitRequested?.Invoke(this, EventArgs.Empty);

        _profilesTab.Padding = new Padding(8);
        _appTab.Padding = new Padding(16);

        BuildProfilesTab();
        BuildAppTab();

        _tabs.TabPages.Add(_profilesTab);
        _tabs.TabPages.Add(_appTab);

        Controls.AddRange([_tabs, _hideButton, _closeAppButton]);

        FormClosing += OnFormClosing;
        Resize += OnResize;
        Loc.Changed += OnLanguageChanged;

        ApplyLocalization();
        RefreshList();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            Loc.Changed -= OnLanguageChanged;

        base.Dispose(disposing);
    }

    public void ForceClose()
    {
        _forceClose = true;
        Close();
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        Activate();
        Focus();
    }

    private void BuildProfilesTab()
    {
        _profileList.Dock = DockStyle.Fill;
        _profileList.View = View.Details;
        _profileList.FullRowSelect = true;
        _profileList.MultiSelect = false;
        _profileList.HideSelection = false;
        _profileList.SelectedIndexChanged += (_, _) => UpdateButtonStates();

        var buttonPanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 40
        };

        _addEthernetButton.Location = new Point(0, 6);
        _addEthernetButton.Size = new Size(100, 28);
        _addEthernetButton.Click += (_, _) => AddProfile(AdapterCategory.Ethernet);

        _addWirelessButton.Location = new Point(106, 6);
        _addWirelessButton.Size = new Size(100, 28);
        _addWirelessButton.Click += (_, _) => AddProfile(AdapterCategory.Wireless);

        _editButton.Location = new Point(624, 6);
        _editButton.Size = new Size(80, 28);
        _editButton.Click += (_, _) => EditSelected();

        _deleteButton.Location = new Point(710, 6);
        _deleteButton.Size = new Size(70, 28);
        _deleteButton.Click += (_, _) => DeleteSelected();

        buttonPanel.Controls.AddRange([
            _addEthernetButton, _addWirelessButton, _editButton, _deleteButton
        ]);

        _profilesTab.Controls.Add(_profileList);
        _profilesTab.Controls.Add(buttonPanel);
    }

    private void BuildAppTab()
    {
        _languageLabel.AutoSize = true;
        _languageLabel.Location = new Point(0, 12);

        _languageCombo.Location = new Point(0, 36);
        _languageCombo.Size = new Size(260, 23);
        _languageCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _languageCombo.SelectedIndexChanged += (_, _) => OnLanguageSelected();

        _startupLabel.AutoSize = true;
        _startupLabel.Location = new Point(0, 84);

        _startupYes.AutoSize = true;
        _startupYes.Location = new Point(0, 110);
        _startupYes.CheckedChanged += (_, _) => OnStartupChanged();

        _startupNo.AutoSize = true;
        _startupNo.Location = new Point(80, 110);
        _startupNo.CheckedChanged += (_, _) => OnStartupChanged();

        _appTab.Controls.AddRange([
            _languageLabel, _languageCombo,
            _startupLabel, _startupYes, _startupNo
        ]);
    }

    private void OnLanguageChanged()
    {
        if (IsDisposed)
            return;

        ApplyLocalization();
        RefreshList();
    }

    private void ApplyLocalization()
    {
        Text = Loc.ControlPanel;
        _profilesTab.Text = Loc.TabProfiles;
        _appTab.Text = Loc.TabApplication;

        if (_profileList.Columns.Count >= 8)
        {
            _profileList.Columns[0].Text = Loc.ColProfile;
            _profileList.Columns[1].Text = Loc.ColType;
            _profileList.Columns[2].Text = Loc.ColAdapter;
            _profileList.Columns[3].Text = Loc.ColIp;
            _profileList.Columns[4].Text = Loc.ColSubnet;
            _profileList.Columns[5].Text = Loc.ColGateway;
            _profileList.Columns[6].Text = Loc.ColDns1;
            _profileList.Columns[7].Text = Loc.ColDns2;
        }
        else
        {
            _profileList.Columns.Clear();
            _profileList.Columns.Add(Loc.ColProfile, 100);
            _profileList.Columns.Add(Loc.ColType, 70);
            _profileList.Columns.Add(Loc.ColAdapter, 120);
            _profileList.Columns.Add(Loc.ColIp, 100);
            _profileList.Columns.Add(Loc.ColSubnet, 100);
            _profileList.Columns.Add(Loc.ColGateway, 100);
            _profileList.Columns.Add(Loc.ColDns1, 100);
            _profileList.Columns.Add(Loc.ColDns2, 100);
        }

        _addEthernetButton.Text = Loc.AddWired;
        _addWirelessButton.Text = Loc.AddWireless;
        _editButton.Text = Loc.Edit;
        _deleteButton.Text = Loc.Delete;
        _hideButton.Text = Loc.Hide;
        _closeAppButton.Text = Loc.CloseApp;

        _languageLabel.Text = Loc.LanguageLabel;
        _startupLabel.Text = Loc.StartupLabel;
        _startupYes.Text = Loc.StartupYes;
        _startupNo.Text = Loc.StartupNo;

        _suppressLanguageChange = true;
        _languageCombo.Items.Clear();
        _languageCombo.Items.AddRange([
            Loc.LanguageSystem,
            Loc.LanguageTurkish,
            Loc.LanguageEnglish
        ]);

        _languageCombo.SelectedIndex = _appSettings.Settings.Language switch
        {
            AppLanguage.Turkish => 1,
            AppLanguage.English => 2,
            _ => 0
        };
        _suppressLanguageChange = false;

        _suppressStartupChange = true;
        var startWithWindows = StartupShortcutService.IsEnabled();
        _appSettings.SetStartWithWindows(startWithWindows);
        _startupYes.Checked = startWithWindows;
        _startupNo.Checked = !startWithWindows;
        _suppressStartupChange = false;
    }

    private void OnLanguageSelected()
    {
        if (_suppressLanguageChange || _languageCombo.SelectedIndex < 0)
            return;

        var language = _languageCombo.SelectedIndex switch
        {
            1 => AppLanguage.Turkish,
            2 => AppLanguage.English,
            _ => AppLanguage.System
        };

        _appSettings.SetLanguage(language);
        Loc.ApplyLanguage(language);
    }

    private void OnStartupChanged()
    {
        if (_suppressStartupChange)
            return;

        if (!_startupYes.Checked && !_startupNo.Checked)
            return;

        try
        {
            StartupShortcutService.SetEnabled(_startupYes.Checked);
            _appSettings.SetStartWithWindows(_startupYes.Checked);
        }
        catch
        {
            _suppressStartupChange = true;
            var enabled = StartupShortcutService.IsEnabled();
            _startupYes.Checked = enabled;
            _startupNo.Checked = !enabled;
            _suppressStartupChange = false;
        }
    }

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        if (_forceClose)
            return;

        e.Cancel = true;
        HideToTray();
    }

    private void OnResize(object? sender, EventArgs e)
    {
        if (WindowState == FormWindowState.Minimized)
            HideToTray();
    }

    private void HideToTray()
    {
        if (!Visible)
            return;

        Hide();
        WindowState = FormWindowState.Normal;
        HiddenToTray?.Invoke(this, EventArgs.Empty);
    }

    private void RefreshList()
    {
        _profileList.Items.Clear();
        foreach (var profile in _storage.Profiles.OrderBy(p => p.Category).ThenBy(p => p.Name))
        {
            var item = new ListViewItem(profile.Name);
            item.SubItems.Add(profile.Category == AdapterCategory.Ethernet ? Loc.TypeWired : Loc.TypeWireless);
            item.SubItems.Add(profile.AdapterName);
            item.SubItems.Add(profile.IpAddress);
            item.SubItems.Add(profile.SubnetMask);
            item.SubItems.Add(string.IsNullOrWhiteSpace(profile.DefaultGateway) ? "-" : profile.DefaultGateway);
            item.SubItems.Add(string.IsNullOrWhiteSpace(profile.Dns1) ? "-" : profile.Dns1);
            item.SubItems.Add(string.IsNullOrWhiteSpace(profile.Dns2) ? "-" : profile.Dns2);
            item.Tag = profile;
            _profileList.Items.Add(item);
        }

        UpdateButtonStates();
    }

    private void UpdateButtonStates()
    {
        var hasSelection = _profileList.SelectedItems.Count > 0;
        _editButton.Enabled = hasSelection;
        _deleteButton.Enabled = hasSelection;
    }

    private void AddProfile(AdapterCategory category)
    {
        using var form = new ProfileEditForm(category);
        if (form.ShowDialog(this) == DialogResult.OK && form.ResultProfile != null)
        {
            _storage.Add(form.ResultProfile);
            RefreshList();
        }
    }

    private void EditSelected()
    {
        if (_profileList.SelectedItems.Count == 0 ||
            _profileList.SelectedItems[0].Tag is not NetworkProfile profile)
            return;

        using var form = new ProfileEditForm(profile.Category, profile);
        if (form.ShowDialog(this) == DialogResult.OK && form.ResultProfile != null)
        {
            _storage.Update(form.ResultProfile);
            RefreshList();
        }
    }

    private void DeleteSelected()
    {
        if (_profileList.SelectedItems.Count == 0 ||
            _profileList.SelectedItems[0].Tag is not NetworkProfile profile)
            return;

        var result = MessageBox.Show(
            this,
            Loc.DeleteProfileMessage(profile.Name),
            Loc.DeleteProfileTitle,
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result == DialogResult.Yes)
        {
            _storage.Delete(profile.Id);
            RefreshList();
        }
    }
}
