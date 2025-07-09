# FTG.API

A .NET 9 Web API for processing manifest files and generating XML exports for scale interface systems.

## Overview

FTG.API is a REST API service that processes manifest files (PDF, CSV, XLSX) and converts them into XML format files for scale interface integration. It supports two main workflows:
- Fresh To Go (FTG) manifest processing from PDF files
- Coles Azura Fresh (CAF) manifest processing from CSV/XLSX files

## Features

- **File Upload Processing**: Supports PDF, CSV, and XLSX file formats
- **Authentication**: Shared key-based authentication with secure string comparison
- **XML Generation**: Creates receipt and shipment XML files for scale systems
- **File Management**: Automatic cleanup of old files based on configurable retention policies
- **Validation**: Built-in manifest validation with error reporting
- **Logging**: Comprehensive logging throughout the processing pipeline

## API Endpoints

### Upload FTG Manifest
```
POST /api/files/ftg/upload
```
Uploads and processes Fresh To Go PDF manifest files.

### Upload CAF Manifest
```
POST /api/files/caf/upload
```
Uploads and processes Coles Azura Fresh CSV/XLSX manifest files.

## Configuration

### Authentication
```json
{
  "Authentication": {
    "SharedKey": "your-shared-key-here"
  }
}
```

### File Cleanup
```json
{
  "FileCleanup": {
    "Enabled": true,
    "DaysToKeep": 7
  }
}
```

### EPPlus License
```json
{
  "EPPlus": {
    "ExcelPackage": {
      "License": "NonCommercialPersonal:Your Name"
    }
  }
}
```

## Dependencies

- **EPPlus**: Excel file processing
- **Newtonsoft.Json**: JSON serialization
- **Microsoft.AspNetCore.OpenApi**: API documentation
- **Swashbuckle.AspNetCore**: Swagger UI

## Project Structure

```
FTG.API/
├── Auth/                    # Authentication services
├── Config/                  # Configuration management
├── Controllers/             # API controllers
├── Processing/              # File processing logic
├── appsettings.json         # Application configuration
└── Program.cs              # Application entry point
```

## Getting Started

1. **Prerequisites**
    - .NET 9 SDK
    - Valid EPPlus license configuration

2. **Configuration**
    - Update `appsettings.json` with your authentication key
    - Configure file cleanup settings as needed

3. **Run the Application**
   ```bash
   dotnet run
   ```

4. **Access the API**
    - API: `http://localhost:5090`
    - Swagger UI: `http://localhost:5090/swagger` (Development only)

## File Processing Workflow

1. **Upload**: Files are uploaded via multipart form data
2. **Validation**: File format and authentication are validated
3. **Processing**: Files are converted to manifest objects
4. **XML Generation**: Receipt and shipment XML files are created
5. **Response**: Processing results and XML content are returned
6. **Cleanup**: Old files are automatically cleaned up if enabled

## Security

- Shared key authentication using cryptographically secure string comparison
- Authentication bypass in development environment
- Configurable file retention policies
- Input validation and error handling

## Error Handling

The API provides comprehensive error handling with:
- Detailed error messages for validation failures
- HTTP status codes for different error types
- Logging of all errors for debugging

## Docker Support

The project includes Docker configuration via `compose.yaml` for containerized deployment.