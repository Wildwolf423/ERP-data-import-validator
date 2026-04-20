using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using ErpDataImportValidator.Application.Abstractions;
using ErpDataImportValidator.Domain.Enums;
using ErpDataImportValidator.Domain.Models;
using Microsoft.Extensions.Logging;

namespace ErpDataImportValidator.Infrastructure.Parsing;

public sealed class XmlImportParser : IImportParser
{
    private static readonly string[] RequiredNodes =
    [
        "DocumentId",
        "SupplierId",
        "SupplierName",
        "DocumentDate",
        "Amount",
        "Currency",
        "Location",
        "DocumentType"
    ];

    private readonly ILogger<XmlImportParser> _logger;

    public XmlImportParser(ILogger<XmlImportParser> logger)
    {
        _logger = logger;
    }

    public ImportFileType SupportedFileType => ImportFileType.Xml;

    public async Task<ImportParseResult> ParseAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        var records = new List<ImportDocumentRecord>();
        var messages = new List<ValidationMessage>();

        XDocument document;

        try
        {
            document = await XDocument.LoadAsync(stream, LoadOptions.SetLineInfo, cancellationToken);
        }
        catch (XmlException exception)
        {
            messages.Add(new ValidationMessage(
                null,
                null,
                null,
                ValidationSeverity.Error,
                ValidationCategory.Structure,
                "MALFORMED_XML",
                $"The XML file could not be read because the document is malformed: {exception.Message}"));

            return new ImportParseResult(SupportedFileType, records, messages);
        }

        if (document.Root?.Name != "ImportBatch")
        {
            messages.Add(new ValidationMessage(
                null,
                null,
                null,
                ValidationSeverity.Error,
                ValidationCategory.Structure,
                "INVALID_XML_ROOT",
                "The XML file must use 'ImportBatch' as the root element."));

            return new ImportParseResult(SupportedFileType, records, messages);
        }

        var elements = document.Root.Elements("Document").ToArray();

        if (elements.Length == 0)
        {
            messages.Add(new ValidationMessage(
                null,
                null,
                null,
                ValidationSeverity.Error,
                ValidationCategory.Structure,
                "NO_DOCUMENT_NODES",
                "The XML file does not contain any 'Document' elements to validate."));

            return new ImportParseResult(SupportedFileType, records, messages);
        }

        for (var index = 0; index < elements.Length; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var element = elements[index];
            var recordNumber = index + 1;
            var sourceReference = $"Document[{recordNumber}]";

            foreach (var nodeName in RequiredNodes)
            {
                if (element.Element(nodeName) is null)
                {
                    messages.Add(new ValidationMessage(
                        recordNumber,
                        Normalize(element.Element("DocumentId")?.Value),
                        ToFieldName(nodeName),
                        ValidationSeverity.Error,
                        ValidationCategory.Structure,
                        "MISSING_REQUIRED_NODE",
                        $"Required XML element '{nodeName}' is missing at {sourceReference}."));
                }
            }

            var rawDocumentId = element.Element("DocumentId")?.Value;
            var rawDocumentDate = element.Element("DocumentDate")?.Value;
            var rawAmount = element.Element("Amount")?.Value;

            records.Add(new ImportDocumentRecord(
                recordNumber,
                sourceReference,
                Normalize(rawDocumentId),
                Normalize(element.Element("SupplierId")?.Value),
                Normalize(element.Element("SupplierName")?.Value),
                ParseDateOnly(rawDocumentDate, recordNumber, rawDocumentId, sourceReference, messages),
                ParseDecimal(rawAmount, recordNumber, rawDocumentId, sourceReference, messages),
                Normalize(element.Element("Currency")?.Value),
                Normalize(element.Element("Location")?.Value),
                Normalize(element.Element("DocumentType")?.Value)));
        }

        _logger.LogInformation("Parsed {RecordCount} XML records with {MessageCount} structural messages.", records.Count, messages.Count);

        return new ImportParseResult(SupportedFileType, records, messages);
    }

    private static DateOnly? ParseDateOnly(
        string? rawValue,
        int recordNumber,
        string? documentId,
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
            "documentDate",
            ValidationSeverity.Error,
            ValidationCategory.Structure,
            "INVALID_DATE_FORMAT",
            $"The value '{rawValue}' in element 'DocumentDate' at {sourceReference} is not a valid date."));

        return null;
    }

    private static decimal? ParseDecimal(
        string? rawValue,
        int recordNumber,
        string? documentId,
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
            "amount",
            ValidationSeverity.Error,
            ValidationCategory.Structure,
            "INVALID_DECIMAL_FORMAT",
            $"The value '{rawValue}' in element 'Amount' at {sourceReference} is not a valid amount."));

        return null;
    }

    private static string ToFieldName(string nodeName)
    {
        return nodeName switch
        {
            "DocumentId" => "documentId",
            "SupplierId" => "supplierId",
            "SupplierName" => "supplierName",
            "DocumentDate" => "documentDate",
            "Amount" => "amount",
            "Currency" => "currency",
            "Location" => "location",
            "DocumentType" => "documentType",
            _ => nodeName
        };
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
