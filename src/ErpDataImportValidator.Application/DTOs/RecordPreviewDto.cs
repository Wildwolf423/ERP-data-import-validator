namespace ErpDataImportValidator.Application.DTOs;

public sealed record RecordPreviewDto(
    int RecordNumber,
    string SourceReference,
    string? DocumentId,
    string? SupplierId,
    string? SupplierName,
    DateOnly? DocumentDate,
    decimal? Amount,
    string? Currency,
    string? Location,
    string? DocumentType,
    bool IsAccepted,
    int WarningCount);
