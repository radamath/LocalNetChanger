using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using LocalNetChanger.Localization;
using LocalNetChanger.Models;

namespace LocalNetChanger.Services;

public sealed record AdapterInfo(
    string Id,
    string Name,
    AdapterCategory Category,
    string Description)
{
    public override string ToString() => Name;
}

public static class NetworkService
{
    private static readonly Regex AdapterNameSafePattern = new(@"^[\w\s\-\.\(\)\[\]#]+$", RegexOptions.Compiled);

    public static IReadOnlyList<AdapterInfo> GetAvailableAdapters()
    {
        return NetworkInterface.GetAllNetworkInterfaces()
            .Where(ni => !ShouldExclude(ni))
            .Select(ni => new
            {
                Interface = ni,
                Category = ResolveCategory(ni)
            })
            .Where(x => x.Category.HasValue)
            .Select(x => new AdapterInfo(
                x.Interface.Id,
                x.Interface.Name,
                x.Category!.Value,
                x.Interface.Description))
            .OrderBy(a => a.Category)
            .ThenBy(a => a.Name)
            .ToList();
    }

    public static AdapterInfo? FindAdapter(string adapterId, string adapterName, AdapterCategory category)
    {
        var adapters = GetAvailableAdapters();

        var byId = adapters.FirstOrDefault(a =>
            string.Equals(a.Id, adapterId, StringComparison.OrdinalIgnoreCase));
        if (byId != null)
            return byId;

        return adapters.FirstOrDefault(a =>
            a.Category == category &&
            string.Equals(a.Name, adapterName, StringComparison.OrdinalIgnoreCase));
    }

    public static bool TryValidateIpAddress(string value, out string error)
    {
        error = string.Empty;
        if (string.IsNullOrWhiteSpace(value))
        {
            error = Loc.IpRequired;
            return false;
        }

        if (!IPAddress.TryParse(value.Trim(), out var ip) || ip.AddressFamily != AddressFamily.InterNetwork)
        {
            error = Loc.IpInvalid;
            return false;
        }

        if (IPAddress.IsLoopback(ip))
        {
            error = Loc.IpLoopback;
            return false;
        }

        return true;
    }

    public static bool TryValidateSubnetMask(string value, out string error)
    {
        error = string.Empty;
        if (string.IsNullOrWhiteSpace(value))
        {
            error = Loc.SubnetRequired;
            return false;
        }

        if (!IPAddress.TryParse(value.Trim(), out var mask) || mask.AddressFamily != AddressFamily.InterNetwork)
        {
            error = Loc.SubnetInvalid;
            return false;
        }

        var bytes = mask.GetAddressBytes();
        uint maskValue = BitConverter.ToUInt32(bytes.Reverse().ToArray(), 0);
        if (maskValue == 0)
        {
            error = Loc.SubnetZero;
            return false;
        }

        uint inverted = ~maskValue;
        if ((inverted & (inverted + 1)) != 0)
        {
            error = Loc.SubnetInvalid;
            return false;
        }

        return true;
    }

    public static bool TryValidateOptionalIpAddress(string? value, out string error)
    {
        error = string.Empty;
        if (string.IsNullOrWhiteSpace(value))
            return true;

        if (!IPAddress.TryParse(value.Trim(), out var ip) || ip.AddressFamily != AddressFamily.InterNetwork)
        {
            error = Loc.IpInvalid;
            return false;
        }

        if (IPAddress.IsLoopback(ip))
        {
            error = Loc.IpLoopback;
            return false;
        }

        return true;
    }

    public static (bool Success, string Message) ApplyProfile(NetworkProfile profile)
    {
        if (!TryValidateIpAddress(profile.IpAddress, out var ipError))
            return (false, ipError);

        if (!TryValidateSubnetMask(profile.SubnetMask, out var maskError))
            return (false, maskError);

        if (!TryValidateOptionalIpAddress(profile.DefaultGateway, out var gatewayError))
            return (false, Loc.GatewayPrefix + gatewayError);

        if (!TryValidateOptionalIpAddress(profile.Dns1, out var dns1Error))
            return (false, Loc.Dns1Prefix + dns1Error);

        if (!TryValidateOptionalIpAddress(profile.Dns2, out var dns2Error))
            return (false, Loc.Dns2Prefix + dns2Error);

        if (string.IsNullOrWhiteSpace(profile.Dns1) && !string.IsNullOrWhiteSpace(profile.Dns2))
            return (false, Loc.Dns2BeforeDns1);

        var adapter = FindAdapter(profile.AdapterId, profile.AdapterName, profile.Category);
        if (adapter == null)
        {
            return (false, Loc.AdapterNotFound(profile.AdapterName));
        }

        if (!AdapterNameSafePattern.IsMatch(adapter.Name))
            return (false, Loc.AdapterNameUnsafe);

        var escapedName = adapter.Name.Replace("\"", "\\\"");
        var ip = profile.IpAddress.Trim();
        var mask = profile.SubnetMask.Trim();
        var gateway = profile.DefaultGateway.Trim();

        var addressCommand = string.IsNullOrWhiteSpace(gateway)
            ? $"interface ip set address name=\"{escapedName}\" static {ip} {mask}"
            : $"interface ip set address name=\"{escapedName}\" static {ip} {mask} {gateway} 1";

        var addressResult = RunNetsh(addressCommand);
        if (!addressResult.Success)
            return addressResult;

        if (!string.IsNullOrWhiteSpace(profile.Dns1))
        {
            var dns1 = profile.Dns1.Trim();
            var dns1Result = RunNetsh($"interface ip set dns name=\"{escapedName}\" static {dns1} primary");
            if (!dns1Result.Success)
                return dns1Result;

            if (!string.IsNullOrWhiteSpace(profile.Dns2))
            {
                var dns2Result = RunNetsh($"interface ip add dns name=\"{escapedName}\" {profile.Dns2.Trim()} index=2");
                if (!dns2Result.Success)
                    return dns2Result;
            }
        }
        else
        {
            RunNetsh($"interface ip set dns name=\"{escapedName}\" dhcp");
        }

        return (true, $"'{profile.Name}' ayarları '{adapter.Name}' birimine uygulandı.");
    }

    public static (bool Success, string Message) ApplyDhcp(AdapterCategory category)
    {
        var adapters = GetAvailableAdapters()
            .Where(a => a.Category == category)
            .ToList();

        if (adapters.Count == 0)
        {
            var typeName = category == AdapterCategory.Ethernet ? Loc.TypeWired.ToLowerInvariant() : Loc.TypeWireless.ToLowerInvariant();
            return (false, Loc.NoActiveAdapter(typeName));
        }

        var applied = new List<string>();
        foreach (var adapter in adapters)
        {
            if (!AdapterNameSafePattern.IsMatch(adapter.Name))
                continue;

            var escapedName = adapter.Name.Replace("\"", "\\\"");
            var addressResult = RunNetsh($"interface ip set address name=\"{escapedName}\" dhcp");
            if (!addressResult.Success)
                return (false, $"'{adapter.Name}': {addressResult.Message}");

            var dnsResult = RunNetsh($"interface ip set dns name=\"{escapedName}\" dhcp");
            if (!dnsResult.Success)
                return (false, $"'{adapter.Name}' DNS: {dnsResult.Message}");

            applied.Add(adapter.Name);
        }

        if (applied.Count == 0)
            return (false, Loc.AdapterNameUnsafe);

        var categoryLabel = category == AdapterCategory.Ethernet ? "Kablolu" : "Kablosuz";
        var adapterList = string.Join(", ", applied);
        return (true, $"{categoryLabel} birim(ler) DHCP'ye alındı: {adapterList}");
    }

    private static (bool Success, string Message) RunNetsh(string arguments)
    {
        if (!IsAdministrator())
            return RunNetshElevated(arguments);

        return RunNetshProcess(arguments, useShellExecute: false);
    }

    private static (bool Success, string Message) RunNetshElevated(string arguments)
    {
        return RunNetshProcess(arguments, useShellExecute: true, verb: "runas");
    }

    private static (bool Success, string Message) RunNetshProcess(
        string arguments,
        bool useShellExecute,
        string? verb = null)
    {
        try
        {
            using var process = new System.Diagnostics.Process();
            process.StartInfo.FileName = "netsh";
            process.StartInfo.Arguments = arguments;
            process.StartInfo.UseShellExecute = useShellExecute;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.Verb = verb ?? string.Empty;

            if (!useShellExecute)
            {
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
            }

            process.Start();

            string output;
            string error;
            if (useShellExecute)
            {
                output = string.Empty;
                error = string.Empty;
            }
            else
            {
                output = process.StandardOutput.ReadToEnd();
                error = process.StandardError.ReadToEnd();
            }

            process.WaitForExit(15000);

            if (process.ExitCode == 0)
                return (true, string.Empty);

            var message = string.IsNullOrWhiteSpace(error) ? output : error;
            return (false, string.IsNullOrWhiteSpace(message)
                ? Loc.NetworkApplyFailed
                : message.Trim());
        }
        catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            return (false, Loc.AdminDenied);
        }
        catch (Exception ex)
        {
            return (false, Loc.CommandFailed(ex.Message));
        }
    }

    private static bool IsAdministrator()
    {
        using var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
        var principal = new System.Security.Principal.WindowsPrincipal(identity);
        return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
    }

    private static bool ShouldExclude(NetworkInterface ni)
    {
        if (ni.NetworkInterfaceType is NetworkInterfaceType.Loopback or NetworkInterfaceType.Tunnel)
            return true;

        var description = ni.Description.ToLowerInvariant();
        var name = ni.Name.ToLowerInvariant();

        if (description.Contains("bluetooth") || name.Contains("bluetooth"))
            return true;

        if (description.Contains("wan miniport") || description.Contains("miniport"))
            return true;

        if (description.Contains("virtual") || description.Contains("hyper-v") ||
            description.Contains("vmware") || description.Contains("vethernet") ||
            description.Contains("tap-windows") || description.Contains("wintun"))
            return true;

        return false;
    }

    private static AdapterCategory? ResolveCategory(NetworkInterface ni)
    {
        var description = ni.Description.ToLowerInvariant();
        var name = ni.Name.ToLowerInvariant();
        var combined = description + " " + name;

        if (ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
            return AdapterCategory.Wireless;

        if (combined.Contains("wireless") || combined.Contains("wi-fi") || combined.Contains("wifi") ||
            combined.Contains("802.11") || combined.Contains("wlan"))
            return AdapterCategory.Wireless;

        if (ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
        {
            if (combined.Contains("wireless") || combined.Contains("wi-fi") || combined.Contains("wifi"))
                return AdapterCategory.Wireless;

            return AdapterCategory.Ethernet;
        }

        if (combined.Contains("ethernet") || combined.Contains("gbe") ||
            (combined.Contains("realtek") && !combined.Contains("wireless")) ||
            (combined.Contains("intel") && combined.Contains("ethernet")))
            return AdapterCategory.Ethernet;

        return null;
    }
}
