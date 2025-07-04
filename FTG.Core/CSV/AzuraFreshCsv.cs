using System.Globalization;
using CsvHelper;
using FTG.Core.Logging;

namespace FTG.Core.CSV;

using Manifest;

public class AzuraFreshCsv
{
    public class AzuraFreshCsvRecord
    {
        public DateOnly Date { get; set; }
        public string InvoiceNo { get; set; } = "";
        public string CustomerPoNo { get; set; } = "";
        public string StoreId { get; set; } = "";
        public string CustomerName { get; set; } = "";
        public int UnitQty { get; set; } = 0;
        public int CrateQty { get; set; } = 0;
    }
    
    public static FreshToGoManifest? ConvertCsvToManifest(string filePath)
    {
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
        {
            GlobalLogger.LogError("CSV file not found: " + filePath);
        }

        try
        {
            using (var reader = new StreamReader(filePath))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                var records = csv.GetRecords<dynamic>();
                GlobalLogger.LogInfo($"CSV file read successfully: {filePath}");
            }
        }
        catch (Exception e)
        {
            GlobalLogger.LogError("Error reading CSV file", e);
            throw;
        }
        return new FreshToGoManifest();
    }
}