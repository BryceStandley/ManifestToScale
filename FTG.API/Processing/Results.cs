using FTG.Core.Manifest;

namespace FTG.API.Processing;

public static class Results
{
    public class XmlExportResults
    {
        public string ReceiptXml { get; init; } = string.Empty;
        public string ShipmentXml { get; init; } = string.Empty;
        public DateOnly? ManifestDate { get; init; } = DateOnly.FromDateTime(DateTime.UtcNow);
        
        public FreshToGoManifest? Manifest { get; init; } = new();
        public ValidationResult ValidationResult { get; init; }
    }
    
    public struct ValidationResult
    {
        public bool IsValid { get; init; }
        public string ErrorMessage { get; init; }
        
        public FreshToGoManifest? Manifest { get; init; }
    }
}