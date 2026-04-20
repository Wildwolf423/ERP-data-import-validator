using ErpDataImportValidator.Domain.Enums;

namespace ErpDataImportValidator.Domain.Models;

public sealed record ImportParseResult(
    ImportFileType FileType,
    IReadOnlyCollection<ImportDocumentRecord> Records,
    IReadOnlyCollection<ValidationMessage> Messages);
