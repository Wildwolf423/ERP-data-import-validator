using ErpDataImportValidator.Domain.Enums;

namespace ErpDataImportValidator.Application.DTOs;

public sealed record SampleFileDto(
    string FileName,
    ImportFileType FileType,
    string Description,
    string RelativePath);
