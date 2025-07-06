namespace FTG.Core.Manifest;

public class ScaleCompany
{
    public string Company { get; set; } = "PER-CO-FTG";
    public string VendorNumber { get; set; } = "853540";
    public string VendorName { get; set; } = "FRESH TO GO FOODS-853540";
    public string VendorReceiptPrefix { get; set; } = "FTG/";
    
    public static ScaleCompany AzuraFresh = new ScaleCompany("PER-CO-CAF", "954111", "Azura Fresh NSW P/L", "CAF/");
    public static ScaleCompany FreshToGo = new ScaleCompany("PER-CO-FTG", "853540", "FRESH TO GO FOODS-853540", "FTG/");

    public ScaleCompany() { }
    public ScaleCompany(string company, string vendorNumber, string vendorName, string vendorReceiptPrefix)
    {
        Company = company;
        VendorNumber = vendorNumber;
        VendorName = vendorName;
        VendorReceiptPrefix = vendorReceiptPrefix;
    }
    public static implicit operator string(ScaleCompany company)
    {
        return company.Company;
    }
    
}