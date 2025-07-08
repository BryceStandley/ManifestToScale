using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using CsvHelper.TypeConversion;
using FTG.Core.Logging;
using FTG.Core.Manifest;
using JetBrains.Annotations;
using OfficeOpenXml;
// ReSharper disable PropertyCanBeMadeInitOnly.Local

namespace FTG.Core.CSV;

public static class AzuraFreshCsv
{
    /// Converts a file containing order records into a FreshToGoManifest object.
    /// <param name="filePath">The path to the file containing the order records, either in CSV or XLSX format.</param>
    /// <returns>
    ///     A FreshToGoManifest object containing the processed orders and metadata, or null if the file is invalid or an
    ///     error occurs during processing.
    /// </returns>
    public static FreshToGoManifest? ConvertToManifest(string filePath)
    {
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            GlobalLogger.LogError("CSV file not found: " + filePath);

        try
        {
            var records = filePath.EndsWith(".xlsx") ? ReadRecordsFromXlsx(filePath) : ReadRecordsFromCsv(filePath);
            if (records.Count == 0)
            {
                GlobalLogger.LogError("No records found in file.");
                return null;
            }

            var orders = CreateOrdersFromRecords(records);
            var manifest = new FreshToGoManifest(orders)
            {
                Company = ScaleCompany.AzuraFresh,
                ManifestDate = records[0].Date // Assuming the first record's date is the manifest date
            };
            GlobalLogger.LogInfo($"File processed successfully: {filePath}");
            return manifest;
        }
        catch (Exception e)
        {
            GlobalLogger.LogError("Error reading file", e);
            return null;
        }
    }

    /// Creates a list of FreshToGoOrder objects from a list of AzuraFreshCsvRecord objects.
    /// <param name="records">The list of AzuraFreshCsvRecord objects from which to generate the orders.</param>
    /// <returns>A list of FreshToGoOrder objects constructed from the provided records.</returns>
    private static List<FreshToGoOrder> CreateOrdersFromRecords(List<AzuraFreshCsvRecord> records)
    {
        return records.Select(record => new FreshToGoOrder(record.StoreId, record.CustomerName, record.PurchaseOrderNo, record.OrderNumber, record.PurchaseOrderNo != string.Empty
                    ? record.PurchaseOrderNo
                    : record.OrderNumber, // Assuming InvoiceNo is used as OrderNumber
                record.InvoiceNo != string.Empty
                    ? record.InvoiceNo
                    : record.PurchaseOrderNo, // Assuming InvoiceNo is used as InventoryNumber
                record.UnitQty, record.CrateQty) { OrderDate = record.Date })
            .ToList();
    }

    /// Reads records from an Excel file and converts them into a list of AzuraFreshCsvRecord objects.
    /// <param name="filePath">The file path of the Excel file to read the records from.</param>
    /// <returns>A list of AzuraFreshCsvRecord objects containing the data extracted from the Excel file.</returns>
    private static List<AzuraFreshCsvRecord> ReadRecordsFromXlsx(string filePath)
    {
        var records = new List<AzuraFreshCsvRecord>();
        using var package = new ExcelPackage(new FileInfo(filePath));
        var worksheet = package.Workbook.Worksheets[0];
        if (worksheet == null)
        {
            GlobalLogger.LogError("Worksheet not found in the Excel file.");
            return records;
        }

        var rowCount = worksheet.Dimension.Rows;
        for (var row = 3; row <= rowCount; row++)
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

    private static List<AzuraFreshCsvRecord> ReadRecordsFromCsv(string filePath)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            ShouldSkipRecord = args => args.Row.Parser.Row <= 2 // Skip the first two rows
        };

        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, config);

        csv.Context.RegisterClassMap<AzuraFreshCsvRecordMap>();
        var records = csv.GetRecords<AzuraFreshCsvRecord>();
        return records.ToList();
    }

    private class FlexibleDateOnlyConverter : DefaultTypeConverter
    {
        private readonly string[] _dateFormats =
        [
            "d/MM/yyyy", // Single digit day: 8/07/2025
            "dd/MM/yyyy", // Double-digit day: 08/07/2025
            "d/M/yyyy", // Single digit day and month: 8/7/2025
            "dd/M/yyyy", // Double-digit day, single month: 08/7/2025
            "d/MM/yy", // Short year variants
            "dd/MM/yy",
            "d/M/yy",
            "dd/M/yy"
        ];

        public override object ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
        {
            if (string.IsNullOrWhiteSpace(text))
                return default(DateOnly);

            foreach (var format in _dateFormats)
                if (DateTime.TryParseExact(text, format, CultureInfo.InvariantCulture, DateTimeStyles.None,
                        out var dateTime))
                    return DateOnly.FromDateTime(dateTime);

            throw new TypeConverterException(this, memberMapData, text, row.Context,
                $"Unable to convert '{text}' to DateOnly using any of the supported formats");
        }
    }

    private class AzuraFreshCsvRecord
    {
        [Index(0)]
        [TypeConverter(typeof(FlexibleDateOnlyConverter))]
        [Format("dd/MM/yyyy")]
        public DateOnly Date { get; set; }

        [Index(1)] public string StoreId { get; set; } = "";

        [Index(2)] public string CustomerName { get; set; } = "";

        [Index(3)] public string PurchaseOrderNo { get; set; } = "";

        [Index(4)] public string CustomerPoNo { get; set; } = "";

        [Index(5)] public string OrderNumber { get; set; } = "";

        [Index(6)] public string InvoiceNo { get; set; } = "";

        [Index(7)] public int UnitQty { get; set; }

        [Index(8)] public int CrateQty { get; set; }
    }

    [UsedImplicitly]
    private sealed class AzuraFreshCsvRecordMap : ClassMap<AzuraFreshCsvRecord>
    {
        public AzuraFreshCsvRecordMap()
        {
            Map(m => m.Date).Index(0).TypeConverter<FlexibleDateOnlyConverter>();
            Map(m => m.StoreId).Index(1);
            Map(m => m.CustomerName).Index(2);
            Map(m => m.PurchaseOrderNo).Index(3);
            Map(m => m.CustomerPoNo).Index(4);
            Map(m => m.OrderNumber).Index(5);
            Map(m => m.InvoiceNo).Index(6);
            Map(m => m.UnitQty).Index(7);
            Map(m => m.CrateQty).Index(8);
        }
    }
}