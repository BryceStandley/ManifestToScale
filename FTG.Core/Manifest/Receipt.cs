using System.Xml.Linq;

namespace FTG.Core.Manifest;

public class Receipt
{
        public string CreationDateTime { get; set; } = string.Empty;
        public string UserDef7 { get; set; } = string.Empty;
        public string UserDef8 { get; set; } = string.Empty;
        public string UserDef6 { get; set; } = string.Empty;
        public string ReceiptDate { get; set; } = string.Empty;
        public string ReceiptId { get; set; } = string.Empty;
        public string Quantity { get; set; } = string.Empty;
        public ScaleCompany Company { get; set; } = new ScaleCompany();
        public string ReceiptPrefix { get; set; } = "FTG/";
        
        private XDocument? ReceiptXml { get; set; }
        
        public Receipt()
        {
        }

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
    
        private class ReceiptXmlBuilder
        {
            private static readonly XNamespace _namespace = "http://www.manh.com/ILSNET/Interface";
            public static XDocument BuildReceiptXml(Receipt data)
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
                            new XElement(_namespace + "Company", data.Company),
                            new XElement(_namespace + "ReceiptDate", data.ReceiptDate),
                            new XElement(_namespace + "ReceiptId", data.ReceiptId),
                            new XElement(_namespace + "ReceiptIdType", "PO"),

                            // Vendor section
                            new XElement(_namespace + "Vendor",
                                new XElement(_namespace + "Company", data.Company),
                                new XElement(_namespace + "ShipFrom", data.Company.VendorNumber),
                                new XElement(_namespace + "ShipFromAddress", 
                                    new XElement(_namespace + "Name", data.Company.VendorName)
                                    ),
                                new XElement(_namespace + "SourceAddress", "")
                            ),
                            
                            new XElement(_namespace + "Warehouse", "PER"),

                            // Details section
                            new XElement(_namespace + "Details",
                                new XElement(_namespace + "ReceiptDetail",
                                    new XElement(_namespace + "Action", "NEW"),
                                    new XElement(_namespace + "UserDef1", data.Company.VendorNumber),
                                    new XElement(_namespace + "UserDef6", data.ReceiptId),
                                    new XElement(_namespace + "ErpOrderLineNum", "1"),

                                    // SKU section
                                    new XElement(_namespace + "SKU",
                                        new XElement(_namespace + "Company", data.Company),
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
        
}