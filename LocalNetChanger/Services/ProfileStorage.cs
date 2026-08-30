using System.Text.Json;
using System.Text.Json.Serialization;
using LocalNetChanger.Models;

namespace LocalNetChanger.Services;

public sealed class ProfileStorage
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    private readonly string _storePath;
    private ProfileStore _store = new();

    public ProfileStorage()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var folder = Path.Combine(appData, "LocalNetChanger");
        Directory.CreateDirectory(folder);
        _storePath = Path.Combine(folder, "profiles.json");
        MigrateLegacyStore(appData, folder);
        Load();
    }

    public IReadOnlyList<NetworkProfile> Profiles => _store.Profiles;

    public void Load()
    {
        if (!File.Exists(_storePath))
        {
            _store = new ProfileStore();
            return;
        }

        try
        {
            var json = File.ReadAllText(_storePath);
            _store = JsonSerializer.Deserialize<ProfileStore>(json, JsonOptions) ?? new ProfileStore();
        }
        catch
        {
            _store = new ProfileStore();
        }
    }

    public void Save()
    {
        var json = JsonSerializer.Serialize(_store, JsonOptions);
        File.WriteAllText(_storePath, json);
    }

    public void Add(NetworkProfile profile)
    {
        _store.Profiles.Add(profile);
        Save();
    }

    public void Update(NetworkProfile profile)
    {
        var index = _store.Profiles.FindIndex(p => p.Id == profile.Id);
        if (index >= 0)
        {
            _store.Profiles[index] = profile;
            Save();
        }
    }

    public void Delete(string id)
    {
        _store.Profiles.RemoveAll(p => p.Id == id);
        Save();
    }

    public IEnumerable<NetworkProfile> GetByCategory(AdapterCategory category)
    {
        return _store.Profiles
            .Where(p => p.Category == category)
            .OrderBy(p => p.Name, StringComparer.CurrentCultureIgnoreCase);
    }

    private static void MigrateLegacyStore(string appData, string newFolder)
    {
        var legacyPath = Path.Combine(appData, "NetChanger", "profiles.json");
        var newPath = Path.Combine(newFolder, "profiles.json");

        if (File.Exists(newPath) || !File.Exists(legacyPath))
            return;

        try
        {
            File.Copy(legacyPath, newPath);
        }
        catch
        {
            // Eski profiller taşınamazsa yeni boş depo kullanılır.
        }
    }
}
