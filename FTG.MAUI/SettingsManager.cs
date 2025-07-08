namespace FTG.MAUI;

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

[JsonSerializable(typeof(List<string>))]
public partial class RecentFilesContext : JsonSerializerContext { }


public static class SettingsManager
{
    private const string OutputFolderPathKey = "OutputFolderPath";
    private const string InputFolderPathKey = "InputFolderPath";
    private const string WindowSizeKey = "WindowSize";
    private const string RecentFilesKey = "RecentFiles";
    private const string ProcessingSettingsKey = "ProcessingSettings";

    // Output folder path
    public static string GetOutputFolderPath()
    {
        var savedPath = Preferences.Get(OutputFolderPathKey, string.Empty);
        if (!string.IsNullOrEmpty(savedPath) && Directory.Exists(savedPath))
        {
            return savedPath;
        }
        return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    }

    public static void SetOutputFolderPath(string path)
    {
        if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
        {
            Preferences.Set(OutputFolderPathKey, path);
        }
    }

    // Input folder path (remembers last folder where files were selected)
    public static string GetLastInputFolderPath()
    {
        var savedPath = Preferences.Get(InputFolderPathKey, string.Empty);
        if (!string.IsNullOrEmpty(savedPath) && Directory.Exists(savedPath))
        {
            return savedPath;
        }
        return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    }

    public static void SetLastInputFolderPath(string filePath)
    {
        if (string.IsNullOrEmpty(filePath)) return;
        var folderPath = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(folderPath) && Directory.Exists(folderPath))
        {
            Preferences.Set(InputFolderPathKey, folderPath);
        }
    }

    // Window size
    [RequiresUnreferencedCode("WindowSize serialization may not work correctly if types are not preserved.")]
    public static (double Width, double Height) GetWindowSize()
    {
        var json = Preferences.Get(WindowSizeKey, string.Empty);
        if (string.IsNullOrEmpty(json)) return (1200, 800); // Default size
        try
        {
            var size = JsonSerializer.Deserialize<WindowSize>(json);
            if (size != null) return (size.Width, size.Height);
        }
        catch
        {
            // If deserialization fails, return default
        }
        return (1200, 800); // Default size
    }

    [RequiresUnreferencedCode("WindowSize serialization may not work correctly if types are not preserved.")]
    public static void SetWindowSize(double width, double height)
    {
        var size = new WindowSize { Width = width, Height = height };
        var json = JsonSerializer.Serialize(size);
        Preferences.Set(WindowSizeKey, json);
    }

    // Recent files
    [RequiresUnreferencedCode("RecentFiles serialization may not work correctly if types are not preserved.")]
    public static List<string> GetRecentFiles()
    {
        var json = Preferences.Get(RecentFilesKey, "[]");
        var options = new JsonSerializerOptions
        {
            TypeInfoResolver = RecentFilesContext.Default
        };
        return JsonSerializer.Deserialize<List<string>>(json, options) ?? new List<string>();

    }

    [RequiresUnreferencedCode("RecentFiles serialization may not work correctly if types are not preserved.")]
    public static void AddRecentFile(string filePath)
    {
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            return;

        var recentFiles = GetRecentFiles();
        
        // Remove if already exists
        recentFiles.RemoveAll(f => string.Equals(f, filePath, StringComparison.OrdinalIgnoreCase));
        
        // Add to beginning
        recentFiles.Insert(0, filePath);
        
        // Keep only last 10 files
        if (recentFiles.Count > 10)
        {
            recentFiles = recentFiles.Take(10).ToList();
        }

        var options = new JsonSerializerOptions
        {
            TypeInfoResolver = RecentFilesContext.Default
        };
        var json = JsonSerializer.Serialize(recentFiles, options);
        Preferences.Set(RecentFilesKey, json);

    }

    // Processing settings
    [RequiresUnreferencedCode("ProcessingSettings serialization may not work correctly if types are not preserved.")]
    public static ProcessingSettings GetProcessingSettings()
    {
        var json = Preferences.Get(ProcessingSettingsKey, string.Empty);
        if (string.IsNullOrEmpty(json)) return new ProcessingSettings();
        try
        {
            return JsonSerializer.Deserialize<ProcessingSettings>(json) ?? new ProcessingSettings();
        }
        catch
        {
            // If deserialization fails, return default
        }
        return new ProcessingSettings();
    }

    [RequiresUnreferencedCode("ProcessingSettings serialization may not work correctly if types are not preserved.")]
    public static void SetProcessingSettings(ProcessingSettings settings)
    {
        var json = JsonSerializer.Serialize(settings);
        Preferences.Set(ProcessingSettingsKey, json);
    }

    // Clear all settings
    public static void ClearAllSettings()
    {
        Preferences.Clear();
    }

    // Clear specific setting
    public static void ClearSetting(string key)
    {
        Preferences.Remove(key);
    }
}

// Helper classes for complex settings
public class WindowSize
{
    public double Width { get; set; }
    public double Height { get; set; }
}

public class ProcessingSettings
{
    public bool CreateBackup { get; set; } = true;
    public bool AddTimestamp { get; set; } = true;
    public bool ShowDetailedOutput { get; set; } = true;
    public string FilePrefix { get; set; } = "processed_";
    public string DateFormat { get; set; } = "yyyyMMdd_HHmmss";
}