namespace ErpDataImportValidator.Domain.Models;

public sealed record ImportDocumentRecord(
    int RecordNumber,
    string SourceReference,
    string? DocumentId,
    string? SupplierId,
    string? SupplierName,
    DateOnly? DocumentDate,
    decimal? Amount,
    string? Currency,
    string? Location,
    string? DocumentType);
