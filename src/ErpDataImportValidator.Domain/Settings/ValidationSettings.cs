using ErpDataImportValidator.Domain.Enums;

namespace ErpDataImportValidator.Domain.Settings;

public sealed class ValidationSettings
{
    public ValidationMode Mode { get; set; } = ValidationMode.Strict;

    public List<string> AllowedCurrencies { get; set; } = ["EUR", "USD", "GBP", "HUF"];

    public List<string> ValidDocumentTypes { get; set; } = ["SupplierInvoice", "CreditMemo", "VendorMaster"];

    public List<string> ValidLocations { get; set; } = ["BUD", "VIE", "PRG", "WAR"];

    public string SupplierIdRegex { get; set; } = "^SUP-[0-9]{4}$";

    public decimal SuspiciousAmountWarningThreshold { get; set; } = 25000m;

    public List<LocationDocumentTypeRule> LocationDocumentTypeRules { get; set; } = [];
}
