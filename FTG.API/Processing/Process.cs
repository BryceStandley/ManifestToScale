using FTG.Core.CSV;

namespace FTG.API.Processing;

using Config;
using static Results;
using Core.Logging;
using Core.Manifest;

using Core.PDF;

public static class Process
{
    public static XmlExportResults ExportXmlFiles(IConfig config, string filePath, string fileName)
    {
        var inputFile = Path.Combine(filePath, fileName);
        var finalFile = Path.Join(config.GetFinishedPath(), Path.GetFileNameWithoutExtension(fileName));
        var outputFilePath = Path.Join(config.GetOutputPath(), Path.GetFileNameWithoutExtension(fileName));
        
        
        var simplifiedPdfPath = outputFilePath + "_simplified.pdf";
        PdfProcessor.SimplifyPdf(inputFile, simplifiedPdfPath);

        var manifest = PdfProcessor.ConvertPdfToExcel(simplifiedPdfPath, outputFilePath + ".xlsx");

        if (manifest == null) return new XmlExportResults();
        
        ManifestToScale.ConvertManifestToCsv(manifest, outputFilePath + ".csv");

        var receiptXmlPath = finalFile + ".rcxml";
        ManifestToScale.GenerateReceiptFromTemplate(manifest, receiptXmlPath);

        var shipmentXmlPath = finalFile + ".shxml";
        ManifestToScale.GenerateShipmentFromTemplate(manifest, shipmentXmlPath);

        var reordered = manifest.GetManifestDate().ToString("yyyy-dd-MM");
        var result = DateOnly.ParseExact(reordered, "yyyy-dd-MM");

        var xml = new XmlExportResults
        {
            ReceiptXml = receiptXmlPath,
            ShipmentXml = shipmentXmlPath,
            ManifestDate = result,
            ValidationResult = ValidateManifest(manifest),
            Manifest = manifest
        };

        return xml;
    }
    
    public static async Task<XmlExportResults> ExportCafFiles(IConfig config, string filePath, string fileName)
    {
        var inputFile = Path.Combine(filePath, fileName);
        var finalFile = Path.Join(config.GetFinishedPath(), Path.GetFileNameWithoutExtension(fileName));

        var manifest = await AzuraFreshCsv.ConvertToManifest(inputFile);

        if (manifest == null) return new XmlExportResults();

        var receiptXmlPath = finalFile + ".rcxml";
        ManifestToScale.GenerateReceiptFromTemplate(manifest, receiptXmlPath);

        var shipmentXmlPath = finalFile + ".shxml";
        ManifestToScale.GenerateShipmentFromTemplate(manifest, shipmentXmlPath);
        
        var reordered = manifest.GetManifestDate().ToString("yyyy-dd-MM");
        var result = DateOnly.ParseExact(reordered, "yyyy-dd-MM");

        var xml = new XmlExportResults
        {
            ReceiptXml = receiptXmlPath,
            ShipmentXml = shipmentXmlPath,
            ManifestDate = result,
            ValidationResult = ValidateManifest(manifest),
            Manifest = manifest
        };

        return xml;
    }
    
    private static ValidationResult ValidateManifest(FreshToGoManifest manifest)
    {
        if (manifest.GetTotalCrates() == 0)
        {
            GlobalLogger.LogWarning("Manifest is empty or null");
            return new ValidationResult
            {
                IsValid = false,
                ErrorMessage = "Manifest is empty or null"
            };
        }

        // Check for duplicate orders
        var orderNumbers = new HashSet<string>();
        foreach (var order in manifest.GetOrders().Where(order => !orderNumbers.Add(order.OrderNumber)))
        {
            GlobalLogger.LogWarning($"Duplicate order found: {order.OrderNumber}");
            return new ValidationResult
            {
                IsValid = false,
                ErrorMessage = $"Duplicate order found: {order.OrderNumber}"
            };
        }

        // Check for total orders and crates
        if (manifest.GetTotalOrders() > 0 && manifest.GetTotalCrates() >= 0)
            return new ValidationResult
            {
                IsValid = true,
                ErrorMessage = string.Empty
            };
        GlobalLogger.LogWarning("Invalid total orders or crates in manifest");
        return new ValidationResult
        {
            IsValid = false,
            ErrorMessage = "Invalid total orders or crates in manifest"
        };

    }
}