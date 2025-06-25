namespace FTG_PDF_API;

using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;

[ApiController]
[Route("api/ftg/files")]
public class FileController(
    IConfiguration configuration,
    IWebHostEnvironment environment,
    ILogger<FileController> logger)
    : ControllerBase
{
    [HttpPost("upload")]
    public async Task<IActionResult> UploadFile(IFormFile file)
    {
        try
        {
            //logger.LogInformation($"Test: {configuration["Authentication:SharedKey"]}");
            if (!IsAuthenticated())
            {
                return Unauthorized("Invalid authentication key");
            }
            
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded");
            }

            // Create uploads directory if it doesn't exist
            var uploadsPath = Path.Combine(environment.ContentRootPath, "uploads");
            if (!Directory.Exists(uploadsPath))
            {
                Directory.CreateDirectory(uploadsPath);
            }

            // Generate unique filename to avoid conflicts
            var fileName = $"{Guid.NewGuid()}_{file.FileName}";
            var filePath = Path.Combine(uploadsPath, fileName);

            // Save file to disk
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            logger.LogInformation($"File saved: {filePath}");

            // Process the file and export XML
            var xmlResults = ExportXmlFiles(uploadsPath, fileName);
            string receiptXmlContent = string.Empty;
            string shipmentXmlContent = string.Empty;
            
            using (var reader = new StreamReader(xmlResults.ReceiptXml))
            {
                receiptXmlContent = reader.ReadToEnd();
            }
            
            using (var reader = new StreamReader(xmlResults.ShipmentXml))
            {
                shipmentXmlContent = reader.ReadToEnd();
            }
            
            var shouldCleanup = configuration.GetValue<bool>("FileCleanup:Enabled");
            if (shouldCleanup)
            {
                var daysToKeep = configuration.GetValue<int>("FileCleanup:DaysToKeep");
                FileCleanup.CleanupFiles(uploadsPath, daysToKeep);
                FileCleanup.CleanupFiles(Path.Join(uploadsPath, "../", "output"), daysToKeep);
                logger.LogInformation($"Cleanup completed. Files older than {daysToKeep} days removed.");
            }
            
            return Ok(new
            {
                message = xmlResults.ValidationResult.IsValid ? "success" : "error",
                error = xmlResults.ValidationResult.ErrorMessage,
                manifestDate = xmlResults.ManifestDate,
                totalOrders = xmlResults.Manifest.GetTotalOrders(),
                totalCrates = xmlResults.Manifest.GetTotalCrates(),
                receiptXmlContent = xmlResults.ValidationResult.IsValid ? receiptXmlContent : string.Empty,
                shipmentXmlContent = xmlResults.ValidationResult.IsValid ? shipmentXmlContent : string.Empty,
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error uploading file");
            return StatusCode(500, "Internal server error");
        }
    }
    
    private class XmlExportResults
    {
        public string ReceiptXml { get; set; } = string.Empty;
        public string ShipmentXml { get; set; } = string.Empty;
        public DateOnly ManifestDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
        
        public FreshToGoManifest Manifest { get; set; } = new FreshToGoManifest();
        public ValidationResult ValidationResult { get; set; }
    }
    
    private XmlExportResults ExportXmlFiles(string filePath, string fileName)
    {
        string basePath = filePath;
        string inputFile = Path.Combine(basePath, fileName);
        string outputPath = Path.Join(basePath, "../", "output");
        string outputFile = Path.Join(basePath,"../", "output", Path.GetFileNameWithoutExtension(fileName));
        
        if (!Directory.Exists(outputPath))
        {
            Directory.CreateDirectory(outputPath);
        }
        
        string simplifiedPdfPath = outputFile + "_simplified.pdf";
        PdfProcessor.SimplifyPdf(inputFile, simplifiedPdfPath);

        var manifest = PdfProcessor.ConvertPdfToExcel(simplifiedPdfPath, outputFile + ".xlsx");

        if (manifest == null) return new XmlExportResults();
        
        ManifestToScale.ConvertManifestToCsv(manifest, outputFile + ".csv");

        var receiptXmlPath = outputFile + ".rcxml";
        ManifestToScale.GenerateReceiptFromTemplate(manifest, receiptXmlPath);

        var shipmentXmlPath = outputFile + ".shxml";
        ManifestToScale.GenerateShipmentFromTemplate(manifest, shipmentXmlPath);

        var reordered = manifest.GetManifestDate().ToString("yyyy-dd-MM");
        DateOnly result = DateOnly.ParseExact(reordered, "yyyy-dd-MM");

        var xml = new XmlExportResults();
        xml.ReceiptXml = receiptXmlPath;
        xml.ShipmentXml = shipmentXmlPath;
        xml.ManifestDate = result;
        xml.ValidationResult = ValidateManifest(manifest);
        xml.Manifest = manifest;

        return xml;
    }
    
    private struct ValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; }
    }

    private ValidationResult ValidateManifest(FreshToGoManifest manifest)
    {
        if (manifest.GetTotalCrates() == 0)
        {
            logger.LogWarning("Manifest is empty or null");
            return new ValidationResult()
            {
                IsValid = false,
                ErrorMessage = "Manifest is empty or null"
            };
        }

        // Check for duplicate orders
        var orderNumbers = new HashSet<string>();
        foreach (var order in manifest.GetOrders())
        {
            if (!orderNumbers.Add(order.OrderNumber))
            {
                logger.LogWarning($"Duplicate order found: {order.OrderNumber}");
                return new ValidationResult()
                {
                    IsValid = false,
                    ErrorMessage = $"Duplicate order found: {order.OrderNumber}"
                };
            }
        }

        // Check for total orders and crates
        if (manifest.GetTotalOrders() <= 0 || manifest.GetTotalCrates() < 0)
        {
            logger.LogWarning("Invalid total orders or crates in manifest");
            return new ValidationResult()
            {
                IsValid = false,
                ErrorMessage = "Invalid total orders or crates in manifest"
            };
        }

        return new ValidationResult()
        {
            IsValid = true,
            ErrorMessage = string.Empty
        };
    }


    private bool IsAuthenticated()
    {
        // Get the shared key from configuration
        var expectedKey = configuration["Authentication:SharedKey"];

        if (string.IsNullOrEmpty(expectedKey))
        {
            logger.LogWarning("Shared key not configured");
            return false;
        }

        // Check for Authorization header
        if (!Request.Headers.ContainsKey("Authorization"))
        {
            return false;
        }

        var authHeader = Request.Headers["Authorization"].FirstOrDefault();

        // Handle both "Bearer token" and direct token formats
        var providedKey = authHeader?.StartsWith("Bearer ") == true
            ? authHeader.Substring(7)
            : authHeader;

        // Use secure string comparison to prevent timing attacks
        return SecureStringCompare(expectedKey, providedKey);
    }

    private bool SecureStringCompare(string expected, string provided)
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
