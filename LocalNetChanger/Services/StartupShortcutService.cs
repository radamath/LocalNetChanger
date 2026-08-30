using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace LocalNetChanger.Services;

public static class StartupShortcutService
{
    private const string ShortcutName = "LocalNetChanger.lnk";
    private const string TrayOnlyArgument = "--tray-only";

    public static bool IsEnabled()
        => File.Exists(GetShortcutPath());

    public static void SetEnabled(bool enabled)
    {
        if (enabled)
            Create();
        else
            Remove();
    }

    private static string GetShortcutPath()
        => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), ShortcutName);

    private static void Create()
    {
        var exe = Environment.ProcessPath ?? Application.ExecutablePath;
        var workingDirectory = Path.GetDirectoryName(exe) ?? Environment.CurrentDirectory;
        var shellType = Type.GetTypeFromProgID("WScript.Shell")
            ?? throw new InvalidOperationException("WScript.Shell kullanılamıyor.");

        var shell = Activator.CreateInstance(shellType)
            ?? throw new InvalidOperationException("Kısayol oluşturulamadı.");

        try
        {
            dynamic shortcut = ((dynamic)shell).CreateShortcut(GetShortcutPath());
            shortcut.TargetPath = exe;
            shortcut.Arguments = TrayOnlyArgument;
            shortcut.WorkingDirectory = workingDirectory;
            shortcut.Save();
        }
        finally
        {
            if (shell is IDisposable disposable)
                disposable.Dispose();
            else
                Marshal.FinalReleaseComObject(shell);
        }
    }

    private static void Remove()
    {
        var path = GetShortcutPath();
        if (File.Exists(path))
            File.Delete(path);
    }
}
