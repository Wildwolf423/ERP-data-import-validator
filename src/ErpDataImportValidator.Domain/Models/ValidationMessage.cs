using ErpDataImportValidator.Domain.Enums;

namespace ErpDataImportValidator.Domain.Models;

public sealed record ValidationMessage(
    int? RecordNumber,
    string? DocumentId,
    string? FieldName,
    ValidationSeverity Severity,
    ValidationCategory Category,
    string Code,
    string Message);
