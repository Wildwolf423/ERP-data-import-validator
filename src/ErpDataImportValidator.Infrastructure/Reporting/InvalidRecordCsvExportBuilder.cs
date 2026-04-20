using System.Text;
using ErpDataImportValidator.Application.Abstractions;
using ErpDataImportValidator.Application.DTOs;
using ErpDataImportValidator.Domain.Models;

namespace ErpDataImportValidator.Infrastructure.Reporting;

public sealed class InvalidRecordCsvExportBuilder : IInvalidRecordExportBuilder
{
    public InvalidRecordExportDto Build(string sourceFileName, IReadOnlyCollection<RecordValidationResult> rejectedRecords)
    {
        var builder = new StringBuilder();
        builder.AppendLine("recordNumber,sourceReference,documentId,supplierId,supplierName,documentDate,amount,currency,location,documentType,errorMessages");

        foreach (var rejectedRecord in rejectedRecords)
        {
            var errorMessage = string.Join(
                " | ",
                rejectedRecord.Messages
                    .Where(message => message.Severity == Domain.Enums.ValidationSeverity.Error)
                    .Select(message => message.Message));

            builder.AppendLine(string.Join(",",
                Escape(rejectedRecord.Record.RecordNumber.ToString()),
                Escape(rejectedRecord.Record.SourceReference),
                Escape(rejectedRecord.Record.DocumentId),
                Escape(rejectedRecord.Record.SupplierId),
                Escape(rejectedRecord.Record.SupplierName),
                Escape(rejectedRecord.Record.DocumentDate?.ToString("yyyy-MM-dd")),
                Escape(rejectedRecord.Record.Amount?.ToString("F2")),
                Escape(rejectedRecord.Record.Currency),
                Escape(rejectedRecord.Record.Location),
                Escape(rejectedRecord.Record.DocumentType),
                Escape(errorMessage)));
        }

        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(sourceFileName);

        return new InvalidRecordExportDto(
            $"{fileNameWithoutExtension}-invalid-records.csv",
            "text/csv",
            builder.ToString());
    }

    private static string Escape(string? value)
    {
        if (value is null)
        {
            return string.Empty;
        }

        var escaped = value.Replace("\"", "\"\"");
        return $"\"{escaped}\"";
    }
}
