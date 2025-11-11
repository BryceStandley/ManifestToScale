using System.Globalization;
using System.Text;
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
    
    private static readonly Dictionary<string, byte[]> CsvCache = new();
    
    /// Converts a file containing order records into a FreshToGoManifest object.
    /// <param name="filePath">The path to the file containing the order records, either in CSV or XLSX format.</param>
    /// <returns>
    ///     A FreshToGoManifest object containing the processed orders and metadata, or null if the file is invalid or an
    ///     error occurs during processing.
    /// </returns>
    public static async Task<OrderManifest?> ConvertToManifest(string filePath, string company)
    {
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            GlobalLogger.LogError("CSV file not found: " + filePath);

        try
        {
            await EnsureCsvCachedAsync(filePath);
            
            var records = ReadRecordsFromCsv(filePath);
            
            //var records = filePath.EndsWith(".xlsx") ? ReadRecordsFromXlsx(filePath) : ReadRecordsFromCsv(filePath);
            if (records.Count == 0)
            {
                GlobalLogger.LogError("No records found in file.");
                return null;
            }

            var orders = CreateOrdersFromRecords(records);
            var manifest = new OrderManifest(orders)
            {
                Company = company == "856946" ? ScaleCompany.AzuraFresh : ScaleCompany.ThemeGroup, // 856946 is Azura Fresh, 222222 is Theme Group
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
    
    private static async Task EnsureCsvCachedAsync(string filePath)
    {
        if (CsvCache.ContainsKey(filePath))
            return;

        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        
        switch (extension)
        {
            case ".csv":
                CsvCache[filePath] = await File.ReadAllBytesAsync(filePath);
                break;
            case ".xlsx":
            {
                using var csvStream = await ConvertXlsxToCsvStreamAsync(filePath);
                CsvCache[filePath] = ((MemoryStream)csvStream)?.ToArray() ?? throw new InvalidOperationException();
                break;
            }
        }
    }
    
    private static CsvReader CreateCachedCsvReader(string filePath, CsvConfiguration? config = null)
    {
        var csvBytes = CsvCache[filePath];
        var stream = new MemoryStream(csvBytes);
        var reader = new StreamReader(stream);
        return new CsvReader(reader, config ?? new CsvConfiguration(CultureInfo.InvariantCulture));
    }

    /// Creates a list of FreshToGoOrder objects from a list of AzuraFreshCsvRecord objects.
    /// <param name="records">The list of AzuraFreshCsvRecord objects from which to generate the orders.</param>
    /// <returns>A list of FreshToGoOrder objects constructed from the provided records.</returns>
    private static List<StoreOrder> CreateOrdersFromRecords(List<AzuraFreshCsvRecord> records)
    {
        var orders = new List<StoreOrder>();
        foreach (var record in records)
        {
            var order = new StoreOrder(record.StoreId, record.CustomerName, record.PurchaseOrderNo, record.OrderNumber, record.PurchaseOrderNo != string.Empty
                    ? record.PurchaseOrderNo
                    : record.OrderNumber, // Assuming InvoiceNo is used as OrderNumber
                record.InvoiceNo != string.Empty
                    ? record.InvoiceNo
                    : record.PurchaseOrderNo, // Assuming InvoiceNo is used as InventoryNumber
                record.UnitQty, record.CrateQty) { OrderDate = record.Date };
            
            if((order.PoNumber == String.Empty && order.OrderNumber == String.Empty) || order.CrateQuantity == 0 || order.Quantity == 0)
            {
                GlobalLogger.LogWarning($"Order with no PO or Order number or has 0 crates found for StoreId: {record.StoreId}, CustomerName: {record.CustomerName}, Date: {record.Date}, Excluding from manifest.");
                continue;
            }
            orders.Add(order);
        }
        
        return orders;
    }

    private static async Task<Stream?> ConvertXlsxToCsvStreamAsync(string filePath)
    {
        try
        {
            ExcelPackage.License.SetNonCommercialPersonal("Bryce Standley");
            
            using var package = new ExcelPackage(new FileInfo(filePath));
            var worksheet = package.Workbook.Worksheets[0];
            if (worksheet == null)
            {
                GlobalLogger.LogError("Worksheet not found in the Excel file.");
                return null;
            }

            var csvContent = new StringBuilder();
            var rowCount = worksheet?.Dimension.Rows ?? 0;
            var colCount = worksheet?.Dimension.Columns ?? 0;

            for (int row = 1; row <= rowCount; row++)
            {
                var values = new List<string>();
                
                for (int col = 1; col <= colCount; col++)
                {
                    var cellValue = worksheet?.Cells[row, col].Value?.ToString() ?? "";
                    if (cellValue.Contains(',') || cellValue.Contains('"') || cellValue.Contains('\n'))
                    {
                        cellValue = $"\"{cellValue.Replace("\"", "\"\"")}\"";
                    }
                
                    values.Add(cellValue);
                }
                csvContent.AppendLine(string.Join(",", values));
            }
            
            var csvBytes = Encoding.UTF8.GetBytes(csvContent.ToString());
            return new MemoryStream(csvBytes);
        }
        catch (Exception e)
        {
            GlobalLogger.LogError($"Converting XLSX file to CSV stream failed with error: {e}");
            throw;
        }
    }
    
    private static List<AzuraFreshCsvRecord> ReadRecordsFromCsv(string filePath)
    {
        var expectedHeaders = new[] { "Ship Date", "Store Num", "Store Name", "PO #", "Cust #", "Order #", "Inv #", "Qty", "Crates" };
        int headerRowIndex = FindHeaderRow(filePath, expectedHeaders);
        GlobalLogger.LogInfo($"Header row data found at index {headerRowIndex}");
        bool shouldStopReading = false;
        
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            IgnoreBlankLines = true,
            ShouldSkipRecord = args => 
            {
                // Skip rows before header
                if (args.Row.Parser.Row <= headerRowIndex)
                    return true;
                
                if (args.Row.Parser.Record == null || args.Row.Parser.Record.All(string.IsNullOrWhiteSpace))
                    return true;
                
                // Check if this looks like a total/summary row
                if (!IsTotalRow(args.Row)) return shouldStopReading;
                
                GlobalLogger.LogInfo($"Totals row found at index {args.Row.Parser.Row}");
                shouldStopReading = true;
                return true; // Skip this row

            },
            PrepareHeaderForMatch = args => args.Header?.Trim() ?? string.Empty,
            GetDynamicPropertyName = args => (args.FieldIndex < 9 ? args.FieldIndex.ToString() : null) ?? string.Empty
        };
        
        using var csv = CreateCachedCsvReader(filePath, config);

        csv.Context.RegisterClassMap<AzuraFreshCsvRecordMap>();
        var records = csv.GetRecords<AzuraFreshCsvRecord>();
        return records.ToList();
    }
    
    private static bool IsTotalRow(IReaderRow row)
    {
        try
        {
            var fields = new List<string>();
            // Read the first 8 fields (0-7) and trim them - We know AzuraFresh CSV has 8 fields and shouldn't have more
            for (int i = 0; i < 9; i++)
            {
                fields.Add(row.GetField(i)?.Trim() ?? string.Empty);
            }
	        
            // Check if most fields are empty and last field contains a number (total)
            int emptyFieldCount = fields.Take(fields.Count - 1).Count(f => string.IsNullOrWhiteSpace(f));
            string lastField = fields.LastOrDefault() ?? string.Empty;
	        
            // Criteria for total row:
            // 1. Most fields (40%+) are empty
            // 2. Last field contains a numeric value
            // 3. First field doesnt contains a date
            // 4. No PO number in row
            // 5. No Order number in row
            bool hasDateInRow = DateOnly.TryParse(fields[0], out _);
            bool hasPoNumber = fields[3].Contains('V');
            bool hasOrderNumber = int.TryParse(fields[5], out _);
            bool mostFieldsEmpty = (double)emptyFieldCount / (fields.Count - 1) >= 0.4;
            bool lastFieldHasValue = !string.IsNullOrWhiteSpace(lastField);
            bool lastFieldIsNumeric = decimal.TryParse(lastField.Replace("$", "").Replace(",", ""), out _);
	        
            return mostFieldsEmpty && lastFieldHasValue && lastFieldIsNumeric && !hasDateInRow && !hasPoNumber && !hasOrderNumber;
        }
        catch
        {
            return false;
        }
    }
    
    private static int FindHeaderRow(string filePath, string[] expectedHeaders)
    {
        using var csv = CreateCachedCsvReader(filePath);
    
        int rowIndex = 0;
    
        while (csv.Read())
        {
            rowIndex++;
            if (ContainsExpectedHeaders(csv, expectedHeaders))
            {
                return rowIndex - 1; // Return 0-based index for skipping
            }
        }
    
        throw new InvalidOperationException("Header row not found in CSV file");
    }

    private static bool ContainsExpectedHeaders(CsvReader csv, string[] expectedHeaders)
    {
        try
        {
            var currentRowFields = new List<string>();
            for (int i = 0; i < csv.Parser.Count; i++)
            {
                currentRowFields.Add(csv.GetField(i)?.Trim() ?? string.Empty);
            }
            return expectedHeaders.All(header => 
                currentRowFields.Any(field => 
                    string.Equals(field, header, StringComparison.OrdinalIgnoreCase)));
        }
        catch
        {
            return false;
        }
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