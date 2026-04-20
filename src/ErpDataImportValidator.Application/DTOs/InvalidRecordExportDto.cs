namespace ErpDataImportValidator.Application.DTOs;

public sealed record InvalidRecordExportDto(
    string FileName,
    string ContentType,
    string Content);
