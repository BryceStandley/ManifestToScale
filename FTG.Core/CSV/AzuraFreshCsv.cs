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
        var orders = new List<FreshToGoOrder>();
        foreach (var record in records)
        {
            var order = new FreshToGoOrder(record.StoreId, record.CustomerName, record.PurchaseOrderNo, record.OrderNumber, record.PurchaseOrderNo != string.Empty
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

    /// Reads records from an Excel file and converts them into a list of AzuraFreshCsvRecord objects.
    /// <param name="filePath">The file path of the Excel file to read the records from.</param>
    /// <returns>A list of AzuraFreshCsvRecord objects containing the data extracted from the Excel file.</returns>
    private static List<AzuraFreshCsvRecord> ReadRecordsFromXlsx(string filePath)
    {
        try
        {
            ExcelPackage.License.SetNonCommercialPersonal("Bryce Standley");
            
            var records = new List<AzuraFreshCsvRecord>();
            using var package = new ExcelPackage(new FileInfo(filePath));
            var worksheet = package.Workbook.Worksheets[0];
            if (worksheet == null)
            {
                GlobalLogger.LogError("Worksheet not found in the Excel file.");
                return records;
            }

            var expectedHeaders = new[] { "Ship Date", "Store Num", "Store Name", "PO #", "Cust #", "Order #", "Inv #", "Qty", "Crates" };
            var headersIndex = FindXlsxHeaderRow(expectedHeaders, worksheet);
            GlobalLogger.LogInfo($"Header row data found at index {headersIndex}");
            if (headersIndex == -1)
            {
                GlobalLogger.LogError("Header row not found in the Excel file.");
                return records;
            }
            
            var totalsRowIndex = FindXlsxTotalsRow(worksheet);
            if (totalsRowIndex != -1)
            {
                GlobalLogger.LogInfo($"Totals row found at index {totalsRowIndex}");
                // If totals row is found, we can stop reading further
                worksheet = worksheet.Cells[1, 1, totalsRowIndex - 1, worksheet.Dimension.Columns].Worksheet;
            }
            else
            {
                GlobalLogger.LogInfo("No totals row found, reading all records.");
            }
            
            var rowCount = worksheet.Dimension.Rows;
            for (var row = headersIndex + 1; row < rowCount; row++)
            {
                
                    var record = new AzuraFreshCsvRecord
                    {
                        Date = DateOnly.FromDateTime(worksheet.Cells[row, 1].GetValue<DateTime>()),
                        StoreId = worksheet.Cells[row, 2].GetValue<string>() ?? "",
                        CustomerName = worksheet.Cells[row, 3].GetValue<string>() ?? "",
                        PurchaseOrderNo = worksheet.Cells[row, 4].GetValue<string>() ?? "",
                        CustomerPoNo = worksheet.Cells[row, 5].GetValue<string>() ?? "",
                        OrderNumber = worksheet.Cells[row, 6].GetValue<string>() ?? "",
                        InvoiceNo = worksheet.Cells[row, 7].GetValue<string>() ?? "",
                        UnitQty = worksheet.Cells[row, 8].GetValue<int>(),
                        CrateQty = worksheet.Cells[row, 9].GetValue<int>()
                    };
                    records.Add(record);
                
                
                
            }

            GlobalLogger.LogInfo($"Excel file read successfully: {filePath}");
            GlobalLogger.LogInfo($"Total records read: {records.Count}");
            return records;
        }
        catch (Exception e)
        {
            GlobalLogger.LogError($"Error reading record: ", e);
            throw;
        }
    }

    private static int FindXlsxTotalsRow(ExcelWorksheet worksheet)
    {
        for (int row = 1; row <= worksheet.Dimension.Rows; row++)
        {
            var fields = new List<string>();
            for (int col = 1; col <= worksheet.Dimension.Columns; col++)
            {
                fields.Add(worksheet.Cells[row, col].Text.Trim());
            }
            
            int emptyFieldCount = fields.Take(fields.Count - 1).Count(f => string.IsNullOrWhiteSpace(f));
            string lastField = fields.LastOrDefault() ?? string.Empty;

            // Criteria for total row:
            // 1. Most fields (50%+) are empty
            // 2. Last field contains a numeric value
            bool mostFieldsEmpty = (double)emptyFieldCount / (fields.Count - 1) >= 0.5;
            bool lastFieldHasValue = !string.IsNullOrWhiteSpace(lastField);
            bool lastFieldIsNumeric = decimal.TryParse(lastField.Replace("$", "").Replace(",", ""), out _);
            bool hasDateInRow = DateOnly.TryParse(fields[0], out _);
            bool hasPoNumber = fields[3].Contains('V');
            bool hasOrderNumber = int.TryParse(fields[5], out _);
	        
            if(mostFieldsEmpty && lastFieldHasValue && lastFieldIsNumeric && !hasDateInRow && !hasPoNumber && !hasOrderNumber)
                return row; // Return 1-based index of the total row
        }
        return -1;
    }
    
    private static int FindXlsxHeaderRow(string[] expectedHeaders, ExcelWorksheet worksheet)
    {
        for (int row = 1; row <= worksheet.Dimension.Rows; row++)
        {
            var headerRow = new List<string>();
            for (int col = 1; col <= worksheet.Dimension.Columns; col++)
            {
                headerRow.Add(worksheet.Cells[row, col].Text.Trim());
            }

            if (expectedHeaders.All(header => headerRow.Any(h => string.Equals(h, header, StringComparison.OrdinalIgnoreCase))))
            {
                return row; // Return 1-based index of the header row
            }
        }

        throw new InvalidOperationException("Header row not found in Excel file");
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
            ShouldSkipRecord = args => 
            {
                // Skip rows before header
                if (args.Row.Parser.Row <= headerRowIndex)
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

        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, config);

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
        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
    
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