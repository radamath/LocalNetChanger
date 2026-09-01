using LocalNetChanger.Forms;
using LocalNetChanger.Localization;
using LocalNetChanger.Models;
using LocalNetChanger.Services;

namespace LocalNetChanger;

public sealed class TrayApplicationContext : ApplicationContext
{
    private readonly NotifyIcon _trayIcon;
    private readonly ProfileStorage _storage;
    private readonly AppSettingsStorage _appSettings;
    private readonly ContextMenuStrip _contextMenu;
    private readonly ToolStripMenuItem _settingsMenu;
    private readonly ToolStripMenuItem _networkChangeMenu;
    private readonly ToolStripMenuItem _wiredMenu;
    private readonly ToolStripMenuItem _wirelessMenu;
    private readonly ToolStripMenuItem _exitMenu;
    private SettingsForm? _settingsForm;
    private SynchronizationContext? _uiContext;

    public TrayApplicationContext(AppSettingsStorage appSettings, bool openSettingsOnStart = false)
    {
        _uiContext = SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext();
        SynchronizationContext.SetSynchronizationContext(_uiContext);
        _appSettings = appSettings;
        _storage = new ProfileStorage();
        _contextMenu = new ContextMenuStrip();
        _networkChangeMenu = new ToolStripMenuItem();
        _wiredMenu = new ToolStripMenuItem();
        _wirelessMenu = new ToolStripMenuItem();

        _wiredMenu.DropDownOpening += (_, _) => BuildCategoryMenu(_wiredMenu, AdapterCategory.Ethernet);
        _wirelessMenu.DropDownOpening += (_, _) => BuildCategoryMenu(_wirelessMenu, AdapterCategory.Wireless);

        _networkChangeMenu.DropDownItems.Add(_wiredMenu);
        _networkChangeMenu.DropDownItems.Add(_wirelessMenu);

        _settingsMenu = new ToolStripMenuItem();
        _settingsMenu.Click += (_, _) => ShowSettings();

        _exitMenu = new ToolStripMenuItem();
        _exitMenu.Click += (_, _) => ExitApplication();

        _contextMenu.Items.Add(_settingsMenu);
        _contextMenu.Items.Add(new ToolStripSeparator());
        _contextMenu.Items.Add(_networkChangeMenu);
        _contextMenu.Items.Add(new ToolStripSeparator());
        _contextMenu.Items.Add(_exitMenu);

        _contextMenu.Opening += (_, _) => _storage.Load();

        _trayIcon = new NotifyIcon
        {
            Icon = (Icon)AppIcon.GetIcon().Clone(),
            Text = Loc.AppName,
            Visible = true,
            ContextMenuStrip = _contextMenu
        };

        _trayIcon.DoubleClick += (_, _) => ShowSettings();

        ApplyTrayLocalization();
        Loc.Changed += OnLanguageChanged;

        BuildCategoryMenu(_wiredMenu, AdapterCategory.Ethernet);
        BuildCategoryMenu(_wirelessMenu, AdapterCategory.Wireless);

        SingleInstanceManager.StartServer(openSettings =>
        {
            RunOnUi(() => HandleRemoteCommand(openSettings));
        });

        Application.Idle += CaptureUiContext;
        if (openSettingsOnStart)
            Application.Idle += OnFirstIdleOpenSettings;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            Loc.Changed -= OnLanguageChanged;

        base.Dispose(disposing);
    }

    private void CaptureUiContext(object? sender, EventArgs e)
    {
        Application.Idle -= CaptureUiContext;
        _uiContext ??= SynchronizationContext.Current;
    }

    private void RunOnUi(Action action)
    {
        _uiContext ??= SynchronizationContext.Current;

        if (_uiContext == null || ReferenceEquals(SynchronizationContext.Current, _uiContext))
        {
            action();
            return;
        }

        _uiContext.Post(_ => action(), null);
    }

    private void OnLanguageChanged()
    {
        RunOnUi(() =>
        {
            ApplyTrayLocalization();
            BuildCategoryMenu(_wiredMenu, AdapterCategory.Ethernet);
            BuildCategoryMenu(_wirelessMenu, AdapterCategory.Wireless);
            _trayIcon.Text = Loc.AppName;
        });
    }

    private void ApplyTrayLocalization()
    {
        _settingsMenu.Text = Loc.MenuControlPanel;
        _networkChangeMenu.Text = Loc.MenuChangeNetwork;
        _wiredMenu.Text = Loc.MenuWired;
        _wirelessMenu.Text = Loc.MenuWireless;
        _exitMenu.Text = Loc.MenuExit;
    }

    private void OnFirstIdleOpenSettings(object? sender, EventArgs e)
    {
        Application.Idle -= OnFirstIdleOpenSettings;
        ShowSettings();
    }

    private void HandleRemoteCommand(bool openSettings)
    {
        if (openSettings)
            ShowSettings();
        else if (_settingsForm is { Visible: true })
        {
            _settingsForm.Show();
            _settingsForm.WindowState = FormWindowState.Normal;
            _settingsForm.BringToFront();
        }
    }

    private void BuildCategoryMenu(ToolStripMenuItem parent, AdapterCategory category)
    {
        parent.DropDownItems.Clear();

        var last = _appSettings.GetLastNetworkChoice(category);

        var dhcpItem = new ToolStripMenuItem(Loc.MenuDhcp)
        {
            Checked = last?.IsDhcp == true
        };
        dhcpItem.Click += (_, _) => ApplyDhcp(category);
        parent.DropDownItems.Add(dhcpItem);

        var profiles = _storage.GetByCategory(category).ToList();
        if (profiles.Count > 0)
        {
            parent.DropDownItems.Add(new ToolStripSeparator());
            foreach (var profile in profiles)
            {
                var item = new ToolStripMenuItem(profile.Name)
                {
                    Checked = last is { IsDhcp: false } && last.ProfileId == profile.Id
                };
                item.Click += (_, _) => ApplyProfile(profile);
                parent.DropDownItems.Add(item);
            }
        }
    }

    private void ApplyDhcp(AdapterCategory category)
    {
        var result = NetworkService.ApplyDhcp(category);
        if (result.AlreadyActive)
            _trayIcon.ShowBalloonTip(4000, Loc.AppName, result.Message, ToolTipIcon.Info);
        else if (result.Success)
        {
            _appSettings.SetLastNetworkChoice(category, isDhcp: true);
            BuildCategoryMenu(category == AdapterCategory.Ethernet ? _wiredMenu : _wirelessMenu, category);
        }
        else
            _trayIcon.ShowBalloonTip(4000, Loc.AppName, result.Message, ToolTipIcon.Error);
    }

    private void ApplyProfile(NetworkProfile profile)
    {
        var result = NetworkService.ApplyProfile(profile);
        if (result.AlreadyActive)
            _trayIcon.ShowBalloonTip(4000, Loc.AppName, result.Message, ToolTipIcon.Info);
        else if (result.Success)
        {
            _appSettings.SetLastNetworkChoice(profile.Category, isDhcp: false, profile.Id);
            BuildCategoryMenu(
                profile.Category == AdapterCategory.Ethernet ? _wiredMenu : _wirelessMenu,
                profile.Category);
        }
        else
            _trayIcon.ShowBalloonTip(4000, Loc.AppName, result.Message, ToolTipIcon.Error);
    }

    private void ShowSettings()
    {
        if (_settingsForm == null || _settingsForm.IsDisposed)
        {
            _settingsForm = new SettingsForm(_storage, _appSettings);
            _settingsForm.FormClosed += (_, _) =>
            {
                _storage.Load();
                BuildCategoryMenu(_wiredMenu, AdapterCategory.Ethernet);
                BuildCategoryMenu(_wirelessMenu, AdapterCategory.Wireless);
                _settingsForm = null;
            };
            _settingsForm.ExitRequested += (_, _) => ExitApplication();
            _settingsForm.Show();
        }
        else
        {
            _settingsForm.Show();
            _settingsForm.WindowState = FormWindowState.Normal;
            _settingsForm.BringToFront();
            _settingsForm.Focus();
        }
    }

    private void ExitApplication()
    {
        SingleInstanceManager.Release();
        _settingsForm?.ForceClose();
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        _contextMenu.Dispose();
        ExitThread();
    }
}
