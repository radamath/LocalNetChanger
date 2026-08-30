using System.Diagnostics;
using System.Reflection;
using System.IO.Compression;

namespace LocalNetChanger.Setup;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        if (!IsAdministrator())
        {
            RelaunchElevated();
            return;
        }

        if (!IsDesktopRuntimeInstalled())
        {
            var result = MessageBox.Show(
                "LocalNetChanger icin .NET 8 Desktop Runtime gereklidir.\r\n\r\n" +
                "Runtime yuklu degil. Indirme sayfasini acmak ister misiniz?\r\n\r\n" +
                "https://dotnet.microsoft.com/download/dotnet/8.0",
                "LocalNetChanger Kurulum",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://dotnet.microsoft.com/download/dotnet/8.0",
                    UseShellExecute = true
                });
            }

            return;
        }

        if (!ShowLicenseDialog())
            return;

        try
        {
            var extractDir = Path.Combine(Path.GetTempPath(), "LocalNetChangerSetup_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(extractDir);
            ExtractPayload(extractDir);
            InstallHelper.InstallFromExtracted(extractDir);
            MessageBox.Show(
                "LocalNetChanger basariyla kuruldu.\r\n\r\nKonum: C:\\Program Files\\LocalNetChanger",
                "Kurulum Tamamlandi",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "Kurulum basarisiz:\r\n" + ex.Message,
                "LocalNetChanger Kurulum",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private static bool IsAdministrator()
    {
        using var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
        var principal = new System.Security.Principal.WindowsPrincipal(identity);
        return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
    }

    private static void RelaunchElevated()
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = Environment.ProcessPath ?? Application.ExecutablePath,
            UseShellExecute = true,
            Verb = "runas"
        });
    }

    private static bool IsDesktopRuntimeInstalled()
    {
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        var candidates = new[]
        {
            Path.Combine(programFiles, "dotnet", "shared", "Microsoft.WindowsDesktop.App"),
            Path.Combine(programFilesX86, "dotnet", "shared", "Microsoft.WindowsDesktop.App")
        };

        foreach (var basePath in candidates)
        {
            if (!Directory.Exists(basePath))
                continue;

            foreach (var dir in Directory.GetDirectories(basePath))
            {
                if (Path.GetFileName(dir).StartsWith("8.", StringComparison.Ordinal))
                    return true;
            }
        }

        return false;
    }

    private static bool ShowLicenseDialog()
    {
        using var form = new Form
        {
            Text = "LocalNetChanger - GPL Lisans Sozlesmesi",
            StartPosition = FormStartPosition.CenterScreen,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            ClientSize = new Size(640, 460)
        };

        var textBox = new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            Location = new Point(12, 12),
            Size = new Size(610, 360),
            Font = new Font("Segoe UI", 9F),
            Text = LoadLicenseText()
        };

        var accept = new Button
        {
            Text = "Kabul Ediyorum",
            DialogResult = DialogResult.OK,
            Location = new Point(432, 385),
            Size = new Size(90, 28)
        };

        var cancel = new Button
        {
            Text = "Iptal",
            DialogResult = DialogResult.Cancel,
            Location = new Point(528, 385),
            Size = new Size(84, 28)
        };

        form.Controls.AddRange([textBox, accept, cancel]);
        form.AcceptButton = accept;
        form.CancelButton = cancel;

        return form.ShowDialog() == DialogResult.OK;
    }

    private static string LoadLicenseText()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith("license.txt", StringComparison.OrdinalIgnoreCase));

        if (resourceName != null)
        {
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream != null)
            {
                using var reader = new StreamReader(stream);
                return reader.ReadToEnd();
            }
        }

        return "GPL v3 lisans metni yuklenemedi.";
    }

    private static void ExtractPayload(string targetDir)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith("payload.zip", StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException("Kurulum paketi bulunamadi.");

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException("Kurulum arsivi acilamadi.");

        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
        archive.ExtractToDirectory(targetDir, overwriteFiles: true);
    }
}
