using FTG_PDF.Core.Logging;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;
using FTG_PDF.Api.Files;
using FTG_PDF.Core.FreshToGo;
using FTG_PDF.Core.Pdf;

namespace FTG_PDF.Api.Controllers;

[ApiController]
[Route("api/ftg/files")]
public class FileController(
    IConfiguration configuration,
    IWebHostEnvironment environment)
    : ControllerBase
{
    private readonly string _fileUploadPath = Path.Combine(environment.ContentRootPath, "uploads");
    private readonly string _fileStoragePath = Path.Combine(environment.ContentRootPath, "output");

    
    [HttpPost("upload")]
    public async Task<IActionResult> UploadFile(IFormFile? file)
    {
        try
        {
            if (!environment.IsDevelopment() && !IsAuthenticated())
            {
                GlobalLogger.LogWarning("Unauthorized access attempt");
                return Unauthorized(new {
                    message = "Invalid authentication key",
                    exception = "Unauthorized access"
                });
            }
            
            if (file == null || file.Length == 0)
            {
                GlobalLogger.LogError("No file uploaded");
                return BadRequest(new {
                    message = "No file uploaded",
                    exception = "File is null or empty"
                });
            }
            
            GlobalLogger.LogInfo($"File received: {file.FileName}, Size: {file.Length} bytes");
            if(file.FileName.EndsWith(".pdf") || file.FileName.EndsWith(".PDF"))
            {
                GlobalLogger.LogInfo("File is a PDF, proceeding with processing.");
            }
            else
            {
                GlobalLogger.LogError($"Unsupported file type: {file.FileName}");
                return BadRequest(new {
                    message = "Unsupported file type. Only PDF files are allowed.",
                    exception = "Invalid file type"
                });
            }
            
            if (!Directory.Exists(_fileUploadPath))
            {
                GlobalLogger.LogInfo($"Uploads directory does not exist, creating: {_fileUploadPath}");
                Directory.CreateDirectory(_fileUploadPath);
            }
            else
            {
                GlobalLogger.LogInfo($"Uploads directory already exists: {_fileUploadPath}");
            }

            var fileGuid = Guid.NewGuid();
            var fileName = $"{fileGuid}.pdf";
            var filePath = Path.Combine(_fileUploadPath, fileName);

            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            GlobalLogger.LogInfo($"File saved: {filePath}");


            var xmlResults = await ExportXmlFiles(_fileUploadPath, fileName);
            if (xmlResults == null || !xmlResults.Success)
            {
                GlobalLogger.LogError($"Failed to export XML files: {xmlResults?.Message}");
                return BadRequest(new
                {
                    message = "Failed to process file",
                    exception = xmlResults?.Message ?? "Unknown error"
                });
            }
            string receiptXml;
            string shipmentXml;
            
            using (var reader = new StreamReader(xmlResults.ReceiptXml))
            {
                receiptXml = reader.ReadToEnd();
            }
            
            using (var reader = new StreamReader(xmlResults.ShipmentXml))
            {
                shipmentXml = reader.ReadToEnd();
            }
            
            var shouldCleanup = configuration.GetValue<bool>("FileCleanup:Enabled");
            if (!shouldCleanup)
                return Ok(new
                {
                    message = "File processed successfully",
                    guid = fileGuid.ToString(),
                    manifestDate = xmlResults.ManifestDate.ToString("YYYY-MM-DD"),
                    receiptXmlContent = receiptXml,
                    shipmentXmlContent = shipmentXml
                });
            
            var daysToKeep = configuration.GetValue<int>("FileCleanup:DaysToKeep");
            FileCleanup.CleanupFiles(_fileUploadPath, daysToKeep);
            FileCleanup.CleanupFiles(Path.Join(_fileUploadPath, "../", "output"), daysToKeep);
            GlobalLogger.LogInfo($"Cleanup completed. Files older than {daysToKeep} days removed.");

            return Ok(new
            {
                message = "File processed successfully",
                guid = fileGuid.ToString(),
                manifestDate = xmlResults.ManifestDate.ToString("YYYY-MM-DD"),
                receiptXmlContent = receiptXml,
                shipmentXmlContent = shipmentXml
            });
        }
        catch (Exception ex)
        {
            GlobalLogger.LogError("Error uploading file", ex);
            return StatusCode(500, new {
                message = "Error uploading file",
                exception = ex
            });
        }
    }
    
    private class XmlExportResults
    {
        public bool Success { get; set; } = false;
        public string Message { get; set; } = string.Empty;
        public string ReceiptXml { get; set; } = string.Empty;
        public string ShipmentXml { get; set; } = string.Empty;
        public DateOnly ManifestDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
        
        public XmlExportResults() { }
        public XmlExportResults(bool success = false, string message = "")
        {
            Success = success;
            Message = message;
        }
    }
    
    private async Task<XmlExportResults?> ExportXmlFiles(string filePath, string fileName)
    {
        string basePath = filePath;
        string inputFile = Path.Combine(basePath, fileName);
        string outputPath = Path.Join(basePath, "../", "output");
        string outputFile = Path.Join(basePath,"../", "output", Path.GetFileNameWithoutExtension(fileName));
        
        if (!Directory.Exists(outputPath))
        {
            Directory.CreateDirectory(outputPath);
            GlobalLogger.LogInfo($"Output directory created: {outputPath}");
        }
        
        string simplifiedPdfPath = outputFile + "_simplified.pdf";
        if(PdfProcessor.SimplifyPdf(inputFile, simplifiedPdfPath))
        {
            GlobalLogger.LogInfo($"PDF simplified successfully: {simplifiedPdfPath}");
        }
        else
        {
            GlobalLogger.LogError("Failed to simplify PDF");
            return new XmlExportResults(false, "Failed to simplify PDF");
        }

        var manifest = PdfProcessor.ConvertPdfToExcel(simplifiedPdfPath, outputFile + ".xlsx", environment.IsDevelopment());

        if (manifest == null) return new XmlExportResults(false, "Failed to convert manifest PDF into Excel");
        
        if(ManifestToScale.ConvertManifestToCsv(manifest, outputFile + ".csv"))
        {
            GlobalLogger.LogInfo($"Manifest CSV generated successfully: {outputFile}.csv");
        }
        else
        {
            GlobalLogger.LogError("Failed to convert manifest to CSV");
            return new XmlExportResults(false, "Failed to convert manifest to CSV");
        }

        var receiptXmlPath = outputFile + ".rcxml";
        if(ManifestToScale.GenerateReceiptFromTemplate(manifest, receiptXmlPath))
        {
            GlobalLogger.LogInfo($"Receipt XML generated successfully: {receiptXmlPath}");
        }
        else
        {
            GlobalLogger.LogError("Failed to generate receipt XML");
            return new XmlExportResults(false, "Failed to generate receipt XML");
        }
        

        var shipmentXmlPath = outputFile + ".shxml";
        if(ManifestToScale.GenerateShipmentFromTemplate(manifest, shipmentXmlPath))
        {
            GlobalLogger.LogInfo($"Shipment XML generated successfully: {shipmentXmlPath}");
        }
        else
        {
            GlobalLogger.LogError("Failed to generate shipment XML");
            return new XmlExportResults(false, "Failed to generate shipment XML");
        }

        var reordered = manifest.GetManifestDate().ToString("yyyy-dd-MM");
        DateOnly result = DateOnly.ParseExact(reordered, "yyyy-dd-MM");

        return new XmlExportResults
        {
            ReceiptXml = receiptXmlPath,
            ShipmentXml = shipmentXmlPath,
            ManifestDate = result
        };
    }
    
    
    [HttpGet("{filename}")]
    public async Task<IActionResult> GetFile(string filename)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(filename) || 
                filename.Contains("..") || 
                Path.GetInvalidFileNameChars().Any(filename.Contains))
            {
                return BadRequest(new {
                    message = "Invalid filename",
                    exception = "Filename contains invalid characters or is empty"
                });
            }

            var filePath = Path.Combine(_fileStoragePath, filename);
            
            if (!System.IO.File.Exists(filePath))
            {
                GlobalLogger.LogError($"File not found: {filePath}");
                return NotFound(new {
                    message = "File not found",
                    exception = "The requested file does not exist"
                });
            }
            
            var provider = new FileExtensionContentTypeProvider();
            if (!provider.TryGetContentType(filePath, out var contentType))
            {
                contentType = "application/octet-stream";
            }
            
            var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
            GlobalLogger.LogInfo($"File retrieved successfully: {filePath}, Size: {fileBytes.Length} bytes");
            return File(fileBytes, contentType, filename);
        }
        catch (UnauthorizedAccessException)
        {
            GlobalLogger.LogWarning($"Unauthorized access attempt for file: {filename}");
            return StatusCode(403, new {
                message = "Access denied",
                exception = "You do not have permission to access this file"
            });
        }
        catch (Exception ex)
        {
            GlobalLogger.LogError($"Error retrieving file: {filename}");
            return StatusCode(500, new {
                message = "Error retrieving file",
                exception = ex.Message
            });
        }
    }

    [HttpGet("download/{filename}")]
    public async Task<IActionResult> DownloadFile(string filename)
    {
        try
        {
            // Validate filename
            if (string.IsNullOrWhiteSpace(filename) || 
                filename.Contains("..") || 
                Path.GetInvalidFileNameChars().Any(filename.Contains))
            {
                GlobalLogger.LogError("Invalid filename");
                return BadRequest( new {
                    message = "Invalid filename",
                    exception = "Filename contains invalid characters or is empty"
                });
            }

            var filePath = Path.Combine(_fileStoragePath, filename);

            if (!System.IO.File.Exists(filePath))
            {
                GlobalLogger.LogError($"File not found: {filePath}");
                return NotFound(new {
                    message = "File not found",
                    exception = "The requested file does not exist"
                });
            }

            var provider = new FileExtensionContentTypeProvider();
            if (!provider.TryGetContentType(filePath, out var contentType))
            {
                contentType = "application/octet-stream";
            }

            var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
            
            Response.Headers.Append("Content-Disposition", $"attachment; filename=\"{filename}\"");
            
            GlobalLogger.LogInfo($"File downloaded successfully: {filePath}, Size: {fileBytes.Length} bytes");
            return File(fileBytes, contentType, filename);
        }
        catch (Exception ex)
        {
            GlobalLogger.LogError($"Error downloading file: {filename}", ex);
            return StatusCode(500, new
            {
                message = "Error downloading file",
                exception = ex.Message
            });
        }
    }

    [HttpGet("stream/{filename}")]
    public IActionResult StreamFile(string filename)
    {
        try
        {
            // Validate filename
            if (string.IsNullOrWhiteSpace(filename) || 
                filename.Contains("..") || 
                Path.GetInvalidFileNameChars().Any(filename.Contains))
            {
                GlobalLogger.LogError("Invalid filename");
                return BadRequest(new {
                    message = "Invalid filename",
                    exception = "Filename contains invalid characters or is empty"
                });
            }

            var filePath = Path.Combine(_fileStoragePath, filename);

            if (!System.IO.File.Exists(filePath))
            {
                GlobalLogger.LogError($"File not found: {filePath}");
                return NotFound($"File '{filename}' not found");
            }

            var provider = new FileExtensionContentTypeProvider();
            if (!provider.TryGetContentType(filePath, out var contentType))
            {
                contentType = "application/octet-stream";
            }

            // Stream file for better memory efficiency with large files
            var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            
            GlobalLogger.LogInfo("File streaming initiated");
            return File(fileStream, contentType, filename, enableRangeProcessing: true);
        }
        catch (Exception ex)
        {
            GlobalLogger.LogError($"Error streaming file: '{filename}'", ex);
            return StatusCode(500, new {
                message = "Error streaming file",
                exception = ex.Message
            });
        }
    }
    

    private bool IsAuthenticated()
    {
        var expectedKey = configuration["Authentication:SharedKey"];

        if (string.IsNullOrEmpty(expectedKey))
        {
            GlobalLogger.LogWarning("Shared key not configured");
            return false;
        }
        
        if (!Request.Headers.ContainsKey("Authorization"))
        {
            GlobalLogger.LogWarning("Authorization header not found");
            return false;
        }

        var authHeader = Request.Headers["Authorization"].FirstOrDefault();
        
        var providedKey = authHeader?.StartsWith("Bearer ") == true
            ? authHeader.Substring(7)
            : authHeader;
        
        if (string.IsNullOrEmpty(providedKey))
        {
            GlobalLogger.LogWarning("Provided authentication key is empty");
            return false;
        }
        
        return SecureStringCompare(expectedKey, providedKey);
    }

    private bool SecureStringCompare(string? expected, string? provided)
    {
        if (expected == null || provided == null)
            return false;

        if (expected.Length != provided.Length)
            return false;

        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        var providedBytes = Encoding.UTF8.GetBytes(provided);

        return CryptographicOperations.FixedTimeEquals(expectedBytes, providedBytes);
    }
}
