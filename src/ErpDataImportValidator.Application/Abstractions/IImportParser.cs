using ErpDataImportValidator.Domain.Enums;
using ErpDataImportValidator.Domain.Models;

namespace ErpDataImportValidator.Application.Abstractions;

public interface IImportParser
{
    ImportFileType SupportedFileType { get; }

    Task<ImportParseResult> ParseAsync(Stream stream, CancellationToken cancellationToken = default);
}
