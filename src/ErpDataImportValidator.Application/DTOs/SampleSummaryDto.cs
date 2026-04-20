namespace ErpDataImportValidator.Application.DTOs;

public sealed record SampleSummaryDto(
    IReadOnlyCollection<SampleFileDto> SampleFiles,
    ValidationSettingsSummaryDto ValidationSettings);
