namespace FTG_PDF_API;

public class ScaleInterfaceTemplate
{
    public string ShipmentHeaderTemplate =
        "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<ns0:Shipments\n\txmlns:ns0=\"http://www.manh.com/ILSNET/Interface\">";
    
    public string ShipmentFooterTemplate = "</ns0:Shipments>";
    
    public string ShipmentTemplate =
        "<ns0:Shipment>\n\t\t<ns0:Action>SAVE</ns0:Action>\n\t\t<ns0:CreationDateTimeStamp>{dateRevDash}T10:46:11</ns0:CreationDateTimeStamp>\n\t\t<ns0:UserStamp>INTERFACE</ns0:UserStamp>\n\t\t<ns0:Carrier>\n\t\t\t<ns0:Action>Save</ns0:Action>\n\t\t\t<ns0:Carrier>TOLL</ns0:Carrier>\n\t\t</ns0:Carrier>\n\t\t<ns0:Customer>\n\t\t\t<ns0:Company>PER-CO-FTG</ns0:Company>\n\t\t\t<ns0:Customer>{storeNum}</ns0:Customer>\n\t\t\t<ns0:FreightBillTo>{storeNum}</ns0:FreightBillTo>\n\t\t</ns0:Customer>\n\t\t<ns0:CustomerPO></ns0:CustomerPO>\n\t\t<ns0:ErpOrder>{poNum}</ns0:ErpOrder>\n\t\t<ns0:OrderDate>{dateRevDash}T00:00:00</ns0:OrderDate>\n\t\t<ns0:OrderType>FTG</ns0:OrderType>\n\t\t<ns0:PlannedShipDate>{dateRevDash}T00:00:00</ns0:PlannedShipDate>\n\t\t<ns0:ScheduledShipDate>{dateRevDash}T00:00:00</ns0:ScheduledShipDate>\n\t\t<ns0:ShipmentId>{orderNum}</ns0:ShipmentId>\n\t\t<ns0:Warehouse>PER</ns0:Warehouse>\n\t\t<ns0:UserDef1>{qty}<ns0:UserDef1>\n\t\t<ns0:UserDef2>{customerNum}<ns0:UserDef2>\n\t\t<ns0:UserDef7>0<ns0:UserDef7>\n\t\t<ns0:UserDef8>0<ns0:UserDef8>\n\t\t<ns0:UserDef9>0<ns0:UserDef9>\n\t\t<ns0:UserDef10>0<ns0:UserDef10>\n\t\t<ns0:Details>\n\t\t\t<ns0:ShipmentDetail>\n\t\t\t\t<ns0:Action>SAVE</ns0:Action>\n\t\t\t\t<ns0:CreationDateTimeStamp>{dateRevDash}T08:46:11</ns0:CreationDateTimeStamp>\n\t\t\t\t<ns0:ErpOrder>{poNum}</ns0:ErpOrder>\n\t\t\t\t<ns0:ErpOrderLineNum>00001</ns0:ErpOrderLineNum>\n\t\t\t\t<ns0:SKU>\n\t\t\t\t\t<ns0:Company>PER-CO-FTG</ns0:Company>\n\t\t\t\t\t<ns0:Item>1111</ns0:Item>\n\t\t\t\t\t<ns0:ItemCategories>\n\t\t\t\t\t\t<ns0:Category1>1111</ns0:Category1>\n\t\t\t\t\t</ns0:ItemCategories>\n\t\t\t\t\t<ns0:Quantity>{crateQty}</ns0:Quantity>\n\t\t\t\t\t<ns0:QuantityUm>UN</ns0:QuantityUm>\n\t\t\t\t</ns0:SKU>\n\t\t\t</ns0:ShipmentDetail>\n\t\t</ns0:Details>\n\t</ns0:Shipment>";
    
    public string ReceiptHeaderTemplate =
        "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<ns0:Receipts\n\txmlns:ns0=\"http://www.manh.com/ILSNET/Interface\">";
    
    public string ReceiptFooterTemplate = "</ns0:Receipts>";

    public string ReceiptTemplate =
        "<ns0:Receipt>\n\t\t<ns0:Action>NEW</ns0:Action>\n\t\t<ns0:CreationDateTimeStamp>{dateRevDash}T07:00:00</ns0:CreationDateTimeStamp>\n\t\t<ns0:UserDef6>Y</ns0:UserDef6>\n\t\t<ns0:UserDef7>0{dateRev}</ns0:UserDef7>\n\t\t<ns0:UserDef8>0{dateRev}</ns0:UserDef8>\n\t\t<ns0:UserStamp>ILSSRV</ns0:UserStamp>\n\t\t<ns0:Company>PER-CO-FTG</ns0:Company>\n\t\t<ns0:ReceiptDate>2025-06-10T00:00:00</ns0:ReceiptDate>\n\t\t<ns0:ReceiptId>FTG/{date}</ns0:ReceiptId>\n\t\t<ns0:ReceiptIdType>PO</ns0:ReceiptIdType>\n\t\t<ns0:Vendor>\n\t\t\t<ns0:Company>PER-CO-FTG</ns0:Company>\n\t\t\t<ns0:ShipFrom>853540</ns0:ShipFrom>\n\t\t\t<ns0:ShipFromAddress>\n\t\t\t\t<ns0:Name>FRESH TO GO FOODS-853540</ns0:Name>\n\t\t\t</ns0:ShipFromAddress>\n\t\t\t<ns0:SourceAddress />\n\t\t</ns0:Vendor>\n\t\t<ns0:Warehouse>PER</ns0:Warehouse>\n\t\t<ns0:Details>\n\t\t\t<ns0:ReceiptDetail>\n\t\t\t\t<ns0:Action>NEW</ns0:Action>\n\t\t\t\t<ns0:UserDef1>853540</ns0:UserDef1>\n\t\t\t\t<ns0:UserDef6>FTG/{date}</ns0:UserDef6>\n\t\t\t\t<ns0:ErpOrderLineNum>1</ns0:ErpOrderLineNum>\n\t\t\t\t<ns0:SKU>\n\t\t\t\t\t<ns0:Company>PER-CO-FTG</ns0:Company>\n\t\t\t\t\t<ns0:HarmCode />\n\t\t\t\t\t<ns0:Item>1111</ns0:Item>\n\t\t\t\t\t<ns0:Quantity>{qty}</ns0:Quantity>\n\t\t\t\t\t<ns0:QuantityUm>UN</ns0:QuantityUm>\n\t\t\t\t</ns0:SKU>\n\t\t\t</ns0:ReceiptDetail>\n\t\t</ns0:Details>\n\t</ns0:Receipt>";
}