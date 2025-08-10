namespace FTG.Core.Manifest;

public class PurchaseOrder
{
    public string CreationDateTime { get; set; } = string.Empty;
    public string UserDef7 { get; set; } = string.Empty;
    public string UserDef8 { get; set; } = string.Empty;
    public string PurchaseOrderDate { get; set; } = string.Empty;
    public string PurchaseOrderId { get; set; } = string.Empty;
    public string Quantity { get; set; } = string.Empty;
    public ScaleCompany Company { get; set; } = new();
    public string PurchaseOrderPrefix { get; set; } = "FTG/";

    public PurchaseOrder() {}
}