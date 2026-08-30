using LocalNetChanger.Localization;
using LocalNetChanger.Services;

namespace LocalNetChanger;

internal static class Program
{
    internal const string OpenSettingsArgument = "--open-settings";
    internal const string TrayOnlyArgument = "--tray-only";

    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();

        var appSettings = new AppSettingsStorage();
        Loc.Initialize(appSettings);

        var trayOnly = args.Any(a =>
            string.Equals(a, TrayOnlyArgument, StringComparison.OrdinalIgnoreCase));

        var openSettings = !trayOnly || args.Any(a =>
            string.Equals(a, OpenSettingsArgument, StringComparison.OrdinalIgnoreCase));

        if (!SingleInstanceManager.TryAcquire())
        {
            SingleInstanceManager.Signal(openSettings);
            return;
        }

        Application.Run(new TrayApplicationContext(appSettings, openSettings));
    }
}
