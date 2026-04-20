using ErpDataImportValidator.Domain.Enums;

namespace ErpDataImportValidator.Application.DTOs;

public sealed record ImportValidationExecutionOptions(
    bool IncludePreview = true,
    int PreviewLimit = 5,
    ValidationMode? Mode = null);
