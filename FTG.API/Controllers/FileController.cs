namespace FTG.API.Controllers;

using Config;
using Core.Files;
using Core.Logging;
using Auth;
using Processing;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/files")]
public class FileController : ControllerBase
{
    private readonly IConfiguration _aspConfiguration;
    private readonly IWebHostEnvironment _environment;
    private readonly IAuth _auth;
    private readonly IConfig _config;
    public FileController(IConfiguration aspConfiguration, IWebHostEnvironment environment, IAuth auth, IConfig config)
    {
        _aspConfiguration = aspConfiguration;
        _environment = environment;
        _auth = auth;
        _config = config;
        GlobalLogger.OnMessageLogged += OnMessageLogged;
    }

    private void OnMessageLogged(string message)
    {
        //Console.WriteLine(message);
    }
    
    [HttpPost("ftg/upload")]
    public async Task<IActionResult> UploadFile(IFormFile? file)
    {
        try
        {
            if (!_auth.IsAuthenticated(Request) && !_environment.IsDevelopment())
            {
                GlobalLogger.LogWarning("Invalid authentication key");
                return Unauthorized("Invalid authentication key");
            }
            
            if (file == null || file.Length == 0)
            {
                GlobalLogger.LogWarning("No file uploaded");
                return BadRequest("No file uploaded");
            }

            // Create an uploads directory if it doesn't exist
            var uploadsPath = Path.Combine(_environment.ContentRootPath, "uploads");
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
            var xmlResults = Process.ExportXmlFiles(_config, uploadsPath, fileName);
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
            
            var shouldCleanup = _aspConfiguration.GetValue<bool>("FileCleanup:Enabled");
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
            
            var daysToKeep = _aspConfiguration.GetValue<int>("FileCleanup:DaysToKeep");
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
            if (!_auth.IsAuthenticated(Request) && !_environment.IsDevelopment())
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
            var filePath = Path.Combine(_config.GetUploadsPath(), fileName);

            // Save file to disk
            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
                GlobalLogger.LogInfo($"File saved: {filePath}");
            }
            
            GlobalLogger.LogInfo($"Supplier: {file.FileName}");
            
            // Process the file
            var xmlResults = await Process.ExportCafFiles(_config, _config.GetUploadsPath(), fileName);
            
            GlobalLogger.LogInfo($"Manifest read successfully");
            GlobalLogger.LogInfo($"   Manifest Date: {xmlResults.Manifest.GetManifestDate()}");
            GlobalLogger.LogInfo($"   Total Orders: {xmlResults.Manifest.GetTotalOrders()}");
            GlobalLogger.LogInfo($"   Total Crates: {xmlResults.Manifest.GetTotalCrates()}");
            
            
            
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
            var shouldCleanup = _aspConfiguration.GetValue<bool>("FileCleanup:Enabled");
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
            var daysToKeep = _aspConfiguration.GetValue<int>("FileCleanup:DaysToKeep");
            FileCleanup.CleanupFiles(_config.GetUploadsPath(), daysToKeep);
            FileCleanup.CleanupFiles(_config.GetOutputPath(), daysToKeep);
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
