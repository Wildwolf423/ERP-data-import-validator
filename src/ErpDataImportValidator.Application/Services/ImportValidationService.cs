using System.Text.RegularExpressions;
using ErpDataImportValidator.Application.Abstractions;
using ErpDataImportValidator.Application.DTOs;
using ErpDataImportValidator.Application.Extensions;
using ErpDataImportValidator.Domain.Enums;
using ErpDataImportValidator.Domain.Models;
using ErpDataImportValidator.Domain.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ErpDataImportValidator.Application.Services;

public sealed class ImportValidationService : IImportValidationService
{
    private readonly IReadOnlyDictionary<ImportFileType, IImportParser> _parsers;
    private readonly IMarkdownValidationReportBuilder _markdownValidationReportBuilder;
    private readonly IInvalidRecordExportBuilder _invalidRecordExportBuilder;
    private readonly IOptions<ValidationSettings> _validationSettings;
    private readonly ILogger<ImportValidationService> _logger;

    public ImportValidationService(
        IEnumerable<IImportParser> parsers,
        IMarkdownValidationReportBuilder markdownValidationReportBuilder,
        IInvalidRecordExportBuilder invalidRecordExportBuilder,
        IOptions<ValidationSettings> validationSettings,
        ILogger<ImportValidationService> logger)
    {
        _parsers = parsers.ToDictionary(parser => parser.SupportedFileType);
        _markdownValidationReportBuilder = markdownValidationReportBuilder;
        _invalidRecordExportBuilder = invalidRecordExportBuilder;
        _validationSettings = validationSettings;
        _logger = logger;
    }

    public async Task<ImportValidationResponseDto> ValidateAsync(
        Stream stream,
        string fileName,
        ImportFileType fileType,
        ImportValidationExecutionOptions options,
        CancellationToken cancellationToken = default)
    {
        if (!_parsers.TryGetValue(fileType, out var parser))
        {
            throw new InvalidOperationException($"No parser is registered for file type '{fileType}'.");
        }

        var activeSettings = _validationSettings.Value;
        var activeMode = options.Mode ?? activeSettings.Mode;

        _logger.LogInformation(
            "Validating import file {FileName} as {FileType} with mode {Mode}.",
            fileName,
            fileType,
            activeMode);

        var parseResult = await parser.ParseAsync(stream, cancellationToken);
        var batchMessages = parseResult.Messages
            .Where(message => !message.RecordNumber.HasValue)
            .ToList();

        var seedMessagesByRecord = parseResult.Messages
            .Where(message => message.RecordNumber.HasValue)
            .GroupBy(message => message.RecordNumber!.Value)
            .ToDictionary(group => group.Key, group => group.ToList());

        var duplicateDocumentIds = parseResult.Records
            .Where(record => !string.IsNullOrWhiteSpace(record.DocumentId))
            .GroupBy(record => record.DocumentId!, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .SelectMany(group => group.Select(record => record.RecordNumber))
            .ToHashSet();

        var recordResults = new List<RecordValidationResult>();

        foreach (var record in parseResult.Records.OrderBy(item => item.RecordNumber))
        {
            var messages = seedMessagesByRecord.TryGetValue(record.RecordNumber, out var seededMessages)
                ? seededMessages
                : [];

            ValidateRequiredFields(record, messages);
            ValidateBusinessRules(record, messages, duplicateDocumentIds, activeSettings, activeMode);

            recordResults.Add(new RecordValidationResult(
                record,
                messages
                    .OrderBy(message => message.RecordNumber ?? 0)
                    .ThenBy(message => message.FieldName)
                    .ThenBy(message => message.Code)
                    .ToArray()));
        }

        var allMessages = batchMessages
            .Concat(recordResults.SelectMany(result => result.Messages))
            .ToArray();

        var report = new ValidationReport(
            fileType,
            parseResult.Records.Count,
            recordResults.Count(result => !result.HasErrors),
            recordResults.Count(result => result.HasErrors),
            allMessages.Count(message => message.Severity == ValidationSeverity.Warning),
            batchMessages.ToArray(),
            recordResults.ToArray(),
            BuildCategorySummary(allMessages));

        var previewLimit = Math.Clamp(options.PreviewLimit, 0, 50);
        var response = new ImportValidationResponseDto(
            fileName,
            report.FileType,
            report.TotalRecords,
            report.AcceptedRecords,
            report.RejectedRecords,
            report.WarningCount,
            report.BatchMessages.Select(message => message.ToDto()).ToArray(),
            report.RecordResults.Select(result => result.ToDto()).ToArray(),
            report.SummaryByCategory.Select(item => item.ToDto()).ToArray(),
            options.IncludePreview
                ? report.RecordResults.Take(previewLimit).Select(result => result.ToPreviewDto()).ToArray()
                : [],
            _markdownValidationReportBuilder.Build(fileName, report),
            report.RejectedRecords > 0
                ? _invalidRecordExportBuilder.Build(fileName, report.RecordResults.Where(result => result.HasErrors).ToArray())
                : null);

        _logger.LogInformation(
            "Validation completed for {FileName}. TotalRecords={TotalRecords}, AcceptedRecords={AcceptedRecords}, RejectedRecords={RejectedRecords}, WarningCount={WarningCount}",
            fileName,
            response.TotalRecords,
            response.AcceptedRecords,
            response.RejectedRecords,
            response.WarningCount);

        return response;
    }

    private static IReadOnlyCollection<CategorySummaryItem> BuildCategorySummary(IEnumerable<ValidationMessage> messages)
    {
        return messages
            .GroupBy(message => message.Category)
            .Select(group => new CategorySummaryItem(
                group.Key,
                group.Count(message => message.Severity == ValidationSeverity.Error),
                group.Count(message => message.Severity == ValidationSeverity.Warning)))
            .OrderBy(item => item.Category)
            .ToArray();
    }

    private static void ValidateRequiredFields(ImportDocumentRecord record, ICollection<ValidationMessage> messages)
    {
        ValidateRequiredValue(record, messages, "documentId", record.DocumentId);
        ValidateRequiredValue(record, messages, "supplierId", record.SupplierId);
        ValidateRequiredValue(record, messages, "supplierName", record.SupplierName);
        ValidateRequiredValue(record, messages, "currency", record.Currency);
        ValidateRequiredValue(record, messages, "location", record.Location);
        ValidateRequiredValue(record, messages, "documentType", record.DocumentType);

        if (record.DocumentDate is null && !HasFieldMessage(messages, "documentDate"))
        {
            messages.Add(CreateMessage(
                record,
                "documentDate",
                ValidationSeverity.Error,
                ValidationCategory.RequiredField,
                "REQUIRED_DOCUMENT_DATE",
                "Document date is required before the record can be imported."));
        }

        if (record.Amount is null && !HasFieldMessage(messages, "amount"))
        {
            messages.Add(CreateMessage(
                record,
                "amount",
                ValidationSeverity.Error,
                ValidationCategory.RequiredField,
                "REQUIRED_AMOUNT",
                "Amount is required before the record can be imported."));
        }
    }

    private static void ValidateBusinessRules(
        ImportDocumentRecord record,
        ICollection<ValidationMessage> messages,
        ISet<int> duplicateDocumentIds,
        ValidationSettings settings,
        ValidationMode mode)
    {
        if (record.Amount.HasValue && record.Amount <= 0)
        {
            messages.Add(CreateMessage(
                record,
                "amount",
                ValidationSeverity.Error,
                ValidationCategory.BusinessRule,
                "AMOUNT_NOT_POSITIVE",
                "Amount must be greater than zero for ERP import processing."));
        }

        if (!string.IsNullOrWhiteSpace(record.Currency) &&
            !settings.AllowedCurrencies.Contains(record.Currency, StringComparer.OrdinalIgnoreCase))
        {
            messages.Add(CreateMessage(
                record,
                "currency",
                ValidationSeverity.Error,
                ValidationCategory.BusinessRule,
                "UNSUPPORTED_CURRENCY",
                $"Currency '{record.Currency}' is not configured as an allowed import currency."));
        }

        if (!string.IsNullOrWhiteSpace(record.Location) &&
            !settings.ValidLocations.Contains(record.Location, StringComparer.OrdinalIgnoreCase))
        {
            messages.Add(CreateMessage(
                record,
                "location",
                ValidationSeverity.Error,
                ValidationCategory.BusinessRule,
                "INVALID_LOCATION",
                $"Location '{record.Location}' is not configured as a valid ERP import location."));
        }

        if (!string.IsNullOrWhiteSpace(record.DocumentType) &&
            !settings.ValidDocumentTypes.Contains(record.DocumentType, StringComparer.OrdinalIgnoreCase))
        {
            messages.Add(CreateMessage(
                record,
                "documentType",
                ValidationSeverity.Error,
                ValidationCategory.BusinessRule,
                "INVALID_DOCUMENT_TYPE",
                $"Document type '{record.DocumentType}' is not configured as a valid ERP import document type."));
        }

        if (record.DocumentDate.HasValue && record.DocumentDate > DateOnly.FromDateTime(DateTime.UtcNow))
        {
            messages.Add(CreateMessage(
                record,
                "documentDate",
                ValidationSeverity.Error,
                ValidationCategory.BusinessRule,
                "FUTURE_DOCUMENT_DATE",
                "Document date cannot be later than today's date."));
        }

        if (!string.IsNullOrWhiteSpace(record.SupplierId) &&
            !Regex.IsMatch(record.SupplierId, settings.SupplierIdRegex, RegexOptions.CultureInvariant))
        {
            messages.Add(CreateMessage(
                record,
                "supplierId",
                ValidationSeverity.Error,
                ValidationCategory.BusinessRule,
                "INVALID_SUPPLIER_ID_FORMAT",
                $"Supplier ID '{record.SupplierId}' does not match the expected ERP format '{settings.SupplierIdRegex}'."));
        }

        if (duplicateDocumentIds.Contains(record.RecordNumber))
        {
            messages.Add(CreateMessage(
                record,
                "documentId",
                ValidationSeverity.Error,
                ValidationCategory.BusinessRule,
                "DUPLICATE_DOCUMENT_ID",
                $"Document ID '{record.DocumentId}' appears more than once in the same import batch."));
        }

        if (!string.IsNullOrWhiteSpace(record.Location) &&
            !string.IsNullOrWhiteSpace(record.DocumentType))
        {
            var rule = settings.LocationDocumentTypeRules
                .FirstOrDefault(item => string.Equals(item.Location, record.Location, StringComparison.OrdinalIgnoreCase));

            if (rule is not null &&
                !rule.AllowedDocumentTypes.Contains(record.DocumentType, StringComparer.OrdinalIgnoreCase))
            {
                messages.Add(CreateMessage(
                    record,
                    "documentType",
                    mode == ValidationMode.Strict ? ValidationSeverity.Error : ValidationSeverity.Warning,
                    ValidationCategory.BusinessRule,
                    "INVALID_LOCATION_DOCUMENTTYPE_COMBINATION",
                    $"Document type '{record.DocumentType}' is not allowed for location '{record.Location}' under the current import configuration."));
            }
        }

        if (record.Amount.HasValue &&
            record.Amount >= settings.SuspiciousAmountWarningThreshold)
        {
            messages.Add(CreateMessage(
                record,
                "amount",
                ValidationSeverity.Warning,
                ValidationCategory.BusinessRule,
                "SUSPICIOUS_HIGH_AMOUNT",
                $"Amount {record.Amount.Value:F2} exceeds the configured review threshold and should be checked before import."));
        }
    }

    private static void ValidateRequiredValue(
        ImportDocumentRecord record,
        ICollection<ValidationMessage> messages,
        string fieldName,
        string? value)
    {
        if (string.IsNullOrWhiteSpace(value) && !HasFieldMessage(messages, fieldName))
        {
            messages.Add(CreateMessage(
                record,
                fieldName,
                ValidationSeverity.Error,
                ValidationCategory.RequiredField,
                $"REQUIRED_{fieldName.ToUpperInvariant()}",
                $"{ToDisplayName(fieldName)} is required."));
        }
    }

    private static bool HasFieldMessage(IEnumerable<ValidationMessage> messages, string fieldName)
    {
        return messages.Any(message =>
            string.Equals(message.FieldName, fieldName, StringComparison.OrdinalIgnoreCase) &&
            message.Severity == ValidationSeverity.Error);
    }

    private static ValidationMessage CreateMessage(
        ImportDocumentRecord record,
        string fieldName,
        ValidationSeverity severity,
        ValidationCategory category,
        string code,
        string message)
    {
        return new ValidationMessage(
            record.RecordNumber,
            record.DocumentId,
            fieldName,
            severity,
            category,
            code,
            message);
    }

    private static string ToDisplayName(string fieldName)
    {
        return fieldName switch
        {
            "documentId" => "Document ID",
            "supplierId" => "Supplier ID",
            "supplierName" => "Supplier name",
            "documentDate" => "Document date",
            "amount" => "Amount",
            "currency" => "Currency",
            "location" => "Location",
            "documentType" => "Document type",
            _ => fieldName
        };
    }
}
