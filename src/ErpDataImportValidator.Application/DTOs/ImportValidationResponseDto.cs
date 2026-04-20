using ErpDataImportValidator.Domain.Enums;

namespace ErpDataImportValidator.Application.DTOs;

public sealed record ImportValidationResponseDto(
    string FileName,
    ImportFileType FileType,
    int TotalRecords,
    int AcceptedRecords,
    int RejectedRecords,
    int WarningCount,
    IReadOnlyCollection<ValidationMessageDto> BatchMessages,
    IReadOnlyCollection<ValidatedRecordDto> RecordResults,
    IReadOnlyCollection<CategorySummaryDto> SummaryByCategory,
    IReadOnlyCollection<RecordPreviewDto> PreviewRecords,
    string MarkdownSummaryReport,
    InvalidRecordExportDto? InvalidRecordExport);
