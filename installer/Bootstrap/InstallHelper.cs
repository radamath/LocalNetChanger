using System.Diagnostics;
using Microsoft.Win32;

namespace LocalNetChanger.Setup;

internal static class InstallHelper
{
    private const string AppName = "LocalNetChanger";
    private const string AppExe = "LocalNetChanger.exe";
    private const string Version = "1.0.0";

    public static void InstallFromExtracted(string extractDir)
    {
        var appSource = Path.Combine(extractDir, "app");
        if (!Directory.Exists(appSource))
            throw new DirectoryNotFoundException("Uygulama dosyalari bulunamadi: " + appSource);

        var installDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), AppName);

        foreach (var process in Process.GetProcessesByName("LocalNetChanger"))
        {
            try { process.Kill(); }
            catch { /* uygulama acik olmayabilir */ }
        }

        Thread.Sleep(500);

        if (Directory.Exists(installDir))
            Directory.Delete(installDir, recursive: true);

        Directory.CreateDirectory(installDir);
        CopyDirectory(appSource, installDir);

        CopyIfExists(Path.Combine(extractDir, "license.txt"), Path.Combine(installDir, "LICENSE.txt"));
        CopyIfExists(Path.Combine(extractDir, "info.txt"), Path.Combine(installDir, "README-KURULUM.txt"));
        CopyIfExists(Path.Combine(extractDir, "uninstall.ps1"), Path.Combine(installDir, "uninstall.ps1"));

        var exePath = Path.Combine(installDir, AppExe);
        var programs = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
        var startMenuDir = Path.Combine(programs, AppName);
        Directory.CreateDirectory(startMenuDir);

        CreateShortcut(Path.Combine(startMenuDir, AppName + ".lnk"), exePath, AppName);
        CreateShortcut(Path.Combine(startMenuDir, "Lisans (GPL v3).lnk"), Path.Combine(installDir, "LICENSE.txt"), "GPL v3 Lisans");

        RegisterUninstall(installDir, exePath);
    }

    private static void CopyDirectory(string source, string target)
    {
        Directory.CreateDirectory(target);

        foreach (var file in Directory.GetFiles(source))
            File.Copy(file, Path.Combine(target, Path.GetFileName(file)), overwrite: true);

        foreach (var dir in Directory.GetDirectories(source))
        {
            var name = Path.GetFileName(dir);
            CopyDirectory(dir, Path.Combine(target, name));
        }
    }

    private static void CopyIfExists(string source, string target)
    {
        if (File.Exists(source))
            File.Copy(source, target, overwrite: true);
    }

    private static void CreateShortcut(string shortcutPath, string targetPath, string description)
    {
        var shellType = Type.GetTypeFromProgID("WScript.Shell")
            ?? throw new InvalidOperationException("WScript.Shell kullanilamiyor.");

        dynamic shell = Activator.CreateInstance(shellType)!;
        dynamic shortcut = shell.CreateShortcut(shortcutPath);
        shortcut.TargetPath = targetPath;
        shortcut.WorkingDirectory = Path.GetDirectoryName(targetPath) ?? string.Empty;
        shortcut.Description = description;
        shortcut.Save();
    }

    private static void RegisterUninstall(string installDir, string exePath)
    {
        var uninstallScript = Path.Combine(installDir, "uninstall.ps1");
        var uninstallKey = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\" + AppName, writable: true)
            ?? throw new InvalidOperationException("Kayit defteri anahtari olusturulamadi.");

        uninstallKey.SetValue("DisplayName", AppName);
        uninstallKey.SetValue("DisplayVersion", Version);
        uninstallKey.SetValue("Publisher", AppName);
        uninstallKey.SetValue("DisplayIcon", exePath);
        uninstallKey.SetValue("InstallLocation", installDir);
        uninstallKey.SetValue("UninstallString", $"powershell.exe -NoProfile -ExecutionPolicy Bypass -File \"{uninstallScript}\"");
        uninstallKey.SetValue("QuietUninstallString", $"powershell.exe -NoProfile -ExecutionPolicy Bypass -File \"{uninstallScript}\" -Silent");
        uninstallKey.SetValue("NoModify", 1, RegistryValueKind.DWord);
        uninstallKey.SetValue("NoRepair", 1, RegistryValueKind.DWord);
    }
}
