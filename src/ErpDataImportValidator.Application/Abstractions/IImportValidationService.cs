using ErpDataImportValidator.Application.DTOs;
using ErpDataImportValidator.Domain.Enums;

namespace ErpDataImportValidator.Application.Abstractions;

public interface IImportValidationService
{
    Task<ImportValidationResponseDto> ValidateAsync(
        Stream stream,
        string fileName,
        ImportFileType fileType,
        ImportValidationExecutionOptions options,
        CancellationToken cancellationToken = default);
}
