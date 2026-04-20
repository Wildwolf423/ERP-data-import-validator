using System.Text;
using ErpDataImportValidator.Application.Abstractions;
using ErpDataImportValidator.Application.DTOs;
using ErpDataImportValidator.Application.Services;
using ErpDataImportValidator.Domain.Enums;
using ErpDataImportValidator.Domain.Settings;
using ErpDataImportValidator.Infrastructure.Parsing;
using ErpDataImportValidator.Infrastructure.Reporting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ErpDataImportValidator.UnitTests;

public sealed class ImportValidationTests
{
    [Fact]
    public async Task CsvStructureValidation_DetectsMissingRequiredColumns()
    {
        var parser = new CsvImportParser(NullLogger<CsvImportParser>.Instance);
        await using var stream = CreateStream("""
            documentId,supplierId,documentDate,amount,currency,location,documentType
            INV-2026-1001,SUP-1001,2026-04-10,100.00,EUR,BUD,SupplierInvoice
            """);

        var result = await parser.ParseAsync(stream);

        Assert.Empty(result.Records);
        Assert.Contains(result.Messages, message => message.Code == "MISSING_REQUIRED_COLUMNS");
    }

    [Fact]
    public async Task XmlStructureValidation_DetectsMalformedXml()
    {
        var parser = new XmlImportParser(NullLogger<XmlImportParser>.Instance);
        await using var stream = CreateStream("""
            <ImportBatch>
              <Document>
                <DocumentId>INV-2026-1001</DocumentId>
            </ImportBatch
            """);

        var result = await parser.ParseAsync(stream);

        Assert.Empty(result.Records);
        Assert.Contains(result.Messages, message => message.Code == "MALFORMED_XML");
    }

    [Fact]
    public async Task DuplicateDetection_RejectsBothRows()
    {
        var service = CreateService();
        await using var stream = CreateStream("""
            documentId,supplierId,supplierName,documentDate,amount,currency,location,documentType
            INV-2026-2001,SUP-1001,Contoso Industrial Supplies,2026-04-10,1280.50,EUR,BUD,SupplierInvoice
            INV-2026-2001,SUP-1002,Fabrikam Logistics,2026-04-11,240.00,USD,VIE,CreditMemo
            """);

        var result = await service.ValidateAsync(stream, "duplicates.csv", ImportFileType.Csv, new ImportValidationExecutionOptions());

        Assert.Equal(2, result.TotalRecords);
        Assert.Equal(0, result.AcceptedRecords);
        Assert.Equal(2, result.RejectedRecords);
        Assert.All(result.RecordResults, record =>
            Assert.Contains(record.Messages, message => message.Code == "DUPLICATE_DOCUMENT_ID"));
    }

    [Fact]
    public async Task FutureDateValidation_RejectsRecord()
    {
        var service = CreateService();
        var futureDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3)).ToString("yyyy-MM-dd");

        await using var stream = CreateStream($"""
            documentId,supplierId,supplierName,documentDate,amount,currency,location,documentType
            INV-2026-2101,SUP-1001,Contoso Industrial Supplies,{futureDate},1280.50,EUR,BUD,SupplierInvoice
            """);

        var result = await service.ValidateAsync(stream, "future-date.csv", ImportFileType.Csv, new ImportValidationExecutionOptions());

        Assert.Equal(1, result.RejectedRecords);
        Assert.Contains(result.RecordResults.Single().Messages, message => message.Code == "FUTURE_DOCUMENT_DATE");
    }

    [Fact]
    public async Task AllowedCurrencyValidation_RejectsUnsupportedCurrency()
    {
        var service = CreateService();
        await using var stream = CreateStream("""
            documentId,supplierId,supplierName,documentDate,amount,currency,location,documentType
            INV-2026-2201,SUP-1001,Contoso Industrial Supplies,2026-04-10,1280.50,PLN,BUD,SupplierInvoice
            """);

        var result = await service.ValidateAsync(stream, "currency.csv", ImportFileType.Csv, new ImportValidationExecutionOptions());

        Assert.Equal(1, result.RejectedRecords);
        Assert.Contains(result.RecordResults.Single().Messages, message => message.Code == "UNSUPPORTED_CURRENCY");
    }

    [Fact]
    public async Task SupplierIdFormatValidation_RejectsInvalidSupplierPattern()
    {
        var service = CreateService();
        await using var stream = CreateStream("""
            documentId,supplierId,supplierName,documentDate,amount,currency,location,documentType
            INV-2026-2301,SUPPLIER-ABC,Contoso Industrial Supplies,2026-04-10,1280.50,EUR,BUD,SupplierInvoice
            """);

        var result = await service.ValidateAsync(stream, "supplier-id.csv", ImportFileType.Csv, new ImportValidationExecutionOptions());

        Assert.Equal(1, result.RejectedRecords);
        Assert.Contains(result.RecordResults.Single().Messages, message => message.Code == "INVALID_SUPPLIER_ID_FORMAT");
    }

    private static IImportValidationService CreateService()
    {
        var settings = new ValidationSettings
        {
            Mode = ValidationMode.Strict,
            AllowedCurrencies = ["EUR", "USD", "GBP", "HUF"],
            ValidDocumentTypes = ["SupplierInvoice", "CreditMemo", "VendorMaster"],
            ValidLocations = ["BUD", "VIE", "PRG", "WAR"],
            SupplierIdRegex = "^SUP-[0-9]{4}$",
            SuspiciousAmountWarningThreshold = 25000m,
            LocationDocumentTypeRules =
            [
                new LocationDocumentTypeRule
                {
                    Location = "BUD",
                    AllowedDocumentTypes = ["SupplierInvoice", "CreditMemo", "VendorMaster"]
                },
                new LocationDocumentTypeRule
                {
                    Location = "VIE",
                    AllowedDocumentTypes = ["SupplierInvoice", "CreditMemo"]
                },
                new LocationDocumentTypeRule
                {
                    Location = "PRG",
                    AllowedDocumentTypes = ["SupplierInvoice"]
                },
                new LocationDocumentTypeRule
                {
                    Location = "WAR",
                    AllowedDocumentTypes = ["SupplierInvoice", "VendorMaster"]
                }
            ]
        };

        return new ImportValidationService(
            [
                new CsvImportParser(NullLogger<CsvImportParser>.Instance),
                new XmlImportParser(NullLogger<XmlImportParser>.Instance)
            ],
            new MarkdownValidationReportBuilder(),
            new InvalidRecordCsvExportBuilder(),
            Options.Create(settings),
            NullLogger<ImportValidationService>.Instance);
    }

    private static MemoryStream CreateStream(string content)
    {
        return new MemoryStream(Encoding.UTF8.GetBytes(content));
    }
}
