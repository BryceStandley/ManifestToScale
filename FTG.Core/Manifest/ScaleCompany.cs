using JetBrains.Annotations;

namespace FTG.Core.Manifest;

public class ScaleCompany
{
    public string Company { get; } = "PER-CO-FTG";
    public string VendorNumber { get; } = "853540";
    public string VendorName { get; } = "FRESH TO GO FOODS-853540";
    public string VendorReceiptPrefix { get; } = "FTG/";
    
    public static readonly ScaleCompany AzuraFresh = new("PER-CO-CAF", "856946", "Azura Fresh WA Pty Ltd", "CAF/");
    [UsedImplicitly] public static readonly ScaleCompany FreshToGo = new ("PER-CO-FTG", "853540", "FRESH TO GO FOODS-853540", "FTG/");

    public ScaleCompany() { }

    private ScaleCompany(string company, string vendorNumber, string vendorName, string vendorReceiptPrefix)
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