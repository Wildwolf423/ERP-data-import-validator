namespace ErpDataImportValidator.Domain.Settings;

public sealed class LocationDocumentTypeRule
{
    public string Location { get; set; } = string.Empty;

    public List<string> AllowedDocumentTypes { get; set; } = [];
}
