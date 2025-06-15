using System.Globalization;
using System.Xml.Linq;
using CsvHelper;

namespace FTG_PDF_API;

public class ManifestToScale
{
    public static void ConvertManifestToCsv(FreshToGoManifest manifest, string outputFile)
    {
        try
        {
            using (var writer = new StreamWriter(outputFile))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteRecords(manifest.GetOrders());
                writer.Flush();
            }

            Console.WriteLine("CSV file written successfully to: " + outputFile);
        }
        catch (Exception e)
        {
            Console.WriteLine("Error writing to CSV file: " + e.Message);
        }

    }

    public static void GenerateReceiptFromTemplate(FreshToGoManifest manifest, string outputFile)
    {
        try
        {
            var details = GenerateReceiptDetails(manifest);
            
            var doc = ReceiptXmlBuilder.BuildReceiptXml(details);
            doc.Save(outputFile);
            Console.WriteLine("Receipt generated successfully: " + outputFile);
            
        }
        catch (Exception e)
        {
            Console.WriteLine("Error generating receipt from template: " + e.Message);
        }
    }
    
    public static void GenerateShipmentFromTemplate(FreshToGoManifest manifest, string outputFile)
    {
        try
        {
            
            var shipmentDoc = ShipmentXmlBuilder.BuildShipmentXml(GenerateShipmentDetails(manifest));
            shipmentDoc.Save(outputFile);
            
            Console.WriteLine("Shipments generated successfully: " + outputFile);
        }
        catch (Exception e)
        {
            Console.WriteLine("Error generating shipments from template: " + e.Message);
        }
    }

    private static ReceiptDetails GenerateReceiptDetails(FreshToGoManifest manifest)
    {
        var details = new ReceiptDetails();
        
        details.CreationDateTime = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);
        
        details.UserDef7 = "0" + manifest.GetManifestDate().ToString("yyyyMMdd");

        details.UserDef8 = details.UserDef7;
        
        details.ReceiptDate = manifest.GetManifestDate().ToDateTime(TimeOnly.MinValue).ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);
        
        details.ReceiptId = "853540/" + manifest.GetManifestDate().ToString("ddMMyyyy");
        
        details.UserDef6 = details.ReceiptId;
        
        details.Quantity = manifest.GetTotalCrates().ToString();
        
        return details;
    }
    
    private class ReceiptDetails
    {
        public ReceiptDetails(string creationDateTime, string userDef7, string userDef8, string userDef6, string receiptDate, string receiptId, string quantity)
        {
            CreationDateTime = creationDateTime;
            UserDef7 = userDef7;
            UserDef8 = userDef8;
            UserDef6 = userDef6;
            ReceiptDate = receiptDate;
            ReceiptId = receiptId;
            Quantity = quantity;
        }
        
        public ReceiptDetails()
        {
            CreationDateTime = string.Empty;
            UserDef7 = string.Empty;
            UserDef8 = string.Empty;
            UserDef6 = string.Empty;
            ReceiptDate= string.Empty;
            ReceiptId = string.Empty;
            Quantity = string.Empty;
        }

        public string CreationDateTime { get; set; }
        public string UserDef7 { get; set; }
        public string UserDef8 { get; set; }
        public string UserDef6 { get; set; }
        public string ReceiptDate { get; set; }
        public string ReceiptId { get; set; }
        public string Quantity { get; set; }
    }
    

    private static List<ShipmentDetails> GenerateShipmentDetails(FreshToGoManifest manifest)
    {
        var shipmentDetailsList = new List<ShipmentDetails>();
        foreach(var order in manifest.GetOrders())
        {
            var details = new ShipmentDetails
            {
                CreationDate = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture),
                StoreNumber = order.StoreNumber.Count() < 4 ? order.StoreNumber.PadLeft(4, '0') : order.StoreNumber,
                PoNumber = order.PoNumber,
                OrderNumber = order.OrderNumber,
                CustomerNumber = order.CustomerNumber,
                Qty = order.Quantity.ToString(),
                CrateQty = order.CrateQuantity.ToString()
            };
            shipmentDetailsList.Add(details);
        }
        
        
        return shipmentDetailsList;
    }
    
    private class ShipmentDetails
    {
        public string CreationDate { get; set; } = string.Empty;
        public string StoreNumber { get; set; } = string.Empty;
        public string PoNumber { get; set; } = string.Empty;
        public string OrderNumber { get; set; } = string.Empty;
        public string CustomerNumber { get; set; } = string.Empty;
        public string Qty { get; set; } = string.Empty;
        public string CrateQty { get; set; } = string.Empty;
    }

    private class ReceiptXmlBuilder
    {
        private static readonly XNamespace _namespace = "http://www.manh.com/ILSNET/Interface";
        
        public static XDocument BuildReceiptXml(ReceiptDetails data)
        {
            var document = new XDocument(
                new XDeclaration("1.0", "utf-8", null),
                new XElement(_namespace + "Receipts",
                    new XAttribute(XNamespace.Xmlns + "ns0", _namespace.NamespaceName),
                    new XElement(_namespace + "Receipt",
                        new XElement(_namespace + "Action", "NEW"),
                        new XElement(_namespace + "CreationDateTimeStamp", data.CreationDateTime),
                        new XElement(_namespace + "UserDef6", "Y"),
                        new XElement(_namespace + "UserDef7", data.UserDef7),
                        new XElement(_namespace + "UserDef8", data.UserDef8),
                        new XElement(_namespace + "UserStamp", "ILSSRV"),
                        new XElement(_namespace + "Company", "PER-CO-FTG"),
                        new XElement(_namespace + "ReceiptDate", data.ReceiptDate),
                        new XElement(_namespace + "ReceiptId", data.ReceiptId),
                        new XElement(_namespace + "ReceiptIdType", "PO"),

                        // Vendor section
                        new XElement(_namespace + "Vendor",
                            new XElement(_namespace + "Company", "PER-CO-FTG"),
                            new XElement(_namespace + "ShipFrom", "853540"),
                            new XElement(_namespace + "ShipFromAddress", 
                                new XElement(_namespace + "Name", "FRESH TO GO FOODS-853540")
                                ),
                            new XElement(_namespace + "SourceAddress", "")
                        ),
                        
                        new XElement(_namespace + "Warehouse", "PER"),

                        // Details section
                        new XElement(_namespace + "Details",
                            new XElement(_namespace + "ReceiptDetail",
                                new XElement(_namespace + "Action", "NEW"),
                                new XElement(_namespace + "UserDef1", "853540"),
                                new XElement(_namespace + "UserDef6", data.ReceiptId),
                                new XElement(_namespace + "ErpOrderLineNum", "1"),

                                // SKU section
                                new XElement(_namespace + "SKU",
                                    new XElement(_namespace + "Company", "PER-CO-FTG"),
                                    new XElement(_namespace + "HarmCode", ""),
                                    new XElement(_namespace + "Item", "1111"),
                                    new XElement(_namespace + "Quantity", data.Quantity),
                                    new XElement(_namespace + "QuantityUm", "UN")
                                )
                            )
                        )
                    )
                )
            );

            return document;
        }
    }
    
    private class ShipmentXmlBuilder
    {
        private static readonly XNamespace _namespace = "http://www.manh.com/ILSNET/Interface";

        public static XDocument BuildShipmentXml(ShipmentDetails data)
        {
            var document = new XDocument(
                new XDeclaration("1.0", "utf-8", null),
                new XElement(_namespace + "Shipments",
                    new XAttribute(XNamespace.Xmlns + "ns0", _namespace.NamespaceName),
                    new XElement(_namespace + "Shipment",
                        new XElement(_namespace + "Action", "SAVE"),
                        new XElement(_namespace + "CreationDateTimeStamp", $"{data.CreationDate}"),
                        // UserDef fields
                        new XElement(_namespace + "UserDef1", data.Qty),
                        new XElement(_namespace + "UserDef2", data.CustomerNumber),
                        new XElement(_namespace + "UserDef7", "0"),
                        new XElement(_namespace + "UserDef8", "0"),
                        new XElement(_namespace + "UserStamp", "INTERFACE"),

                        // Carrier section
                        new XElement(_namespace + "Carrier",
                            new XElement(_namespace + "Action", "Save"),
                            new XElement(_namespace + "Carrier", "TOLL")
                        ),

                        // Customer section
                        new XElement(_namespace + "Customer",
                            new XElement(_namespace + "Company", "PER-CO-FTG"),
                            new XElement(_namespace + "Customer", data.StoreNumber),
                            new XElement(_namespace + "FreightBillTo", data.StoreNumber)
                        ),

                        new XElement(_namespace + "CustomerPO", ""),
                        new XElement(_namespace + "ErpOrder", data.PoNumber),
                        new XElement(_namespace + "OrderDate", data.CreationDate),
                        new XElement(_namespace + "OrderType", "FTG"),
                        new XElement(_namespace + "PlannedShipDate", data.CreationDate),
                        new XElement(_namespace + "ScheduledShipDate", data.CreationDate),
                        new XElement(_namespace + "ShipmentId", data.OrderNumber),
                        new XElement(_namespace + "Warehouse", "PER"),



                        // Details section
                        new XElement(_namespace + "Details",
                            new XElement(_namespace + "ShipmentDetail",
                                new XElement(_namespace + "Action", "SAVE"),
                                new XElement(_namespace + "CreationDateTimeStamp", data.CreationDate),
                                new XElement(_namespace + "ErpOrder", data.PoNumber),
                                new XElement(_namespace + "ErpOrderLineNum", "00001"),

                                // SKU section
                                new XElement(_namespace + "SKU",
                                    new XElement(_namespace + "Company", "PER-CO-FTG"),
                                    new XElement(_namespace + "Item", "1111"),
                                    new XElement(_namespace + "ItemCategories",
                                        new XElement(_namespace + "Category1", "1111")
                                    ),
                                    new XElement(_namespace + "Quantity", data.CrateQty),
                                    new XElement(_namespace + "QuantityUm", "UN")
                                )
                            )
                        )
                    )
                )
            );

            return document;
        }

        public static XDocument BuildShipmentXml(List<ShipmentDetails> data)
        {
            var document = new XDocument(
                new XDeclaration("1.0", "utf-8", null),
                new XElement(_namespace + "Shipments", 
                    new XAttribute(XNamespace.Xmlns + "ns0", _namespace.NamespaceName),
                    data.Select(BuildInternalShipmentXml)
                )
            );

            return document;
        }

        public static XElement BuildInternalShipmentXml(ShipmentDetails data)
        {
            var document = new XElement(new XElement(_namespace + "Shipment",
                    new XElement(_namespace + "Action", "SAVE"),
                    new XElement(_namespace + "CreationDateTimeStamp", $"{data.CreationDate}"),
                    // UserDef fields
                    new XElement(_namespace + "UserDef1", data.Qty),
                    new XElement(_namespace + "UserDef2", data.CustomerNumber),
                    new XElement(_namespace + "UserDef7", "0"),
                    new XElement(_namespace + "UserDef8", "0"),

                    new XElement(_namespace + "UserStamp", "INTERFACE"),

                    // Carrier section
                    new XElement(_namespace + "Carrier",
                        new XElement(_namespace + "Action", "Save"),
                        new XElement(_namespace + "Carrier", "TOLL")
                    ),

                    // Customer section
                    new XElement(_namespace + "Customer",
                        new XElement(_namespace + "Company", "PER-CO-FTG"),
                        new XElement(_namespace + "Customer", data.StoreNumber),
                        new XElement(_namespace + "FreightBillTo", data.StoreNumber)
                    ),

                    new XElement(_namespace + "CustomerPO", ""),
                    new XElement(_namespace + "ErpOrder", data.PoNumber),
                    new XElement(_namespace + "OrderDate", data.CreationDate),
                    new XElement(_namespace + "OrderType", "FTG"),
                    new XElement(_namespace + "PlannedShipDate", data.CreationDate),
                    new XElement(_namespace + "ScheduledShipDate", data.CreationDate),
                    new XElement(_namespace + "ShipmentId", data.OrderNumber),
                    new XElement(_namespace + "Warehouse", "PER"),

                    // Details section
                    new XElement(_namespace + "Details",
                        new XElement(_namespace + "ShipmentDetail",
                            new XElement(_namespace + "Action", "SAVE"),
                            new XElement(_namespace + "CreationDateTimeStamp", data.CreationDate),
                            new XElement(_namespace + "ErpOrder", data.PoNumber),
                            new XElement(_namespace + "ErpOrderLineNum", "00001"),

                            // SKU section
                            new XElement(_namespace + "SKU",
                                new XElement(_namespace + "Company", "PER-CO-FTG"),
                                new XElement(_namespace + "Item", "1111"),
                                new XElement(_namespace + "ItemCategories",
                                    new XElement(_namespace + "Category1", "1111")
                                ),
                                new XElement(_namespace + "Quantity", data.CrateQty),
                                new XElement(_namespace + "QuantityUm", "UN")
                            )
                        )
                    )
                )
            );
            
            return document;
        }
        
    }
}
