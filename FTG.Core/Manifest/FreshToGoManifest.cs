namespace FTG.Core.Manifest;

using System.Globalization;

public class FreshToGoOrder
{
    public DateOnly OrderDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public string StoreNumber { get; set; } = string.Empty;
    public string StoreName { get; set; }= string.Empty;
    public string PoNumber { get; set; } = string.Empty;
    public string CustomerNumber { get; set; } = string.Empty;
    public string OrderNumber { get; set;  } = string.Empty;
    public string InvoiceNumber { get; set; } = string.Empty;
    public int Quantity { get; set; } = 0;
    public int CrateQuantity { get; set; } = 0;
    
    public FreshToGoOrder() { }
    
    public FreshToGoOrder(string storeNumber, string storeName, string poNumber, string customerNumber, string orderNumber, string invoiceNumber, int quantity, int crateQuantity)
    {
        StoreNumber = storeNumber;
        StoreName = storeName;
        PoNumber = poNumber;
        CustomerNumber = customerNumber;
        OrderNumber = orderNumber;
        InvoiceNumber = invoiceNumber;
        Quantity = quantity;
        CrateQuantity = crateQuantity;
    }
    
    public FreshToGoOrder(string orderLineFromPdf)
    {
        // Example of an order line from PDF:
        //   ShipDate StoreNum StoreName PO# Cust# Order# Inv# Qty Crates
        //  02/06/2025 0332 COLES S/M GARDEN CITY 25486091V 50113007 SO77834 INV169570 3.0 1
        var parts = orderLineFromPdf.Split([' '], StringSplitOptions.RemoveEmptyEntries);
        
        
        OrderDate = DateOnly.ParseExact(parts[0], "dd/MM/yyyy", CultureInfo.InvariantCulture);
        StoreNumber = parts[1];
        
        StoreName = string.Join(" ", parts[2..^6]); // Join all parts except the last 6 which are details
        
        // Work backwards for other details which will leave the remaining parts from the store name
        CrateQuantity = int.Parse(parts[^1]);
        Quantity = (int)float.Parse(parts[^2]);
        InvoiceNumber = parts[^3];
        OrderNumber = parts[^4];
        CustomerNumber = parts[^5];
        PoNumber = parts[^6];
        
    }
}

public class FreshToGoManifest
{
    public List<FreshToGoOrder> GetOrders() => Orders;
    public int GetTotalOrders() => TotalOrders;
    public int GetTotalCrates() => _mTotalCrates;
    public DateOnly GetManifestDate() => _mManifestDate;
    
    public string GetManifestDateString() => _mManifestDate.ToString("dd-MM-yyyy");
    
    public ScaleCompany Company { get; init; } = new();

    public List<FreshToGoOrder> Orders { get; set; } = [];

    private int TotalOrders { get; }

    public DateOnly ManifestDate
    {
        get => _mManifestDate;
        set => _mManifestDate = value;
    }


    private readonly int _mTotalCrates;
    private DateOnly _mManifestDate = DateOnly.FromDateTime(DateTime.Today);
    
    
    
    
    public FreshToGoManifest() { }
    
    public FreshToGoManifest(List<FreshToGoOrder> orders)
    {
        Orders = orders;
        TotalOrders = orders.Count;
        _mTotalCrates = orders.Sum(order => order.CrateQuantity);
        if (orders.Count > 0)
        {
            _mManifestDate = orders[0].OrderDate; // Assuming all orders have the same date
        }
    }
    
    public void AddOrder(FreshToGoOrder order)
    {
        Orders.Add(order);
    }

    public void UpdateOrderPoNumber(string poNumber, string storeNumber, FreshToGoOrder updatedOrder)
    {
        var index = Orders.FindIndex(o => o.PoNumber == poNumber && o.StoreNumber == storeNumber);
        if (index != -1)
        {
            Orders[index] = updatedOrder;
        }
    }

}