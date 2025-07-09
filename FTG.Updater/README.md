# FTG.Updater

A .NET 9 MAUI application that automatically checks for, downloads, and installs updates for the Manifest To Scale (FTG.MAUI) application from GitHub releases.

## Overview

FTG.Updater is an automatic update utility that ensures users always have the latest version of the Manifest To Scale application. It connects to GitHub releases, compares versions, downloads updates, and seamlessly installs them before launching the main application.

## Features

- **Automatic Update Checking**: Queries GitHub API for latest releases
- **Version Comparison**: Intelligent semantic version comparison
- **Download Progress**: Real-time download progress indication
- **Silent Installation**: Automatic extraction and file replacement
- **Process Management**: Safely stops running applications before updating
- **Fallback Launch**: Launches current version if update fails
- **User Interface**: Clean, informative progress interface

## How It Works

### Update Flow
1. **Startup**: Updater launches and checks for updates
2. **Version Check**: Compares current version with latest GitHub release
3. **Download**: Downloads update package if newer version available
4. **Installation**: Extracts and replaces application files
5. **Launch**: Starts the updated main application
6. **Exit**: Updater closes automatically

### Deployment Structure
```
AppFolder/
├── updater/
│   └── ManifestToScale.exe    # FTG.Updater executable
└── app/
    └── mts.exe                # FTG.MAUI main application
```

## Configuration

### GitHub Repository
```csharp
private readonly string _githubRepo = "BryceStandley/ManifestToScale";
```

### Version Detection
- Reads version from main application executable (`mts.exe`)
- Falls back to updater assembly version if main app not found
- Supports semantic versioning (e.g., "1.2.1")

## Update Process

### Version Checking
```csharp
// Compares versions using System.Version
var latest = new Version(latestVersion);
var current = new Version(currentVersion);
return latest > current;
```

### File Download
- Downloads ZIP assets from GitHub releases
- Shows real-time progress to user
- Handles download errors gracefully

### Installation Steps
1. **Stop Processes**: Terminates running `mts.exe` processes
2. **Extract Files**: Unzips update package to temporary location
3. **File Replacement**: Copies new files to application directory
4. **Cleanup**: Removes temporary files
5. **Launch**: Starts updated application

## User Interface

### Components
- **Status Text**: Current operation status
- **Progress Bar**: Visual progress indicator
- **Output Log**: Detailed operation information
- **Launch Button**: Manual launch option (enabled after completion)

### Status Messages
- "Checking for updates..."
- "Update available. Downloading..."
- "Update completed successfully!"
- "No updates available."
- "Update failed... Launching current version."

## GitHub Integration

### API Endpoints
```
GET https://api.github.com/repos/{owner}/{repo}/releases/latest
```

### Asset Selection
- Looks for ZIP files containing "windows" or "ManifestToScale"
- Downloads first matching asset from latest release
- Handles asset selection errors with detailed logging

### Rate Limiting
- Uses appropriate User-Agent headers
- Handles GitHub API rate limiting gracefully

## Error Handling

### Robust Error Management
- **Network Errors**: Handles API connection failures
- **Download Errors**: Manages incomplete downloads
- **File System Errors**: Handles file access permissions
- **Process Errors**: Manages running application conflicts

### Fallback Behavior
- Always attempts to launch main application
- Shows error details in output log
- Provides manual launch option
- Never leaves user without working application

## Security

### Safe Update Process
- Validates downloaded files before extraction
- Creates backup through temporary folder staging
- Atomic file replacement to prevent corruption
- Handles interrupted updates gracefully

### Process Safety
- Properly terminates running processes
- Waits for clean process exit
- Prevents file access conflicts

## Dependencies

### Core Libraries
- **Microsoft.Maui.Controls**: MAUI framework for UI
- **Newtonsoft.Json**: JSON processing for GitHub API
- **System.IO.Compression**: ZIP file extraction

### Platform Support
- **Windows**: Primary target platform
- **Self-Contained**: Includes .NET runtime

## Development

### Project Structure
```
FTG.Updater/
├── MainPage.xaml/.cs        # Main UI and update logic
├── UpdaterService.cs        # GitHub integration and update logic
├── AppShell.xaml/.cs       # Application shell
└── Platforms/              # Platform-specific code
    └── Windows/            # Windows platform implementation
```

### Key Classes

#### UpdaterService
- GitHub API integration
- Version comparison logic
- Download and installation management

#### MainPage
- User interface coordination
- Progress reporting
- Error display and handling

## JSON Serialization

### Source Generators
Uses .NET source generators for efficient JSON serialization:
```csharp
[JsonSerializable(typeof(GitHubRelease))]
[JsonSerializable(typeof(GitHubAsset))]
public partial class GitHubJsonContext : JsonSerializerContext
```

### GitHub API Models
- **GitHubRelease**: Release information
- **GitHubAsset**: Downloadable files
- **GitHubUser**: Author information

## Building and Deployment

### Build Configuration
```xml
<SelfContained>true</SelfContained>
<RuntimeIdentifier>win-x64</RuntimeIdentifier>
<PublishSingleFile>true</PublishSingleFile>
```

### Deployment
1. Build updater as self-contained executable
2. Place in `/updater/` folder relative to main application
3. Configure main application to launch updater on startup

## Usage Integration

### Main Application Integration
The main application should launch the updater:
```bash
# Instead of launching mts.exe directly:
./updater/ManifestToScale.exe
```

### Automatic Updates
- No user intervention required
- Runs silently in background
- Provides visual feedback during process
- Launches main app automatically when complete

## Troubleshooting

### Common Issues
- **Network Connectivity**: Check internet connection for GitHub API access
- **File Permissions**: Ensure write access to application directory
- **Process Conflicts**: Close main application before running updater manually

### Logging
Detailed operation logging in the output window helps diagnose issues during the update process.