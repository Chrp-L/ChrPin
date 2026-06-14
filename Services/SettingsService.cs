using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using ChrPin.Models;

namespace ChrPin.Services;

public sealed class SettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _settingsPath;

    public SettingsService()
    {
        var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ChrPin");
        Directory.CreateDirectory(directory);
        _settingsPath = Path.Combine(directory, "settings.json");
    }

    public Settings Load()
    {
        if (!File.Exists(_settingsPath))
        {
            return new Settings();
        }

        try
        {
            var settings = JsonSerializer.Deserialize<Settings>(File.ReadAllText(_settingsPath), JsonOptions) ?? new Settings();
            if (settings.Hotkey.IsLegacyDefault())
            {
                settings.Hotkey.Ctrl = false;
                Save(settings);
            }

            return settings;
        }
        catch
        {
            return new Settings();
        }
    }

    public void Save(Settings settings)
    {
        File.WriteAllText(_settingsPath, JsonSerializer.Serialize(settings, JsonOptions));
    }
}
