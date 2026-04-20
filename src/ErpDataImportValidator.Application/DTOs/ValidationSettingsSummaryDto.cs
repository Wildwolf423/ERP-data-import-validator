using ErpDataImportValidator.Domain.Enums;

namespace ErpDataImportValidator.Application.DTOs;

public sealed record ValidationSettingsSummaryDto(
    ValidationMode Mode,
    IReadOnlyCollection<string> AllowedCurrencies,
    IReadOnlyCollection<string> ValidDocumentTypes,
    IReadOnlyCollection<string> ValidLocations,
    string SupplierIdRegex,
    decimal SuspiciousAmountWarningThreshold);
