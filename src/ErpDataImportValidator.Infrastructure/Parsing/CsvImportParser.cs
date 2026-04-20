using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using ErpDataImportValidator.Application.Abstractions;
using ErpDataImportValidator.Domain.Enums;
using ErpDataImportValidator.Domain.Models;
using Microsoft.Extensions.Logging;

namespace ErpDataImportValidator.Infrastructure.Parsing;

public sealed class CsvImportParser : IImportParser
{
    private static readonly string[] RequiredColumns =
    [
        "documentId",
        "supplierId",
        "supplierName",
        "documentDate",
        "amount",
        "currency",
        "location",
        "documentType"
    ];

    private readonly ILogger<CsvImportParser> _logger;

    public CsvImportParser(ILogger<CsvImportParser> logger)
    {
        _logger = logger;
    }

    public ImportFileType SupportedFileType => ImportFileType.Csv;

    public async Task<ImportParseResult> ParseAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        var records = new List<ImportDocumentRecord>();
        var messages = new List<ValidationMessage>();

        using var reader = new StreamReader(stream, leaveOpen: true);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            IgnoreBlankLines = false,
            TrimOptions = TrimOptions.Trim,
            HeaderValidated = null,
            MissingFieldFound = null,
            BadDataFound = null
        });

        if (!await csv.ReadAsync())
        {
            messages.Add(CreateBatchMessage(
                "EMPTY_CSV_FILE",
                "The uploaded CSV file is empty. Provide a header row and at least one business record."));

            return new ImportParseResult(SupportedFileType, records, messages);
        }

        csv.ReadHeader();
        var headerRecord = csv.HeaderRecord ?? [];

        var missingColumns = RequiredColumns
            .Where(column => !headerRecord.Contains(column, StringComparer.OrdinalIgnoreCase))
            .ToArray();

        if (missingColumns.Length > 0)
        {
            messages.Add(CreateBatchMessage(
                "MISSING_REQUIRED_COLUMNS",
                $"The CSV file cannot be validated because these required columns are missing: {string.Join(", ", missingColumns)}."));

            return new ImportParseResult(SupportedFileType, records, messages);
        }

        while (await csv.ReadAsync())
        {
            cancellationToken.ThrowIfCancellationRequested();

            var sourceReference = $"Row {csv.Parser.Row}";
            var recordNumber = records.Count + 1;

            var rawValues = RequiredColumns.ToDictionary(
                column => column,
                column => csv.GetField(column),
                StringComparer.OrdinalIgnoreCase);

            if (rawValues.Values.All(value => string.IsNullOrWhiteSpace(value)))
            {
                messages.Add(new ValidationMessage(
                    null,
                    null,
                    null,
                    ValidationSeverity.Warning,
                    ValidationCategory.Structure,
                    "EMPTY_ROW_SKIPPED",
                    $"An empty row was found at {sourceReference} and was ignored during validation."));

                continue;
            }

            var documentDate = ParseDateOnly(rawValues["documentDate"], recordNumber, rawValues["documentId"], "documentDate", sourceReference, messages);
            var amount = ParseDecimal(rawValues["amount"], recordNumber, rawValues["documentId"], "amount", sourceReference, messages);

            records.Add(new ImportDocumentRecord(
                recordNumber,
                sourceReference,
                Normalize(rawValues["documentId"]),
                Normalize(rawValues["supplierId"]),
                Normalize(rawValues["supplierName"]),
                documentDate,
                amount,
                Normalize(rawValues["currency"]),
                Normalize(rawValues["location"]),
                Normalize(rawValues["documentType"])));
        }

        _logger.LogInformation("Parsed {RecordCount} CSV records with {MessageCount} structural messages.", records.Count, messages.Count);

        return new ImportParseResult(SupportedFileType, records, messages);
    }

    private static DateOnly? ParseDateOnly(
        string? rawValue,
        int recordNumber,
        string? documentId,
        string fieldName,
        string sourceReference,
        ICollection<ValidationMessage> messages)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return null;
        }

        if (DateOnly.TryParse(rawValue, CultureInfo.InvariantCulture, DateTimeStyles.None, out var value))
        {
            return value;
        }

        messages.Add(new ValidationMessage(
            recordNumber,
            Normalize(documentId),
            fieldName,
            ValidationSeverity.Error,
            ValidationCategory.Structure,
            "INVALID_DATE_FORMAT",
            $"The value '{rawValue}' in field '{fieldName}' at {sourceReference} is not a valid document date."));

        return null;
    }

    private static decimal? ParseDecimal(
        string? rawValue,
        int recordNumber,
        string? documentId,
        string fieldName,
        string sourceReference,
        ICollection<ValidationMessage> messages)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return null;
        }

        if (decimal.TryParse(rawValue, NumberStyles.Number, CultureInfo.InvariantCulture, out var value))
        {
            return value;
        }

        messages.Add(new ValidationMessage(
            recordNumber,
            Normalize(documentId),
            fieldName,
            ValidationSeverity.Error,
            ValidationCategory.Structure,
            "INVALID_DECIMAL_FORMAT",
            $"The value '{rawValue}' in field '{fieldName}' at {sourceReference} is not a valid amount."));

        return null;
    }

    private static ValidationMessage CreateBatchMessage(string code, string message)
    {
        return new ValidationMessage(
            null,
            null,
            null,
            ValidationSeverity.Error,
            ValidationCategory.Structure,
            code,
            message);
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
