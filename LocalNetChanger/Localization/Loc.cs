using System.Globalization;
using LocalNetChanger.Models;
using LocalNetChanger.Services;

namespace LocalNetChanger.Localization;

public static class Loc
{
    private static bool _english;

    public static event Action? Changed;

    public static void Initialize(AppSettingsStorage storage)
    {
        ApplyLanguage(storage.Settings.Language);
    }

    public static void ApplyLanguage(AppLanguage language)
    {
        _english = language switch
        {
            AppLanguage.English => true,
            AppLanguage.Turkish => false,
            _ => !CultureInfo.CurrentUICulture.TwoLetterISOLanguageName
                .Equals("tr", StringComparison.OrdinalIgnoreCase)
        };

        Changed?.Invoke();
    }

    public static string AppName => "LocalNetChanger";

    public static string ControlPanel => T("Kontrol Paneli", "Control Panel");
    public static string TabProfiles => T("Profiller", "Profiles");
    public static string TabApplication => T("Ayarlar", "Settings");

    public static string MenuControlPanel => T("Kontrol Paneli", "Control Panel");
    public static string MenuChangeNetwork => T("Ağ Değiştir", "Change Network");
    public static string MenuWired => T("Kablolu", "Wired");
    public static string MenuWireless => T("Kablosuz", "Wireless");
    public static string MenuDhcp => "DHCP";
    public static string MenuExit => T("Çıkış", "Exit");

    public static string ColProfile => T("Profil", "Profile");
    public static string ColType => T("Tür", "Type");
    public static string ColAdapter => T("Ağ Birimi", "Adapter");
    public static string ColIp => T("IP Adresi", "IP Address");
    public static string ColSubnet => T("Alt Ağ Maskesi", "Subnet Mask");
    public static string ColGateway => T("Ağ Geçidi", "Gateway");
    public static string ColDns1 => "DNS 1";
    public static string ColDns2 => "DNS 2";

    public static string TypeWired => T("Kablolu", "Wired");
    public static string TypeWireless => T("Kablosuz", "Wireless");

    public static string AddWired => T("+ Kablolu", "+ Wired");
    public static string AddWireless => T("+ Kablosuz", "+ Wireless");
    public static string Edit => T("Düzenle", "Edit");
    public static string Delete => T("Sil", "Delete");
    public static string Hide => T("Sistem Tepsisine Küçült", "Minimize to tray");
    public static string CloseApp => T("Uygulamayı Kapat", "Close the App");

    public static string LanguageLabel => T("Dil", "Language");
    public static string LanguageSystem => T("Sistem dili", "System default");
    public static string LanguageTurkish => "Türkçe";
    public static string LanguageEnglish => "English";

    public static string StartupLabel => T("Windows başlangıcında çalıştır", "Run at Windows startup");
    public static string StartupYes => T("Evet", "Yes");
    public static string StartupNo => T("Hayır", "No");

    public static string DeleteProfileTitle => T("Profili Sil", "Delete Profile");
    public static string DeleteProfileMessage(string name) => T(
        $"'{name}' profilini silmek istediğinize emin misiniz?",
        $"Delete profile '{name}'?");

    public static string NewProfile => T("Yeni Profil", "New Profile");
    public static string EditProfile => T("Profili Düzenle", "Edit Profile");
    public static string ProfileName => T("Profil Adı:", "Profile Name:");
    public static string Adapter => T("Ağ Birimi:", "Adapter:");
    public static string Save => T("Kaydet", "Save");
    public static string Cancel => T("İptal", "Cancel");
    public static string Validation => T("Doğrulama", "Validation");

    public static string ProfileNameRequired => T("Profil adı zorunludur.", "Profile name is required.");
    public static string AdapterRequired => T("Bir ağ birimi seçin.", "Select a network adapter.");
    public static string Dns2RequiresDns1 => T(
        "DNS 2 girmeden önce DNS 1 belirtmelisiniz.",
        "Specify DNS 1 before DNS 2.");

    public static string IpRequired => T("IP adresi zorunludur.", "IP address is required.");
    public static string IpInvalid => T("Geçerli bir IPv4 adresi girin.", "Enter a valid IPv4 address.");
    public static string IpLoopback => T("Loopback adresi kullanılamaz.", "Loopback address cannot be used.");
    public static string SubnetRequired => T("Alt ağ maskesi zorunludur.", "Subnet mask is required.");
    public static string SubnetInvalid => T(
        "Geçerli bir alt ağ maskesi girin (ör. 255.255.255.0).",
        "Enter a valid subnet mask (e.g. 255.255.255.0).");
    public static string SubnetZero => T("Alt ağ maskesi sıfır olamaz.", "Subnet mask cannot be zero.");
    public static string AdminDenied => T("Yönetici izni verilmedi.", "Administrator permission was denied.");
    public static string NetworkApplyFailed => T("Ağ ayarları uygulanamadı.", "Network settings could not be applied.");
    public static string ProfileAlreadyActive(string name) => T(
        $"'{name}' profili zaten aktif.",
        $"Profile '{name}' is already active.");
    public static string DhcpAlreadyActive(string category) => T(
        $"{category} bağlantısı zaten DHCP kullanıyor.",
        $"{category} connection is already using DHCP.");
    public static string CommandFailed(string detail) => T(
        $"Komut çalıştırılamadı: {detail}",
        $"Command failed: {detail}");

    public static string AdapterNotFound(string name) => T(
        $"'{name}' ağ birimi bulunamadı. Bağlantı takılı ve etkin olduğundan emin olun.",
        $"Adapter '{name}' was not found. Make sure it is connected and enabled.");

    public static string AdapterNameUnsafe => T(
        "Ağ birimi adı güvenlik nedeniyle işlenemedi.",
        "Adapter name could not be processed for security reasons.");

    public static string NoActiveAdapter(string typeName) => T(
        $"Etkin {typeName} ağ birimi bulunamadı.",
        $"No active {typeName} network adapter was found.");

    public static string GatewayPrefix => T("Varsayılan ağ geçidi: ", "Default gateway: ");
    public static string Dns1Prefix => "DNS 1: ";
    public static string Dns2Prefix => "DNS 2: ";
    public static string Dns2BeforeDns1 => T(
        "DNS 2 girmeden önce DNS 1 belirtmelisiniz.",
        "Specify DNS 1 before DNS 2.");

    private static string T(string turkish, string english) => _english ? english : turkish;
}
