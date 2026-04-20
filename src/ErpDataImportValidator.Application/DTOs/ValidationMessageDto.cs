using ErpDataImportValidator.Domain.Enums;

namespace ErpDataImportValidator.Application.DTOs;

public sealed record ValidationMessageDto(
    int? RecordNumber,
    string? DocumentId,
    string? FieldName,
    ValidationSeverity Severity,
    ValidationCategory Category,
    string Code,
    string Message);
