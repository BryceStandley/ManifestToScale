using System.Text;
using FTG.Core.Logging;

namespace FTG.Core.PDF;
using Manifest;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using iText.Kernel.Pdf.Canvas;
using iText.Kernel.Geom;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public static class PdfProcessor
{

    /// <summary> Combines all pages of a PDF into a single page, stacking them vertically. </summary>
    /// <param name="inputPath">Input file path</param>
    /// <param name="outputPath">Output file path</param>
    public static bool SimplifyPdf(string inputPath, string outputPath)
    {
        // Check if the output file already exists and delete it
        if (System.IO.Path.Exists(outputPath))
        {
            GlobalLogger.LogInfo($"Output file already exists: {outputPath}. Deleting it.");
            File.Delete(outputPath);
        }
            
        var originalDocument = new PdfDocument(new PdfReader(inputPath));
        var newDocument = new PdfDocument(new PdfWriter(outputPath));
        
        try
        {
            
            var numOfPages = originalDocument.GetNumberOfPages();
            var pageSize = originalDocument.GetPage(1).GetPageSize();
            
            var newSinglePage = newDocument.AddNewPage(new PageSize(pageSize.GetWidth(), pageSize.GetHeight() * numOfPages));
            
            var canvas = new PdfCanvas(newSinglePage);
            
            for(var i = 1; i <= numOfPages; i++)
            {
                var page = originalDocument.GetPage(i);
                var formXObject = page.CopyAsFormXObject(newDocument);
                
                // Calculate position for each page
                var yPosition = (numOfPages - i) * pageSize.GetHeight();
                
                canvas.AddXObjectAt(formXObject, 0, yPosition);
            }
            
            newDocument.Close();
            originalDocument.Close();
            return true;

        }
        catch (Exception ex)
        {
            GlobalLogger.LogError($"Error simplifying pdf: {ex.Message}");
            newDocument.Close();
            originalDocument.Close();
            return false;
        }

    }
    
    public static OrderManifest? ConvertPdfToExcel(string inputPath, string outputPath)
    {
        ExcelPackage.License.SetNonCommercialPersonal("Bryce Standley");
        
        var extractedText = ExtractText(inputPath);

        if (extractedText == string.Empty)
        {
            GlobalLogger.LogInfo($"Extracted text from PDF is empty or null. Please check the PDF file.");
            return null;
        }
        
        File.WriteAllText(System.IO.Path.ChangeExtension(outputPath, "txt"), extractedText);
        
        var cleanedText = CleanExtractedText(extractedText);
        
        var cleanedOutputPath = System.IO.Path.ChangeExtension(outputPath, "_cleaned.txt");
        File.WriteAllText(cleanedOutputPath, cleanedText);

        var manifest = new OrderManifest(CreateOrdersFromText(cleanedText));

        CreateExcelFromManifest(manifest, outputPath);
        
        return manifest;
    }

    private static List<StoreOrder> CreateOrdersFromText(string extractedText)
    {
        // Split the text into lines
        var lines = extractedText.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries);
        
        // Create a list to hold the orders
        var orders = (from line in lines where !line.StartsWith("ShipDate StoreNum StoreName PO# Cust# Order# Inv# Qty Crates") select new StoreOrder(line)).ToList();


        return orders.ToList();
    }
    
    private static string ExtractText(string pdfPath)
    {
        //Open the PDF document outside the try-catch block to ensure it is disposed of properly
        var pdfDoc = new PdfDocument(new PdfReader(pdfPath));
        
        try
        {
            var text = new StringBuilder();
            
            var strategy = new SimpleTextExtractionStrategy();
            var pageText = PdfTextExtractor.GetTextFromPage(pdfDoc.GetPage(1), strategy);
            text.Append(pageText);
            
            pdfDoc.Close();
            
            return text.ToString();
        }
        catch (Exception ex)
        {
            GlobalLogger.LogError($"Error extracting text: {ex.Message}");
            pdfDoc.Close();
            return "";
        }
    }

    private static string CleanExtractedText(string extractedText)
    {
        string[] stringLinesToRemove = ["Fresh To Go Foods Pty Ltd Cross Dock Manifest", "Manifest Group:", "Page: ", "Total ", "Receiver Name:", "Signature:", "Temperature:", "Dollies/Pallets:", "ShipDate StoreNum StoreName PO# Cust# Order# Inv# Qty Crates"];
        
        var lines = extractedText.Split('\n');
        var filteredLines = lines.Where(line => !stringLinesToRemove.Any(line.Contains))
                                .Select(line => line.Trim())
                                .Where(line => !string.IsNullOrWhiteSpace(line))
                                .ToList();

        const string headers = "ShipDate StoreNum StoreName PO# Cust# Order# Inv# Qty Crates";

        var output = headers + "\n" + string.Join("\n", filteredLines);
        
        return output;
    }
    
    private static void CreateExcelFromManifest(OrderManifest manifest, string outputPath)
    {
        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("Manifest Data");
        
        // Add headers
        worksheet.Cells[1, 1].Value = "ShipDate";
        worksheet.Cells[1, 2].Value = "StoreNum";
        worksheet.Cells[1, 3].Value = "StoreName";
        worksheet.Cells[1, 4].Value = "PO";
        worksheet.Cells[1, 5].Value = "CustNum";
        worksheet.Cells[1, 6].Value = "OrderNum";
        worksheet.Cells[1, 7].Value = "InvNum";
        worksheet.Cells[1, 8].Value = "Qty";
        worksheet.Cells[1, 9].Value = "Crates";

        foreach (var order in manifest.GetOrders())
        {
            var row = worksheet.Dimension.Rows + 1; // Get the next empty row
            
            worksheet.Cells[row, 1].Value = order.OrderDate.ToString("dd/MM/yyyy");
            worksheet.Cells[row, 2].Value = order.StoreNumber;
            worksheet.Cells[row, 3].Value = order.StoreName;
            worksheet.Cells[row, 4].Value = order.PoNumber;
            worksheet.Cells[row, 5].Value = order.CustomerNumber;
            worksheet.Cells[row, 6].Value = order.OrderNumber;
            worksheet.Cells[row, 7].Value = order.InvoiceNumber;
            worksheet.Cells[row, 8].Value = order.Quantity;
            worksheet.Cells[row, 9].Value = order.CrateQuantity;
        }
        
        package.SaveAs(new FileInfo(outputPath));
        
        GlobalLogger.LogInfo($"Excel file created: {outputPath}");
    }
    
}
