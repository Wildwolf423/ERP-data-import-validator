using ErpDataImportValidator.Application.DTOs;
using ErpDataImportValidator.Domain.Models;
using ErpDataImportValidator.Domain.Settings;

namespace ErpDataImportValidator.Application.Extensions;

public static class ValidationMappings
{
    public static ValidationMessageDto ToDto(this ValidationMessage message)
    {
        return new ValidationMessageDto(
            message.RecordNumber,
            message.DocumentId,
            message.FieldName,
            message.Severity,
            message.Category,
            message.Code,
            message.Message);
    }

    public static ValidatedRecordDto ToDto(this RecordValidationResult result)
    {
        return new ValidatedRecordDto(
            result.Record.RecordNumber,
            result.Record.SourceReference,
            result.Record.DocumentId,
            result.Record.SupplierId,
            result.Record.SupplierName,
            result.Record.DocumentDate,
            result.Record.Amount,
            result.Record.Currency,
            result.Record.Location,
            result.Record.DocumentType,
            !result.HasErrors,
            result.Messages.Select(message => message.ToDto()).ToArray());
    }

    public static CategorySummaryDto ToDto(this CategorySummaryItem item)
    {
        return new CategorySummaryDto(item.Category, item.ErrorCount, item.WarningCount);
    }

    public static RecordPreviewDto ToPreviewDto(this RecordValidationResult result)
    {
        return new RecordPreviewDto(
            result.Record.RecordNumber,
            result.Record.SourceReference,
            result.Record.DocumentId,
            result.Record.SupplierId,
            result.Record.SupplierName,
            result.Record.DocumentDate,
            result.Record.Amount,
            result.Record.Currency,
            result.Record.Location,
            result.Record.DocumentType,
            !result.HasErrors,
            result.Messages.Count(message => message.Severity == Domain.Enums.ValidationSeverity.Warning));
    }

    public static ValidationSettingsSummaryDto ToSummaryDto(this ValidationSettings settings)
    {
        return new ValidationSettingsSummaryDto(
            settings.Mode,
            settings.AllowedCurrencies.ToArray(),
            settings.ValidDocumentTypes.ToArray(),
            settings.ValidLocations.ToArray(),
            settings.SupplierIdRegex,
            settings.SuspiciousAmountWarningThreshold);
    }
}
