namespace FTG.API.Controllers;

using Config;
using Core.Files;
using Core.Logging;
using Auth;
using Processing;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/files")]
public class FileController(
    IConfiguration aspConfiguration,
    IWebHostEnvironment environment,
    IAuth auth,
    IConfig config)
    : ControllerBase
{
    [HttpPost("ftg/upload")]
    public async Task<IActionResult> UploadFile(IFormFile? file)
    {
        try
        {
            if (!auth.IsAuthenticated(Request) && !environment.IsDevelopment())
            {
                return Unauthorized("Invalid authentication key");
            }
            
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded");
            }

            // Create an uploads directory if it doesn't exist
            var uploadsPath = Path.Combine(environment.ContentRootPath, "uploads");
            if (!Directory.Exists(uploadsPath))
            {
                Directory.CreateDirectory(uploadsPath);
            }

            // Generate unique filename to avoid conflicts
            var fileName = $"{Guid.NewGuid()}_{file.FileName}";
            var filePath = Path.Combine(uploadsPath, fileName);

            // Save file to disk
            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            GlobalLogger.LogInfo($"File saved: {filePath}");

            // Process the file and export XML
            var xmlResults = Process.ExportXmlFiles(config, uploadsPath, fileName);
            // ReSharper disable once RedundantAssignment
            var receiptXmlContent = string.Empty;
            // ReSharper disable once RedundantAssignment
            var shipmentXmlContent = string.Empty;
            
            using (var reader = new StreamReader(xmlResults.ReceiptXml))
            {
                receiptXmlContent = await reader.ReadToEndAsync();
            }
            
            using (var reader = new StreamReader(xmlResults.ShipmentXml))
            {
                shipmentXmlContent = await reader.ReadToEndAsync();
            }
            
            var shouldCleanup = aspConfiguration.GetValue<bool>("FileCleanup:Enabled");
            if (!shouldCleanup)
                return Ok(new
                {
                    message = xmlResults.ValidationResult.IsValid ? "success" : "error",
                    error = xmlResults.ValidationResult.ErrorMessage,
                    manifestDate = xmlResults.ManifestDate,
                    totalOrders = xmlResults.Manifest.GetTotalOrders(),
                    totalCrates = xmlResults.Manifest.GetTotalCrates(),
                    manifest = xmlResults.Manifest,
                    receiptXmlContent = xmlResults.ValidationResult.IsValid ? receiptXmlContent : string.Empty,
                    shipmentXmlContent = xmlResults.ValidationResult.IsValid ? shipmentXmlContent : string.Empty
                });
            
            var daysToKeep = aspConfiguration.GetValue<int>("FileCleanup:DaysToKeep");
            FileCleanup.CleanupFiles(uploadsPath, daysToKeep);
            FileCleanup.CleanupFiles(Path.Join(uploadsPath, "../", "output"), daysToKeep);
            GlobalLogger.LogInfo($"Cleanup completed. Files older than {daysToKeep} days removed.");
            return Ok(new
            {
                message = xmlResults.ValidationResult.IsValid ? "success" : "error",
                error = xmlResults.ValidationResult.ErrorMessage,
                manifestDate = xmlResults.ManifestDate,
                totalOrders = xmlResults.Manifest.GetTotalOrders(),
                totalCrates = xmlResults.Manifest.GetTotalCrates(),
                manifest = xmlResults.Manifest,
                receiptXmlContent = xmlResults.ValidationResult.IsValid ? receiptXmlContent : string.Empty,
                shipmentXmlContent = xmlResults.ValidationResult.IsValid ? shipmentXmlContent : string.Empty
            });
        }
        catch (Exception ex)
        {
            GlobalLogger.LogError("Error uploading file",ex);
            return StatusCode(500, "Internal server error");
        }
    }


    // New Endpoint for uploading Coles Azura Fresh CSV files
    [HttpPost("caf/upload")]
    public async Task<IActionResult> UploadCsv(IFormFile? file)
    {
        try
        {
            if (!auth.IsAuthenticated(Request) && !environment.IsDevelopment())
            {
                GlobalLogger.LogWarning("Invalid authentication key");
                return Unauthorized("Invalid authentication key");
            }
            
            if (file == null || file.Length == 0)
            {
                GlobalLogger.LogWarning("No file uploaded");
                return BadRequest("No file uploaded");
            }

            // Generate unique filename to avoid conflicts
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(config.GetUploadsPath(), fileName);

            // Save file to disk
            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
                GlobalLogger.LogInfo($"File saved: {filePath}");
            }
            
            // Process the file
            var xmlResults = Process.ExportCafFiles(config, config.GetUploadsPath(), fileName);
            // ReSharper disable once RedundantAssignment
            var receiptXmlContent = string.Empty;
            // ReSharper disable once RedundantAssignment
            var shipmentXmlContent = string.Empty;
            
            using (var reader = new StreamReader(xmlResults.ReceiptXml))
            {
                receiptXmlContent = await reader.ReadToEndAsync();
            }
            
            using (var reader = new StreamReader(xmlResults.ShipmentXml))
            {
                shipmentXmlContent = await reader.ReadToEndAsync();
            }
            
            // Perform file cleanup if enabled
            var shouldCleanup = aspConfiguration.GetValue<bool>("FileCleanup:Enabled");
            if (!shouldCleanup)
                return Ok(new
                {
                    message = xmlResults.ValidationResult.IsValid ? "success" : "error",
                    error = xmlResults.ValidationResult.ErrorMessage,
                    manifestDate = xmlResults.ManifestDate,
                    totalOrders = xmlResults.Manifest.GetTotalOrders(),
                    totalCrates = xmlResults.Manifest.GetTotalCrates(),
                    company = xmlResults.Manifest.Company.Company,
                    manifest = xmlResults.Manifest,
                    receiptXmlContent = xmlResults.ValidationResult.IsValid ? receiptXmlContent : string.Empty,
                    shipmentXmlContent = xmlResults.ValidationResult.IsValid ? shipmentXmlContent : string.Empty
                });
            var daysToKeep = aspConfiguration.GetValue<int>("FileCleanup:DaysToKeep");
            FileCleanup.CleanupFiles(config.GetUploadsPath(), daysToKeep);
            FileCleanup.CleanupFiles(config.GetOutputPath(), daysToKeep);
            GlobalLogger.LogInfo($"Cleanup completed. Files older than {daysToKeep} days removed.");
            return Ok(new
            {
                message = xmlResults.ValidationResult.IsValid ? "success" : "error",
                error = xmlResults.ValidationResult.ErrorMessage,
                manifestDate = xmlResults.ManifestDate,
                totalOrders = xmlResults.Manifest.GetTotalOrders(),
                totalCrates = xmlResults.Manifest.GetTotalCrates(),
                company = xmlResults.Manifest.Company.Company,
                manifest = xmlResults.Manifest,
                receiptXmlContent = xmlResults.ValidationResult.IsValid ? receiptXmlContent : string.Empty,
                shipmentXmlContent = xmlResults.ValidationResult.IsValid ? shipmentXmlContent : string.Empty
            });
        }
        catch (Exception ex)
        {
            GlobalLogger.LogError("Error uploading file", ex);
            return StatusCode(500, "Internal server error");
        }
    }
  
    
}
