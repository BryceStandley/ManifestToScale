using JetBrains.Annotations;

namespace FTG.Core.Manifest;

public class ScaleCompany
{
    public string Company { get; } = "PER-CO-FTG";
    public string VendorNumber { get; } = "853540";
    public string VendorName { get; } = "FRESH TO GO FOODS-853540";
    public string VendorReceiptPrefix { get; } = "FTG/";

    public string VendorSkuNumber { get; } = "1111"; // 1111 for FTG and Azura and "2222 for Theme Group
    
    public static readonly ScaleCompany AzuraFresh = new("PER-CO-CAF", "856946", "AZURA FRESH WA PTY LTD", "CAF/", "1111");
    public static readonly ScaleCompany ThemeGroup = new("PER-CO-CAF", "222222", "THEME GROUP PTY LTD", "CTG/", "2222");
    [UsedImplicitly] public static readonly ScaleCompany FreshToGo = new ("PER-CO-FTG", "853540", "FRESH TO GO FOODS-853540", "FTG/", "1111");

    public ScaleCompany() { }

    private ScaleCompany(string company, string vendorNumber, string vendorName, string vendorReceiptPrefix, string vendorSkuNumber)
    {
        Company = company;
        VendorNumber = vendorNumber;
        VendorName = vendorName;
        VendorReceiptPrefix = vendorReceiptPrefix;
        VendorSkuNumber = vendorSkuNumber;
    }
    public static implicit operator string(ScaleCompany company)
    {
        return company.Company;
    }
    
}