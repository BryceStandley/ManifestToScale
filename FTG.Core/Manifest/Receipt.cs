using System.Xml.Linq;
using JetBrains.Annotations;

namespace FTG.Core.Manifest;

public class Receipt
{
        public string CreationDateTime { get; init; } = string.Empty;
        public string UserDef7 { get; init; } = string.Empty;
        public string UserDef8 { get; set; } = string.Empty;
        public string UserDef6 { get; set; } = string.Empty;
        public string ReceiptDate { get; set; } = string.Empty;
        public string ReceiptId { get; private set; } = string.Empty;
        public string Quantity { get; set; } = string.Empty;
        public ScaleCompany Company { get; set; } = new();
        public string ReceiptPrefix { get; set; } = "FTG/";
        public PurchaseOrder PurchaseOrder {get; set; } = new();
        
        private XDocument? ReceiptXml { get; set; }
        
        public Receipt()
        {
        }

        [UsedImplicitly]
        public Receipt(string creationDateTime, string userDef7, string userDef8, string userDef6, string receiptDate, string receiptId, string quantity)
        {
                CreationDateTime = creationDateTime;
                UserDef7 = userDef7;
                UserDef8 = userDef8;
                UserDef6 = userDef6;
                ReceiptDate = receiptDate;
                ReceiptId = receiptId;
                Quantity = quantity;
        }
        
        public void SetReceiptId(string receiptId)
        {
                ReceiptId = ReceiptPrefix + receiptId;
        }
        
        public XDocument GetReceiptXml()
        {
            ReceiptXml = ReceiptXmlBuilder.BuildReceiptXml(this);
            return ReceiptXml;
        }
    
        private static class ReceiptXmlBuilder
        {
            private static readonly XNamespace Namespace = "http://www.manh.com/ILSNET/Interface";
            public static XDocument BuildReceiptXml(Receipt data)
            {
                var document = new XDocument(
                    new XDeclaration("1.0", "utf-8", null),
                    new XElement(Namespace + "Receipts",
                        new XAttribute(XNamespace.Xmlns + "ns0", Namespace.NamespaceName),
                        // PO Section
                        new XElement(Namespace + "PurchaseOrder",
                            new XElement(Namespace + "Action", "NEW"),
                            new XElement(Namespace + "CreationDateTimeStamp", data.PurchaseOrder.CreationDateTime),
                            new XElement(Namespace + "UserDef6", "Y"),
                            new XElement(Namespace + "UserDef7", data.PurchaseOrder.UserDef7),
                            new XElement(Namespace + "UserDef8", data.PurchaseOrder.UserDef8),
                            new XElement(Namespace + "UserStamp", "ILSSRV"),
                            new XElement(Namespace + "Company", data.PurchaseOrder.Company.Company),
                            new XElement(Namespace + "PurchaseOrderId", data.PurchaseOrder.PurchaseOrderId),

                            // PO Vendor section
                            new XElement(Namespace + "Vendor",
                                new XElement(Namespace + "Action", "NEW"),
                                new XElement(Namespace + "Company", data.PurchaseOrder.Company.Company),
                                new XElement(Namespace + "ShipFrom", data.PurchaseOrder.Company.VendorNumber),
                                new XElement(Namespace + "ShipFromAddress", 
                                    new XElement(Namespace + "Name", data.PurchaseOrder.Company.VendorName)
                                    ),
                                new XElement(Namespace + "SourceAddress", "")
                            ),
                        
                            new XElement(Namespace + "Warehouse", "PER"),

                            // PO Details section
                            new XElement(Namespace + "Details",
                                new XElement(Namespace + "PurchaseOrderDetail",
                                    new XElement(Namespace + "Action", "NEW"),
                                    new XElement(Namespace + "UserDef1", data.PurchaseOrder.Company.VendorNumber),
                                    new XElement(Namespace + "LineNumber", "1"),

                                    // SKU section
                                    new XElement(Namespace + "SKU",
                                        new XElement(Namespace + "Company", data.PurchaseOrder.Company.Company),
                                        new XElement(Namespace + "Item", data.PurchaseOrder.Company.VendorSkuNumber),
                                        new XElement(Namespace + "Quantity", data.PurchaseOrder.Quantity),
                                        new XElement(Namespace + "QuantityUm", "UN")
                                    )
                                )
                            )
                        ),


                        // Receipt Section
                        new XElement(Namespace + "Receipt",
                            new XElement(Namespace + "Action", "NEW"),
                            new XElement(Namespace + "CreationDateTimeStamp", data.CreationDateTime),
                            new XElement(Namespace + "UserDef6", data.UserDef6),
                            new XElement(Namespace + "UserDef7", data.UserDef7),
                            new XElement(Namespace + "UserDef8", data.UserDef8),
                            new XElement(Namespace + "UserStamp", "ILSSRV"),
                            new XElement(Namespace + "Company", data.Company.Company),
                            new XElement(Namespace + "ReceiptDate", data.ReceiptDate),
                            new XElement(Namespace + "ReceiptId", data.ReceiptId),
                            new XElement(Namespace + "ReceiptIdType", "PO"),
                            new XElement(Namespace + "TrailerId", "1"), // This allows the receipt to automatically be marked as urgent in SCI

                            // Vendor section
                            new XElement(Namespace + "Vendor",
                                new XElement(Namespace + "Company", data.Company.Company),
                                new XElement(Namespace + "ShipFrom", data.Company.VendorNumber),
                                new XElement(Namespace + "ShipFromAddress", 
                                    new XElement(Namespace + "Name", data.Company.VendorName)
                                    ),
                                new XElement(Namespace + "SourceAddress", "")
                            ),
                            
                            new XElement(Namespace + "Warehouse", "PER"),

                            // Details section
                            new XElement(Namespace + "Details",
                                new XElement(Namespace + "ReceiptDetail",
                                    new XElement(Namespace + "Action", "NEW"),
                                    new XElement(Namespace + "UserDef1", data.Company.VendorNumber),
                                    new XElement(Namespace + "UserDef6", data.ReceiptId),
                                    new XElement(Namespace + "ErpOrderLineNum", "1"),
                                    new XElement(Namespace + "PurchaseOrderLineNumber", "1"),
                                    new XElement(Namespace + "PurchaseOrderId", data.PurchaseOrder.PurchaseOrderId),

                                    // SKU section
                                    new XElement(Namespace + "SKU",
                                        new XElement(Namespace + "Company", data.Company.Company),
                                        new XElement(Namespace + "HarmCode", ""),
                                        new XElement(Namespace + "Item", data.Company.VendorSkuNumber),
                                        new XElement(Namespace + "Quantity", data.Quantity),
                                        new XElement(Namespace + "QuantityUm", "UN")
                                    )
                                )
                            )
                        )
                    )
                );

                return document;
            }
        }
        
}