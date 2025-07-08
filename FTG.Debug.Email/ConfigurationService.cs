namespace FTG.Debug.Email;

using System.Text.Json;
using System.Text.Json.Serialization;
using System.Reflection;

public class ConfigurationService
{
    private MailgunSettings? _settings;
    private const string SettingsFileName = "appsettings.json";
    
    // JSON Source Generator Context
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        TypeInfoResolver = AppConfigJsonContext.Default,
        WriteIndented = true
    };

    public async Task<MailgunSettings> GetMailgunSettingsAsync()
    {
        if (_settings != null)
            return _settings;

        try
        {
            // First try to load from embedded resource (appsettings.json)
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = $"{assembly.GetName().Name}.{SettingsFileName}";

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream != null)
            {
                using var reader = new StreamReader(stream);
                var json = await reader.ReadToEndAsync();
                var config = JsonSerializer.Deserialize(json, AppConfigJsonContext.Default.AppConfig);
                
                if (config?.MailgunSettings != null)
                {
                    _settings = config.MailgunSettings;
                    return _settings;
                }
            }

            // Fallback: Try to load from file system (for development)
            var appDataPath = FileSystem.AppDataDirectory;
            var settingsPath = Path.Combine(appDataPath, SettingsFileName);
            
            if (File.Exists(settingsPath))
            {
                var json = await File.ReadAllTextAsync(settingsPath);
                var config = JsonSerializer.Deserialize(json, AppConfigJsonContext.Default.AppConfig);
                
                if (config?.MailgunSettings != null)
                {
                    _settings = config.MailgunSettings;
                    return _settings;
                }
            }

            // If no settings found, return empty settings
            _settings = new MailgunSettings();
            return _settings;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading configuration: {ex.Message}");
            _settings = new MailgunSettings();
            return _settings;
        }
    }

    public async Task SaveApiKeyAsync(string apiKey)
    {
        try
        {
            var settings = await GetMailgunSettingsAsync();
            settings.ApiKey = apiKey;

            var config = new AppConfig { MailgunSettings = settings };
            var json = JsonSerializer.Serialize(config, AppConfigJsonContext.Default.AppConfig);

            var appDataPath = FileSystem.AppDataDirectory;
            var settingsPath = Path.Combine(appDataPath, SettingsFileName);
            
            await File.WriteAllTextAsync(settingsPath, json);
            
            System.Diagnostics.Debug.WriteLine($"API Key saved to: {settingsPath}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving API key: {ex.Message}");
        }
    }

    public async Task<bool> HasValidApiKeyAsync()
    {
        var settings = await GetMailgunSettingsAsync();
        return !string.IsNullOrWhiteSpace(settings.ApiKey) && 
               settings.ApiKey != "your-mailgun-api-key-here" &&
               settings.ApiKey != "your-actual-mailgun-api-key-here";
    }
}

public class AppConfig
{
    public MailgunSettings? MailgunSettings { get; set; }
}

public class MailgunSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string Domain { get; set; } = "ftg.vectorpixel.net";
    public string DefaultFrom { get; set; } = "noreply@ftg.vectorpixel.net";
}

// JSON Source Generator Context for AOT compatibility
[JsonSerializable(typeof(AppConfig))]
[JsonSerializable(typeof(MailgunSettings))]
[JsonSourceGenerationOptions(WriteIndented = true)]
internal partial class AppConfigJsonContext : JsonSerializerContext
{
}