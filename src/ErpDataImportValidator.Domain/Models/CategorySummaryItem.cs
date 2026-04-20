using ErpDataImportValidator.Domain.Enums;

namespace ErpDataImportValidator.Domain.Models;

public sealed record CategorySummaryItem(
    ValidationCategory Category,
    int ErrorCount,
    int WarningCount);
