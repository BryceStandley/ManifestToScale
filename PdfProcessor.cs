using System.Text;
using OfficeOpenXml;

namespace FTG_PDF_API;

using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Data;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using iText.Kernel.Pdf.Canvas;
using iText.Kernel.Pdf.Xobject;
using iText.Kernel.Geom;
using iText.Layout;
using iText.Layout.Element;
using OfficeOpenXml;
using System.Text.RegularExpressions;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;

public class PdfProcessor
{

    /// <summary> Combines all pages of a PDF into a single page, stacking them vertically. </summary>
    /// <param name="inputPath">Input file path</param>
    /// <param name="outputPath">Output file path</param>
    public static void SimplifyPdf(string inputPath, string outputPath)
    {
        // Check if the output file already exists and delete it
        if (System.IO.Path.Exists(outputPath))
        {
            Console.WriteLine($"Output file already exists: {outputPath}. Deleting it.");
            File.Delete(outputPath);
        }
            
        PdfDocument originalDocument = new PdfDocument(new PdfReader(inputPath));
        PdfDocument newDocument = new PdfDocument(new PdfWriter(outputPath));
        
        try
        {
            
            var numOfPages = originalDocument.GetNumberOfPages();
            var pageSize = originalDocument.GetPage(1).GetPageSize();
            
            var newSinglePage = newDocument.AddNewPage(new PageSize(pageSize.GetWidth(), pageSize.GetHeight() * numOfPages));
            
            var canvas = new PdfCanvas(newSinglePage);
            
            for(int i = 1; i <= numOfPages; i++)
            {
                var page = originalDocument.GetPage(i);
                var formXObject = page.CopyAsFormXObject(newDocument);
                
                // Calculate position for each page
                float yPosition = (numOfPages - i) * pageSize.GetHeight();
                
                canvas.AddXObjectAt(formXObject, 0, yPosition);
            }
            
            newDocument.Close();
            originalDocument.Close();

        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error simplifying pdf: {ex.Message}");
            newDocument.Close();
            originalDocument.Close();
        }

    }
    
    public static FreshToGoManifest? ConvertPdfToExcel(string inputPath, string outputPath)
    {
        ExcelPackage.License.SetNonCommercialPersonal("Bryce Standley");
        
        string extractedText = ExtractText(inputPath);

        if (extractedText == string.Empty)
        {
            Console.WriteLine($"Extracted text from PDF is empty or null. Please check the PDF file.");
            return null;
        }
        
        File.WriteAllText(System.IO.Path.ChangeExtension(outputPath, "txt"), extractedText);
        
        string cleanedText = CleanExtractedText(extractedText);
        
        var cleanedOutputPath = System.IO.Path.ChangeExtension(outputPath, "_cleaned.txt");
        File.WriteAllText(cleanedOutputPath, cleanedText);

        FreshToGoManifest manifest = new FreshToGoManifest(CreateOrdersFromText(cleanedText));

        CreateExcelFromManifest(manifest, outputPath);
        
        return manifest;
    }

    private static List<FreshToGoOrder> CreateOrdersFromText(string extractedText)
    {
        // Split the text into lines
        var lines = extractedText.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        
        // Create a list to hold the orders
        var orders = new List<FreshToGoOrder>();
        
        for(int i = 0; i < lines.Length; i++)
        {
            //Temp Debug
            console.WriteLine(lines[i]);
            // Skip header line
            if (lines[i].StartsWith("ShipDate StoreNum StoreName PO# Cust# Order# Inv# Qty Crates"))
                continue;
            
            orders.Add(new FreshToGoOrder(lines[i]));
        }
        
        
        return orders.ToList();
    }
    
    private static string ExtractText(string pdfPath)
    {
        //Open the PDF document outside the try-catch block to ensure it is disposed properly
        var pdfDoc = new PdfDocument(new PdfReader(pdfPath));
        
        try
        {
            var text = new StringBuilder();
            
            var strategy = new SimpleTextExtractionStrategy();
            string pageText = PdfTextExtractor.GetTextFromPage(pdfDoc.GetPage(1), strategy);
            text.Append(pageText);
            
            pdfDoc.Close();
            
            
            
            return text.ToString();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error extracting text: {ex.Message}");
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

        string headers = "ShipDate StoreNum StoreName PO# Cust# Order# Inv# Qty Crates";

        string output = headers + "\n" + string.Join("\n", filteredLines);
        
        return output;
    }
    
    private static void CreateExcelFromManifest(FreshToGoManifest manifest, string outputPath)
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
            int row = worksheet.Dimension.Rows + 1; // Get the next empty row
            
            worksheet.Cells[row, 1].Value = order.OrderDate.ToString("dd/MM/yyyy");
            worksheet.Cells[row, 2].Value = order.StoreNumber;
            worksheet.Cells[row, 3].Value = order.StoreName;
            worksheet.Cells[row, 4].Value = order.PoNumber;
            worksheet.Cells[row, 5].Value = order.CustomerNumber;
            worksheet.Cells[row, 6].Value = order.OrderNumber;
            worksheet.Cells[row, 7].Value = order.InventoryNumber;
            worksheet.Cells[row, 8].Value = order.Quantity;
            worksheet.Cells[row, 9].Value = order.CrateQuantity;
        }
        
        package.SaveAs(new FileInfo(outputPath));
        
        Console.WriteLine($"Excel file created: {outputPath}");
    }
    
}
