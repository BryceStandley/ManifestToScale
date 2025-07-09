# FTG.MAUI

A .NET 9 MAUI desktop application for processing manifest files and generating scale interface XML files. Provides a user-friendly Windows interface for the FTG.Core processing capabilities.

## Overview

FTG.MAUI is a cross-platform desktop application built with .NET MAUI that provides a graphical interface for processing Fresh To Go and Azura Fresh manifest files. It serves as a standalone desktop client for users who need to convert PDF, CSV, and Excel manifest files into XML format for scale integration systems.

## Features

- **File Processing**: Support for PDF, CSV, and XLSX manifest files
- **User-Friendly Interface**: Intuitive file selection and processing workflow
- **Progress Tracking**: Real-time processing progress with detailed output
- **Settings Persistence**: Automatic saving of user preferences and paths
- **Output Management**: Organized output folders with unique job identifiers
- **File Explorer Integration**: Quick access to output folders
- **Error Handling**: Comprehensive error reporting and validation

## Supported File Formats

### Input Files
- **PDF**: Fresh To Go manifest documents
- **CSV**: Azura Fresh comma-separated data files
- **XLSX**: Excel spreadsheets with manifest data

### Output Files
- **Receipt XML**: `.rcxml` files for scale receipt processing
- **Shipment XML**: `.shxml` files for scale shipment processing
- **Excel**: `.xlsx` converted manifest data (PDF input only)
- **CSV**: Converted manifest data in CSV format (PDF input only)

## User Interface

### Main Features
- **Input File Selection**: Browse and select manifest files
- **Output Folder Configuration**: Set and remember output directory
- **Processing Controls**: Start processing with validation
- **Progress Indicator**: Visual progress bar and status updates
- **Output Log**: Detailed processing information and results
- **Quick Actions**: Open output folder directly from the app

### Processing Workflow
1. **Select Input File**: Choose PDF, CSV, or XLSX manifest file
2. **Set Output Folder**: Configure where processed files will be saved
3. **Start Processing**: Click "Start Processing" to begin conversion
4. **Monitor Progress**: Watch real-time progress and detailed logs
5. **Access Results**: Use "Open Output" to view generated files

## Application Configuration

### Window Settings
- **Default Size**: 800x800 pixels
- **Resizable**: Min 800x800, Max 1920x1080
- **Self-Contained**: Single executable deployment

### File Management
- **Unique Job Folders**: Each processing job creates a unique GUID-named folder
- **Scale Interface Files**: Separate subfolder for XML files ready for scale import
- **Automatic Cleanup**: Optional cleanup of temporary files

## Dependencies

### Core Libraries
- **Microsoft.Maui.Controls**: MAUI framework
- **CommunityToolkit.Maui**: Additional MAUI controls and utilities
- **FTG.Core**: Core processing logic (project reference)

### Platform Support
- **Windows**: Primary target platform (Windows 10/11)
- **Self-Contained**: Includes .NET runtime in deployment

## Installation & Deployment

### Build Configuration
```xml
<SelfContained>true</SelfContained>
<RuntimeIdentifier>win-x64</RuntimeIdentifier>
<PublishSingleFile>true</PublishSingleFile>
```

### Application Properties
- **Application ID**: `com.BryceStandley.ftg.maui`
- **Version**: 1.2.1
- **Assembly Name**: `mts` (Manifest To Scale)

## Settings Management

The application automatically saves user preferences:

### Persistent Settings
- **Output Folder Path**: Last used output directory
- **Input Folder Path**: Last input file location
- **Recent Files**: History of processed files (up to 10)
- **Window Size**: Application window dimensions
- **Processing Settings**: User preferences for processing options

### Settings Storage
Settings are stored using MAUI Preferences API and persist between application sessions.

## Processing Output

### Folder Structure
```
OutputFolder/
└── {JobGUID}/
    ├── scale_interface_files/
    │   ├── {Company}_Receipt-{Date}.rcxml
    │   └── {Company}_Shipments-{Date}.shxml
    ├── simplified_{filename}.pdf (PDF input only)
    ├── {filename}.xlsx (PDF input only)
    └── {filename}.csv (PDF input only)
```

### File Naming Conventions
- **Receipt XML**: `{CompanyCode}_Receipt-{dd-MM-yyyy}.rcxml`
- **Shipment XML**: `{CompanyCode}_Shipments-{dd-MM-yyyy}.shxml`
- **Job Folder**: Unique GUID for each processing job

## Error Handling

### Validation Checks
- File existence validation
- File format verification
- Output directory creation
- Processing error reporting

### User Feedback
- Input validation with helpful error messages
- Processing status updates
- Detailed error information in output log
- Success confirmation dialogs

## Development

### Project Structure
```
FTG.MAUI/
├── FileProcessor.xaml/.cs    # Main processing interface
├── AppShell.xaml/.cs        # Application shell
├── SettingsManager.cs       # Settings persistence
├── MauiProgram.cs          # Application configuration
└── Platforms/              # Platform-specific code
    └── Windows/            # Windows platform files
```

### Key Components
- **FileProcessor**: Main UI and processing logic
- **SettingsManager**: Handles all user preference storage
- **Global Logging**: Integration with FTG.Core logging system

## Building and Running

### Prerequisites
- .NET 9 SDK
- Windows 10/11 for Windows builds
- MAUI workload installed

### Build Commands
```bash
# Debug build
dotnet build

# Release build
dotnet build -c Release

# Publish single-file executable
dotnet publish -c Release -r win-x64 --self-contained
```

### Running
```bash
# Development
dotnet run

# Standalone executable
./bin/Release/net9.0-windows10.0.19041.0/win-x64/publish/mts.exe
```

## Troubleshooting

### Common Issues
- **File Access Errors**: Ensure output directory is writable
- **Processing Failures**: Check input file format and integrity
- **Missing Dependencies**: Verify .NET 9 runtime (if not self-contained)

### Logging
The application uses the FTG.Core global logging system for detailed error tracking and debugging information.