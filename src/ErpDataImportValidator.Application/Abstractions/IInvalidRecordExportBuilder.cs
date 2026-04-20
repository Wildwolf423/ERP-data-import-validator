using ErpDataImportValidator.Application.DTOs;
using ErpDataImportValidator.Domain.Models;

namespace ErpDataImportValidator.Application.Abstractions;

public interface IInvalidRecordExportBuilder
{
    InvalidRecordExportDto Build(string sourceFileName, IReadOnlyCollection<RecordValidationResult> rejectedRecords);
}
