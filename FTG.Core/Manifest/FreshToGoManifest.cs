namespace FTG.Core.Manifest;

using System.Globalization;

public class FreshToGoOrder
{
    public DateOnly OrderDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public string StoreNumber { get; set; } = "";
    public string StoreName { get; set; } = "";
    public string PoNumber { get; set; } = "";
    public string CustomerNumber { get; set; } = "";
    public string OrderNumber { get; set; } = "";
    public string InventoryNumber { get; set; } = "";
    public int Quantity { get; set; } = 0;
    public int CrateQuantity { get; set; } = 0;
    
    public FreshToGoOrder(string storeNumber, string storeName, string poNumber, string customerNumber, string orderNumber, string inventoryNumber, int quantity, int crateQuantity)
    {
        StoreNumber = storeNumber;
        StoreName = storeName;
        PoNumber = poNumber;
        CustomerNumber = customerNumber;
        OrderNumber = orderNumber;
        InventoryNumber = inventoryNumber;
        Quantity = quantity;
        CrateQuantity = crateQuantity;
    }
    
    public FreshToGoOrder(string orderLineFromPdf)
    {
        // Example of a order line from PDF:
        //   ShipDate  StoreNum StoreName           PO#      Cust#    Order#  Inv#     Qty Crates
        //  02/06/2025 0332 COLES S/M GARDEN CITY 25486091V 50113007 SO77834 INV169570 3.0 1
        string[] parts = orderLineFromPdf.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        
        
        OrderDate = DateOnly.ParseExact(parts[0], "dd/MM/yyyy", CultureInfo.InvariantCulture);
        StoreNumber = parts[1];
        
        StoreName = string.Join(" ", parts[2..^6]); // Join all parts except the last 6 which are details
        
        // Work backwards for other details which will leave the remaining parts from the store name
        CrateQuantity = int.Parse(parts[^1]);
        Quantity = (int)float.Parse(parts[^2]);
        InventoryNumber = parts[^3];
        OrderNumber = parts[^4];
        CustomerNumber = parts[^5];
        PoNumber = parts[^6];
        
    }
}

public class FreshToGoManifest
{
    public List<FreshToGoOrder> GetOrders() => m_Orders;
    public int GetTotalOrders() => m_totalOrders;
    public int GetTotalCrates() => m_totalCrates;
    public DateOnly GetManifestDate() => m_manifestDate;
    
    public ScaleCompany Company { get; set; } = new ScaleCompany();
    
    public List<FreshToGoOrder> Orders
    {
        get => m_Orders;
        set => m_Orders = value;
    }
    
    public int TotalOrders
    {
        get => m_totalOrders;
        set => m_totalOrders = value;
    }
    
    public int TotalCrates
    {
        get => m_totalCrates;
        set => m_totalCrates = value;
    }
    public DateOnly ManifestDate
    {
        get => m_manifestDate;
        set => m_manifestDate = value;
    }
    
    
    
    private List<FreshToGoOrder> m_Orders = new List<FreshToGoOrder>();
    private int m_totalOrders = 0;
    private int m_totalCrates = 0;
    private DateOnly m_manifestDate = DateOnly.FromDateTime(DateTime.Today);
    
    
    
    
    public FreshToGoManifest() { }
    
    public FreshToGoManifest(List<FreshToGoOrder> orders)
    {
        m_Orders = orders;
        m_totalOrders = orders.Count;
        m_totalCrates = orders.Sum(order => order.CrateQuantity);
        if (orders.Count > 0)
        {
            m_manifestDate = orders[0].OrderDate; // Assuming all orders have the same date
        }
    }
    
    public void AddOrder(FreshToGoOrder order)
    {
        m_Orders.Add(order);
    }
}