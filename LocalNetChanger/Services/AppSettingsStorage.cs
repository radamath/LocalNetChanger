using System.Text.Json;
using System.Text.Json.Serialization;
using LocalNetChanger.Models;

namespace LocalNetChanger.Services;

public sealed class AppSettingsStorage
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    private readonly string _storePath;

    public AppSettings Settings { get; private set; } = new();

    public AppSettingsStorage()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "LocalNetChanger");
        Directory.CreateDirectory(folder);
        _storePath = Path.Combine(folder, "settings.json");
        Load();
    }

    public void Load()
    {
        if (!File.Exists(_storePath))
        {
            Settings = new AppSettings();
            return;
        }

        try
        {
            var json = File.ReadAllText(_storePath);
            Settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
        }
        catch
        {
            Settings = new AppSettings();
        }
    }

    public void Save()
    {
        var json = JsonSerializer.Serialize(Settings, JsonOptions);
        File.WriteAllText(_storePath, json);
    }

    public void SetLanguage(AppLanguage language)
    {
        Settings.Language = language;
        Save();
    }

    public void SetStartWithWindows(bool enabled)
    {
        Settings.StartWithWindows = enabled;
        Save();
    }

    public LastNetworkChoice? GetLastNetworkChoice(AdapterCategory category) =>
        category == AdapterCategory.Ethernet ? Settings.LastWired : Settings.LastWireless;

    public void SetLastNetworkChoice(AdapterCategory category, bool isDhcp, string? profileId = null)
    {
        var choice = new LastNetworkChoice { IsDhcp = isDhcp, ProfileId = profileId };

        if (category == AdapterCategory.Ethernet)
            Settings.LastWired = choice;
        else
            Settings.LastWireless = choice;

        Save();
    }
}
