using System.Xml.Linq;
using JetBrains.Annotations;

namespace FTG.Core.Manifest;

public class ShipmentFile(List<Shipment> shipments)
{
    private List<Shipment> Shipments { get; } = shipments;
    private XDocument? ShipmentXml { get; set; }

    public XDocument GetShipmentXml()
    {
        ShipmentXml = ShipmentXmlBuilder.BuildShipmentXml(Shipments);
        return ShipmentXml;
    }

    private static class ShipmentXmlBuilder
    {
        private static readonly XNamespace Namespace = "http://www.manh.com/ILSNET/Interface";

        public static XDocument BuildShipmentXml(List<Shipment> data)
        {
            var document = new XDocument(
                new XDeclaration("1.0", "utf-8", null),
                new XElement(Namespace + "Shipments",
                    new XAttribute(XNamespace.Xmlns + "ns0", Namespace.NamespaceName),
                    data.Select(BuildInternalShipmentXml)
                )
            );

            return document;
        }
        
        [UsedImplicitly]
        public static XDocument BuildShipmentXml(Shipment data)
        {
            var document = new XDocument(
                new XDeclaration("1.0", "utf-8", null),
                new XElement(Namespace + "Shipments",
                    new XAttribute(XNamespace.Xmlns + "ns0", Namespace.NamespaceName),
                    new XElement(Namespace + "Shipment",
                        new XElement(Namespace + "Action", "SAVE"),
                        new XElement(Namespace + "CreationDateTimeStamp", $"{data.CreationDate}"),
                        // UserDef fields
                        new XElement(Namespace + "UserDef1", data.Qty),
                        new XElement(Namespace + "UserDef2", data.CustomerNumber),
                        new XElement(Namespace + "UserDef7", "0"),
                        new XElement(Namespace + "UserDef8", "0"),
                        new XElement(Namespace + "UserStamp", "INTERFACE"),

                        // Carrier section
                        new XElement(Namespace + "Carrier",
                            new XElement(Namespace + "Action", "Save"),
                            new XElement(Namespace + "Carrier", "TOLL")
                        ),

                        // Customer section
                        new XElement(Namespace + "Customer",
                            new XElement(Namespace + "Company", data.Company.Company),
                            new XElement(Namespace + "Customer", data.StoreNumber),
                            new XElement(Namespace + "FreightBillTo", data.StoreNumber)
                        ),

                        new XElement(Namespace + "CustomerPO", ""),
                        new XElement(Namespace + "ErpOrder", data.PoNumber),
                        new XElement(Namespace + "OrderDate", data.OrderDate),
                        new XElement(Namespace + "OrderType", data.OrderType),
                        new XElement(Namespace + "PlannedShipDate", data.OrderDate),
                        new XElement(Namespace + "ScheduledShipDate", data.OrderDate),
                        new XElement(Namespace + "ShipmentId", data.OrderNumber),
                        new XElement(Namespace + "Warehouse", "PER"),



                        // Details section
                        new XElement(Namespace + "Details",
                            new XElement(Namespace + "ShipmentDetail",
                                new XElement(Namespace + "Action", "SAVE"),
                                new XElement(Namespace + "CreationDateTimeStamp", data.CreationDate),
                                new XElement(Namespace + "ErpOrder", data.PoNumber),
                                new XElement(Namespace + "ErpOrderLineNum", "00001"),

                                // SKU section
                                new XElement(Namespace + "SKU",
                                    new XElement(Namespace + "Company", data.Company.Company),
                                    new XElement(Namespace + "Item", data.Company.VendorSkuNumber),
                                    new XElement(Namespace + "ItemCategories",
                                        new XElement(Namespace + "Category1", data.Company.VendorSkuNumber)
                                    ),
                                    new XElement(Namespace + "Quantity", data.CrateQty),
                                    new XElement(Namespace + "QuantityUm", "UN")
                                )
                            )
                        )
                    )
                )
            );

            return document;
        }

        

        private static XElement BuildInternalShipmentXml(Shipment data)
        {
            var document = new XElement(new XElement(Namespace + "Shipment",
                    new XElement(Namespace + "Action", "SAVE"),
                    new XElement(Namespace + "CreationDateTimeStamp", $"{data.CreationDate}"),
                    // UserDef fields
                    new XElement(Namespace + "UserDef1", data.Qty),
                    new XElement(Namespace + "UserDef2", data.CustomerNumber),
                    new XElement(Namespace + "UserDef7", "0"),
                    new XElement(Namespace + "UserDef8", "0"),

                    new XElement(Namespace + "UserStamp", "INTERFACE"),

                    // Carrier section
                    new XElement(Namespace + "Carrier",
                        new XElement(Namespace + "Action", "Save"),
                        new XElement(Namespace + "Carrier", "TOLL")
                    ),

                    // Customer section
                    new XElement(Namespace + "Customer",
                        new XElement(Namespace + "Company", data.Company.Company),
                        new XElement(Namespace + "Customer", data.StoreNumber),
                        new XElement(Namespace + "FreightBillTo", data.StoreNumber)
                    ),

                    new XElement(Namespace + "CustomerPO", ""),
                    new XElement(Namespace + "ErpOrder", data.PoNumber),
                    new XElement(Namespace + "OrderDate", data.OrderDate),
                    new XElement(Namespace + "OrderType", data.OrderType),
                    new XElement(Namespace + "PlannedShipDate", data.OrderDate),
                    new XElement(Namespace + "ScheduledShipDate", data.OrderDate),
                    new XElement(Namespace + "ShipmentId", data.OrderNumber),
                    new XElement(Namespace + "Warehouse", "PER"),

                    // Details section
                    new XElement(Namespace + "Details",
                        new XElement(Namespace + "ShipmentDetail",
                            new XElement(Namespace + "Action", "SAVE"),
                            new XElement(Namespace + "CreationDateTimeStamp", data.CreationDate),
                            new XElement(Namespace + "ErpOrder", data.PoNumber),
                            new XElement(Namespace + "ErpOrderLineNum", "00001"),

                            // SKU section
                            new XElement(Namespace + "SKU",
                                new XElement(Namespace + "Company", data.Company.Company),
                                new XElement(Namespace + "Item", data.Company.VendorSkuNumber),
                                new XElement(Namespace + "ItemCategories",
                                    new XElement(Namespace + "Category1", data.Company.VendorSkuNumber)
                                ),
                                new XElement(Namespace + "Quantity", data.CrateQty),
                                new XElement(Namespace + "QuantityUm", "UN")
                            )
                        )
                    )
                )
            );

            return document;
        }
    }
}

public class Shipment
{
    public string CreationDate { get; init; } = string.Empty;
    public string OrderDate { get; init; } = string.Empty;
    public string StoreNumber { get; init; } = string.Empty;
    public string PoNumber { get; init; } = string.Empty;
    public string OrderNumber { get; init; } = string.Empty;
    public string CustomerNumber { get; init; } = string.Empty;
    public string Qty { get; init; } = string.Empty;
    public string CrateQty { get; init; } = string.Empty;
    public ScaleCompany Company { get; init; } = new();
    public string OrderType { get; init; } = "FTG";
        
}