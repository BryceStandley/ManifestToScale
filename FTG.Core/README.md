# FTG.Core

A .NET 9 core library providing manifest processing, PDF handling, CSV parsing, and XML generation capabilities for Fresh To Go and Azura Fresh scale integration systems.

## Overview

FTG.Core is the foundational library that handles the core business logic for processing manifest files and generating scale interface XML files. It provides a comprehensive set of tools for working with PDF documents, CSV files, Excel spreadsheets, and XML generation.

## Features

- **PDF Processing**: Extract and process manifest data from PDF documents
- **CSV/Excel Handling**: Parse CSV and XLSX files with flexible date formatting
- **XML Generation**: Create receipt and shipment XML files for scale systems
- **Manifest Management**: Handle Fresh To Go and Azura Fresh manifest formats
- **File Operations**: Utilities for file cleanup and management
- **Logging**: Global logging system with caller information

## Key Components

### PDF Processing (`FTG.Core.PDF`)
- **PdfProcessor**: Simplifies multi-page PDFs and extracts text content
- **Text Extraction**: Converts PDF content to structured Excel format
- **Manifest Parsing**: Creates manifest objects from extracted PDF data

### CSV Processing (`FTG.Core.CSV`)
- **AzuraFreshCsv**: Processes Azura Fresh CSV and XLSX files
- **Flexible Date Parsing**: Handles multiple date formats automatically
- **Data Validation**: Ensures data integrity during conversion

### Manifest System (`FTG.Core.Manifest`)
- **FreshToGoManifest**: Core manifest data structure
- **FreshToGoOrder**: Individual order representation
- **ScaleCompany**: Company-specific configuration
- **ManifestToScale**: Converts manifests to scale XML formats

### XML Generation
- **Receipt XML**: Generates receipt files for scale systems
- **Shipment XML**: Creates shipment files with order details
- **Template-based**: Uses structured XML templates with namespaces

### Utilities
- **FileCleanup**: Automatic cleanup of old files
- **GlobalLogger**: Centralized logging with metadata
- **Error Handling**: Comprehensive error reporting

## Dependencies

- **CsvHelper**: CSV file parsing and processing
- **EPPlus**: Excel file manipulation
- **iText7**: PDF processing and text extraction
- **PDFsharp**: Additional PDF operations
- **Newtonsoft.Json**: JSON serialization
- **JetBrains.Annotations**: Code annotations

## Usage Examples

### Processing PDF Manifest
```csharp
// Simplify PDF structure
PdfProcessor.SimplifyPdf(inputPath, simplifiedPath);

// Convert to manifest
var manifest = PdfProcessor.ConvertPdfToExcel(simplifiedPath, outputPath);

// Generate XML files
ManifestToScale.GenerateReceiptFromTemplate(manifest, receiptPath);
ManifestToScale.GenerateShipmentFromTemplate(manifest, shipmentPath);
```

### Processing CSV/XLSX Manifest
```csharp
// Convert CSV/XLSX to manifest
var manifest = AzuraFreshCsv.ConvertToManifest(filePath);

// Export to CSV
ManifestToScale.ConvertManifestToCsv(manifest, outputPath);

// Generate XML files
ManifestToScale.GenerateReceiptFromTemplate(manifest, receiptPath);
ManifestToScale.GenerateShipmentFromTemplate(manifest, shipmentPath);
```

### File Cleanup
```csharp
// Clean files older than 7 days
FileCleanup.CleanupFiles(directoryPath, 7);
```

### Logging
```csharp
// Log information
GlobalLogger.LogInfo("Processing started");

// Log with extension method
this.LogHere("Custom message");

// Log errors
GlobalLogger.LogError("Error occurred", exception);
```

## Supported File Formats

### Input Formats
- **PDF**: Fresh To Go manifest documents
- **CSV**: Azura Fresh comma-separated data
- **XLSX**: Excel spreadsheets with manifest data

### Output Formats
- **XML**: Receipt and shipment files for scale systems
- **CSV**: Converted manifest data
- **XLSX**: Excel format manifest data

## Company Support

### Fresh To Go (FTG)
- Company Code: `PER-CO-FTG`
- Vendor Number: `853540`
- Processes PDF manifests

### Azura Fresh (CAF)
- Company Code: `PER-CO-CAF`
- Vendor Number: `954111`
- Processes CSV/XLSX files

## Data Validation

The library includes comprehensive validation:
- **Manifest Validation**: Checks for empty manifests and duplicate orders
- **Date Parsing**: Flexible date format recognition
- **Data Integrity**: Ensures consistent order and crate counts
- **Error Reporting**: Detailed error messages for troubleshooting

## XML Schema

Generated XML files follow the MANH WMS interface schema:
```
Namespace: http://www.manh.com/ILSNET/Interface
```

### Receipt XML Structure
- Receipt header with dates and identifiers
- Vendor information
- SKU details with quantities

### Shipment XML Structure
- Shipment header with order information
- Customer and carrier details
- Line item details with SKU information

## Error Handling

Robust error handling throughout:
- Try-catch blocks with detailed logging
- Graceful degradation on parsing failures
- Validation error reporting
- File operation error handling

## Performance Considerations

- Memory-efficient PDF processing
- Streaming for large file operations
- Optimized XML generation
- Configurable cleanup policies