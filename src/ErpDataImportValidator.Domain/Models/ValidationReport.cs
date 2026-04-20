using ErpDataImportValidator.Domain.Enums;

namespace ErpDataImportValidator.Domain.Models;

public sealed record ValidationReport(
    ImportFileType FileType,
    int TotalRecords,
    int AcceptedRecords,
    int RejectedRecords,
    int WarningCount,
    IReadOnlyCollection<ValidationMessage> BatchMessages,
    IReadOnlyCollection<RecordValidationResult> RecordResults,
    IReadOnlyCollection<CategorySummaryItem> SummaryByCategory);
