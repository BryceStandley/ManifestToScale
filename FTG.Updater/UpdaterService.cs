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
    private readonly string? _executableDirectory;
    private readonly string? _appDirectory;
    
    public string? CurrentAppDirectory => _executableDirectory;

    public class UpdateResult
    {
        public bool Result { get; set; }
        public string OutputInfo { get; set; }
        
        public UpdateResult(bool result, string outputInfo)
        {
            Result = result;
            OutputInfo = outputInfo;
        }
    }
    
    public UpdaterService()
    {
        _appDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        _executableDirectory = Path.GetDirectoryName(Environment.ProcessPath);
        _currentVersion = GetCurrentVersion();
    }

    public async Task<UpdateResult> CheckForUpdatesAsync(string outputInfo)
    {
        using var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("MyApp-Updater/1.0");
    
        var response = await client.GetStringAsync(
            $"https://api.github.com/repos/{_githubRepo}/releases/latest");
        
        var release = JsonSerializer.Deserialize<GitHubRelease>(response, GitHubJsonContext.Default.GitHubRelease);
        outputInfo = $"Latest version: {release?.TagName}\nCurrent version: {_currentVersion}\n";
        if (release?.TagName != null)
            return new UpdateResult(_currentVersion != null && IsNewerVersion(release.TagName, _currentVersion),
                outputInfo);
        outputInfo += "Failed to retrieve latest version information.\n";
        return new UpdateResult(false, outputInfo);

    }

    public async Task<UpdateResult> DownloadAndInstallUpdateAsync(IProgress<float> progress, string outputInfo)
    {
        try
        {
            var release = await GetLatestReleaseAsync();

            if (release?.Assets != null)
            {
                var asset = release.Assets.FirstOrDefault(a => 
                    a.Name != null &&
                    a.Name.EndsWith(".zip") && 
                    (a.Name.Contains("windows") || a.Name.Contains("ManifestToScale")));

                if (asset == null) 
                {
                    // Log available assets for debugging
                    var availableAssets = string.Join(", ", release.Assets.Select(a => a.Name));
                    outputInfo += $"No suitable asset found. Available assets: {availableAssets}\n";
                    throw new InvalidOperationException($"No suitable asset found. Available assets: {availableAssets}");
                }

                if (_executableDirectory != null)
                {
                    var tempPath = Path.Combine(_executableDirectory, "update_temp");
                    Directory.CreateDirectory(tempPath);


                    if (asset.BrowserDownloadUrl != null)
                        await DownloadFileAsync(asset.BrowserDownloadUrl,
                            Path.Combine(tempPath, "update.zip"), progress);
                    outputInfo += $"Downloaded... Now updating...\n";

                    var update = await ExtractAndReplaceAsync(tempPath, outputInfo);
                    if (update.Result)
                    {
                        outputInfo += update.OutputInfo;
                        outputInfo += $"Update completed successfully! New version: {release.TagName}\n";
                        return new UpdateResult(true, outputInfo);
                    }
                    else
                    {
                        outputInfo += update.OutputInfo;
                        outputInfo += "Update failed. Please try again later.\n";
                        return new UpdateResult(false, outputInfo);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            outputInfo += $"Error downloading and installing update: {ex.Message}\n";
            return new UpdateResult(false, outputInfo);
        }
        return new UpdateResult(false, outputInfo);
    }

    private async Task<UpdateResult> ExtractAndReplaceAsync(string tempPath, string outputInfo)
    {
        var zipPath = Path.Combine(tempPath, "update.zip");
        var extractPath = Path.Combine(tempPath, "extracted");
        
        if(Directory.Exists(extractPath))
        {
            // Cleanup previous extraction from failed or previous updates
            Directory.Delete(extractPath, true);
        }
        
        ZipFile.ExtractToDirectory(zipPath, extractPath);
        outputInfo += $"Extracted to {extractPath}\n";
        
        // Stop main app if running
        var processes = Process.GetProcessesByName("mts.exe");
        foreach (var process in processes)
        {
            outputInfo += $"Stopping process: {process.ProcessName} (ID: {process.Id})\n";
            process.Kill();
            await process.WaitForExitAsync();
        }
        
        
        var filesToUpdate = Directory.GetFiles(Path.Combine(extractPath,"ManifestToScale", "app"),"*", 
            SearchOption.AllDirectories);
        
        foreach (var file in filesToUpdate)
        {
            var fileName = Path.GetFileName(file);
            if(fileName == "ManifestToScale.exe" || fileName == "ManifestToScale.pdb")
            {
                // Skip the updater app executable and its pdb
                continue;
            }

            if (_executableDirectory != null)
            {
                var targetPath = Path.Combine(_executableDirectory, "../app", fileName);
                outputInfo += $"Copying {file} to {targetPath}\n";
            
                Directory.CreateDirectory(Path.GetDirectoryName(targetPath) ?? string.Empty);
                File.Copy(file, targetPath, true);
            }
        }
        
        // Cleanup
        Directory.Delete(tempPath, true);
        return new UpdateResult(true, outputInfo);
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
            Console.WriteLine($"Comparing versions: latest={latest}, current={current}");
            
            return latest > current;
        }
        catch (Exception)
        {
            // If version parsing fails, assume update is available
            return true;
        }
    }

    private async Task<GitHubRelease?> GetLatestReleaseAsync()
    {
        using var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("ManifestToScale-Updater/1.0");
    
        var response = await client.GetStringAsync(
            $"https://api.github.com/repos/{_githubRepo}/releases/latest");
    
        // Use the source generator context
        return JsonSerializer.Deserialize<GitHubRelease>(response, GitHubJsonContext.Default.GitHubRelease);
    }

    private async Task DownloadFileAsync(string url, string filePath, IProgress<float> progress)
    {
        using var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("ManifestToScale-Updater/1.0");
        
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
            if (_executableDirectory != null)
            {
                var mainAppPath = Path.Combine(_executableDirectory, "../app", "mts.exe");
                if (File.Exists(mainAppPath))
                {
                    var versionInfo = FileVersionInfo.GetVersionInfo(mainAppPath);
                    var appVersion = versionInfo.ProductVersion?.Split("+")[0]; // Get the first part before any build metadata
                    return appVersion ?? "1.0.0";
                }
            }

            // Fallback to an updater version
            var assembly = Assembly.GetExecutingAssembly();
            return assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion ?? "1.0.0.0";
        }
        catch (Exception)
        {
            return "1.0.0";
        }
    }
    
}

[JsonSerializable(typeof(GitHubRelease))]
[JsonSerializable(typeof(GitHubAsset))]
[JsonSerializable(typeof(GitHubUser))]
[JsonSerializable(typeof(GitHubAsset[]))]
public partial class GitHubJsonContext : JsonSerializerContext
{
}

// Models for GitHub API
public class GitHubRelease
{
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("assets_url")]
    public string? AssetsUrl { get; set; }

    [JsonPropertyName("upload_url")]
    public string? UploadUrl { get; set; }

    [JsonPropertyName("html_url")]
    public string? HtmlUrl { get; set; }

    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("author")]
    public GitHubUser? Author { get; set; }

    [JsonPropertyName("node_id")]
    public string? NodeId { get; set; }

    [JsonPropertyName("tag_name")]
    public string? TagName { get; set; }

    [JsonPropertyName("target_commitish")]
    public string? TargetCommitish { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("draft")]
    public bool Draft { get; set; }

    [JsonPropertyName("prerelease")]
    public bool Prerelease { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("published_at")]
    public DateTime PublishedAt { get; set; }

    [JsonPropertyName("assets")]
    public GitHubAsset[]? Assets { get; set; }

    [JsonPropertyName("tarball_url")]
    public string? TarballUrl { get; set; }

    [JsonPropertyName("zipball_url")]
    public string? ZipballUrl { get; set; }

    [JsonPropertyName("body")]
    public string? Body { get; set; }
}

public class GitHubAsset
{
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("node_id")]
    public string? NodeId { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("label")]
    public string? Label { get; set; }

    [JsonPropertyName("uploader")]
    public GitHubUser? Uploader { get; set; }

    [JsonPropertyName("content_type")]
    public string? ContentType { get; set; }

    [JsonPropertyName("state")]
    public string? State { get; set; }

    [JsonPropertyName("size")]
    public long Size { get; set; }

    [JsonPropertyName("digest")]
    public string? Digest { get; set; }

    [JsonPropertyName("download_count")]
    public int DownloadCount { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [JsonPropertyName("browser_download_url")]
    public string? BrowserDownloadUrl { get; set; }
}

public class GitHubUser
{
    [JsonPropertyName("login")]
    public string? Login { get; set; }

    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("node_id")]
    public string? NodeId { get; set; }

    [JsonPropertyName("avatar_url")]
    public string? AvatarUrl { get; set; }

    [JsonPropertyName("gravatar_id")]
    public string? GravatarId { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("html_url")]
    public string? HtmlUrl { get; set; }

    [JsonPropertyName("followers_url")]
    public string? FollowersUrl { get; set; }

    [JsonPropertyName("following_url")]
    public string? FollowingUrl { get; set; }

    [JsonPropertyName("gists_url")]
    public string? GistsUrl { get; set; }

    [JsonPropertyName("starred_url")]
    public string? StarredUrl { get; set; }

    [JsonPropertyName("subscriptions_url")]
    public string? SubscriptionsUrl { get; set; }

    [JsonPropertyName("organizations_url")]
    public string? OrganizationsUrl { get; set; }

    [JsonPropertyName("repos_url")]
    public string? ReposUrl { get; set; }

    [JsonPropertyName("events_url")]
    public string? EventsUrl { get; set; }

    [JsonPropertyName("received_events_url")]
    public string? ReceivedEventsUrl { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("user_view_type")]
    public string? UserViewType { get; set; }

    [JsonPropertyName("site_admin")]
    public bool SiteAdmin { get; set; }
}