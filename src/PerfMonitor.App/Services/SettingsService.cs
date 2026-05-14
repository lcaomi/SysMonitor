using System.IO;
using System.Text.Json;
using PerfMonitor.App.Models;

namespace PerfMonitor.App.Services;

public class SettingsService
{
    private static readonly string AppDataPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WindowsPerfMonitor");

    private static readonly string SettingsFilePath =
        Path.Combine(AppDataPath, "settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsFilePath))
            {
                var defaults = new AppSettings();
                Save(defaults);
                return defaults;
            }

            var json = File.ReadAllText(SettingsFilePath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
            if (settings is null)
                throw new InvalidOperationException("Deserialized settings was null");

            return settings;
        }
        catch
        {
            return HandleCorruptConfig();
        }
    }

    public void Save(AppSettings settings)
    {
        try
        {
            Directory.CreateDirectory(AppDataPath);
            var json = JsonSerializer.Serialize(settings, JsonOptions);
            File.WriteAllText(SettingsFilePath, json);
        }
        catch
        {
            // Silently fail — don't crash if we can't write config
        }
    }

    private AppSettings HandleCorruptConfig()
    {
        try
        {
            if (File.Exists(SettingsFilePath))
            {
                var backupPath = SettingsFilePath + ".backup";
                File.Move(SettingsFilePath, backupPath, overwrite: true);
            }
        }
        catch { }

        var defaults = new AppSettings();
        Save(defaults);
        return defaults;
    }
}
