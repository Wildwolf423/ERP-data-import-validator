using ErpDataImportValidator.Domain.Enums;

namespace ErpDataImportValidator.Application.DTOs;

public sealed record CategorySummaryDto(
    ValidationCategory Category,
    int ErrorCount,
    int WarningCount);
