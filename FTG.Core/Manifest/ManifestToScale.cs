using FTG.Core.Logging;

namespace FTG.Core.Manifest;

using System.Globalization;
using CsvHelper;

public static class ManifestToScale
{
    public static bool ConvertManifestToCsv(OrderManifest manifest, string outputFile)
    {
        try
        {
            using (var writer = new StreamWriter(outputFile))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteRecords(manifest.GetOrders());
                writer.Flush();
            }

            GlobalLogger.LogInfo("CSV file written successfully to: " + outputFile);
            return true;
        }
        catch (Exception e)
        {
            GlobalLogger.LogError("Error writing to CSV file", e);
            return false;
        }

    }

    public static bool GenerateReceiptFromTemplate(OrderManifest manifest, string outputFile)
    {
        try
        {
            var details = GenerateReceiptDetails(manifest);
            
            var doc = details.GetReceiptXml();
            doc.Save(outputFile);
            GlobalLogger.LogInfo("Receipt generated successfully: " + outputFile);
            return true;

        }
        catch (Exception e)
        {
            GlobalLogger.LogError("Error generating receipt from template", e);
            return false;
        }
    }
    
    public static bool GenerateShipmentFromTemplate(OrderManifest manifest, string outputFile)
    {
        try
        {
            var shipmentFile = new ShipmentFile(GenerateShipmentDetails(manifest));
            shipmentFile.GetShipmentXml().Save(outputFile);
            GlobalLogger.LogInfo("Shipments generated successfully: " + outputFile);
            return true;
        }
        catch (Exception e)
        {
            GlobalLogger.LogError("Error generating shipments from template", e);
            return false;
        }
    }

    private static Receipt GenerateReceiptDetails(OrderManifest manifest)
    {
        var details = new Receipt
        {
            CreationDateTime = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture),
            UserDef7 = "0" + manifest.GetManifestDate().ToString("yyyyMMdd")
        };

        details.UserDef8 = details.UserDef7;
        
        details.ReceiptDate = manifest.GetManifestDate().ToDateTime(TimeOnly.MinValue).ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);
        
        details.ReceiptPrefix = manifest.Company.VendorReceiptPrefix;
        
        details.SetReceiptId(manifest.GetManifestDate().ToString("ddMMyyyy"));
        
        details.UserDef6 = details.ReceiptId;
        
        details.Quantity = manifest.GetTotalCrates().ToString();

        details.Company = manifest.Company;

        var po = new PurchaseOrder();

        po.CreationDateTime = details.CreationDateTime;

        po.UserDef7 = details.UserDef7;

        po.UserDef8 = details.UserDef8;

        po.PurchaseOrderDate = details.ReceiptDate;

        po.PurchaseOrderId = details.ReceiptId;

        po.Quantity = details.Quantity;

        po.Company = details.Company;

        po.PurchaseOrderPrefix = details.ReceiptPrefix;

        details.PurchaseOrder = po;
        
        return details;
    }
    

    private static List<Shipment> GenerateShipmentDetails(OrderManifest manifest)
    {
        return manifest.GetOrders()
            .Select(order => new Shipment
            {
                CreationDate = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture),
                StoreNumber = order.StoreNumber.Length < 4 ? order.StoreNumber.PadLeft(4, '0') : order.StoreNumber,
                OrderDate = manifest.GetManifestDate().ToDateTime(TimeOnly.MinValue).ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture),
                PoNumber = order.PoNumber,
                OrderNumber = manifest.Company == ScaleCompany.AzuraFresh ? "CAF-" + order.OrderNumber : "CTG-" + order.OrderNumber, // Add CAF to Azura Shipments and CTG to ThemeGroup Shipments
                CustomerNumber = order.CustomerNumber,
                Qty = order.Quantity.ToString(),
                CrateQty = order.CrateQuantity.ToString(),
                Company = manifest.Company,
                OrderType = manifest.Company.Company == ScaleCompany.AzuraFresh ? "CAF" : "FTG"
            })
            .ToList();
    }
    
}
