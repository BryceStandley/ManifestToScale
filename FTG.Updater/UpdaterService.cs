using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FTG.Updater;

public class UpdaterService
{
    private readonly string _githubRepo = "BryceStandley/ManifestToScale";
    private readonly string? _currentVersion;
    private readonly string? _appDirectory;
    
    public UpdaterService()
    {
        _currentVersion = GetCurrentVersion();
        _appDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
    }

    public async Task<bool> CheckForUpdatesAsync()
    {
        using var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("ManifestToScale-Updater/1.0");
        
        var response = await client.GetStringAsync(
            $"https://api.github.com/repos/{_githubRepo}/releases/latest");
        
        var release = JsonSerializer.Deserialize<GitHubRelease>(response);
        
        return IsNewerVersion(release.TagName, _currentVersion);
    }

    public async Task<bool> DownloadAndInstallUpdateAsync(IProgress<float> progress)
    {
        try
        {
            var release = await GetLatestReleaseAsync();
            var asset = release.Assets.FirstOrDefault(a => 
                a.Name.EndsWith(".zip") && a.Name.Contains("windows"));

            if (asset == null) return false;

            // Download update
            var tempPath = Path.Combine(_appDirectory, "update_temp");
            Directory.CreateDirectory(tempPath);
            
            await DownloadFileAsync(asset.BrowserDownloadUrl, 
                Path.Combine(tempPath, "update.zip"), progress);

            // Extract and replace files
            await ExtractAndReplaceAsync(tempPath);
            
            return true;
        }
        catch (Exception ex)
        {
            // Log error
            return false;
        }
    }

    private async Task ExtractAndReplaceAsync(string tempPath)
    {
        var zipPath = Path.Combine(tempPath, "update.zip");
        var extractPath = Path.Combine(tempPath, "extracted");
        
        ZipFile.ExtractToDirectory(zipPath, extractPath);
        
        // Stop main app if running
        var processes = Process.GetProcessesByName("MyApp");
        foreach (var process in processes)
        {
            process.Kill();
            await process.WaitForExitAsync();
        }

        // Replace files (preserve user data)
        var filesToUpdate = Directory.GetFiles(extractPath, "*", 
            SearchOption.AllDirectories);
        
        foreach (var file in filesToUpdate)
        {
            var relativePath = Path.GetRelativePath(extractPath, file);
            var targetPath = Path.Combine(_appDirectory, relativePath);
            
            // Skip user data directory
            if (relativePath.StartsWith("app_data")) continue;
            
            Directory.CreateDirectory(Path.GetDirectoryName(targetPath) ?? string.Empty);
            File.Copy(file, targetPath, true);
        }
        
        // Cleanup
        Directory.Delete(tempPath, true);
    }
    
    private bool IsNewerVersion(string latestVersion, string currentVersion)
    {
        // Remove 'v' prefix if present
        latestVersion = latestVersion.TrimStart('v');
        currentVersion = currentVersion.TrimStart('v');
        
        try
        {
            var latest = new Version(latestVersion);
            var current = new Version(currentVersion);
            
            return latest > current;
        }
        catch (Exception)
        {
            // If version parsing fails, assume update is available
            return true;
        }
    }

    private async Task<GitHubRelease> GetLatestReleaseAsync()
    {
        using var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("MyApp-Updater/1.0");
        
        var response = await client.GetStringAsync(
            $"https://api.github.com/repos/{_githubRepo}/releases/latest");
        
        return JsonSerializer.Deserialize<GitHubRelease>(response);
    }

    private async Task DownloadFileAsync(string url, string filePath, IProgress<float> progress)
    {
        using var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("MyApp-Updater/1.0");
        
        using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();
        
        var totalBytes = response.Content.Headers.ContentLength ?? 0;
        var downloadedBytes = 0L;
        
        using var contentStream = await response.Content.ReadAsStreamAsync();
        using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
        
        var buffer = new byte[8192];
        int bytesRead;
        
        while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
        {
            await fileStream.WriteAsync(buffer, 0, bytesRead);
            downloadedBytes += bytesRead;
            
            if (totalBytes > 0)
            {
                var progressPercentage = (float)((downloadedBytes * 100) / totalBytes) * 0.1f;
                progress?.Report(progressPercentage);
            }
        }
    }

    private string GetCurrentVersion()
    {
        try
        {
            // Try to get version from the main app executable
            var mainAppPath = Path.Combine(_appDirectory, "MyApp.exe");
            if (File.Exists(mainAppPath))
            {
                var versionInfo = FileVersionInfo.GetVersionInfo(mainAppPath);
                return versionInfo.ProductVersion ?? versionInfo.FileVersion ?? "1.0.0";
            }
            
            // Fallback to updater version
            var assembly = Assembly.GetExecutingAssembly();
            return assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion ?? "1.0.0";
        }
        catch (Exception)
        {
            return "1.0.0";
        }
    }
    
}

// Models for GitHub API
public class GitHubRelease
{
    [JsonPropertyName("tag_name")]
    public string TagName { get; set; }
    
    [JsonPropertyName("assets")]
    public GitHubAsset[] Assets { get; set; }
}

public class GitHubAsset
{
    [JsonPropertyName("name")]
    public string Name { get; set; }
    
    [JsonPropertyName("browser_download_url")]
    public string BrowserDownloadUrl { get; set; }
}