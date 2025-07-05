using System.Globalization;
using CsvHelper;
using FTG.Core.Logging;
using OfficeOpenXml;

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
            if (filePath.EndsWith(".xlsx"))
            {
                var records = ReadRecords(filePath);
                if (records.Count == 0)
                {
                    GlobalLogger.LogError("No records found in the Excel file.");
                    return null;
                }
                var orders = CreateOrdersFromRecords(records);
                var manifest = new FreshToGoManifest(orders)
                {
                    Company = ScaleCompany.AzuraFresh,
                    ManifestDate = records[0].Date // Assuming the first record's date is the manifest date
                };
                GlobalLogger.LogInfo($"Excel file processed successfully: {filePath}");
                return manifest;
            }
            
            if (filePath.EndsWith(".csv"))
            {
                using (var reader = new StreamReader(filePath))
                using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    var records = csv.GetRecords<dynamic>();
                    GlobalLogger.LogInfo($"CSV file read successfully: {filePath}");
                }
            }
            
            
        }
        catch (Exception e)
        {
            GlobalLogger.LogError("Error reading CSV file", e);
            throw;
        }
        return new FreshToGoManifest();
    }

    private static List<FreshToGoOrder> CreateOrdersFromRecords(List<AzuraFreshCsvRecord> records)
    {
        var orders = new List<FreshToGoOrder>();
        foreach (var record in records)
        {
            var order = new FreshToGoOrder(
                record.StoreId,
                record.CustomerName,
                record.CustomerPoNo,
                record.InvoiceNo,
                record.CustomerPoNo, // Assuming InvoiceNo is used as OrderNumber
                record.InvoiceNo, // Assuming InvoiceNo is used as InventoryNumber
                record.UnitQty,
                record.CrateQty
            );
            order.OrderDate = record.Date;
            orders.Add(order);
        }
        return orders;
    }
    
    private static List<AzuraFreshCsvRecord> ReadRecords(string filePath)
    {
        var records = new List<AzuraFreshCsvRecord>();
        using (var package = new ExcelPackage(new FileInfo(filePath)))
        {
            var worksheet = package.Workbook.Worksheets[0];
            if (worksheet == null)
            {
                GlobalLogger.LogError("Worksheet not found in the Excel file.");
                return records;
            }
            int rowCount = worksheet.Dimension.Rows;
            for (int row = 3; row <= rowCount; row++)
            {
                var record = new AzuraFreshCsvRecord
                {
                    Date = DateOnly.FromDateTime(worksheet.Cells[row, 1].GetValue<DateTime>()),
                    InvoiceNo = worksheet.Cells[row, 2].GetValue<string>() ?? "",
                    CustomerPoNo = worksheet.Cells[row, 3].GetValue<string>() ?? "",
                    StoreId = worksheet.Cells[row, 4].GetValue<string>() ?? "",
                    CustomerName = worksheet.Cells[row, 5].GetValue<string>() ?? "",
                    UnitQty = worksheet.Cells[row, 6].GetValue<int>(),
                    CrateQty = worksheet.Cells[row, 7].GetValue<int>()
                };
                records.Add(record);
            }
            GlobalLogger.LogInfo($"Excel file read successfully: {filePath}");
            GlobalLogger.LogInfo($"Total records read: {records.Count}");
            return records;
        }
    }
}