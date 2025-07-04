using FTG.Core.Manifest;

namespace FTG.API.Processing;

public class Results
{
    public class XmlExportResults
    {
        public string ReceiptXml { get; set; } = string.Empty;
        public string ShipmentXml { get; set; } = string.Empty;
        public DateOnly ManifestDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
        
        public FreshToGoManifest Manifest { get; set; } = new FreshToGoManifest();
        public ValidationResult ValidationResult { get; set; }
    }
    
    public struct ValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; }
    }
}