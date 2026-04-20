using System.Text;
using ErpDataImportValidator.Application.Abstractions;
using ErpDataImportValidator.Domain.Enums;
using ErpDataImportValidator.Domain.Models;

namespace ErpDataImportValidator.Infrastructure.Reporting;

public sealed class MarkdownValidationReportBuilder : IMarkdownValidationReportBuilder
{
    public string Build(string fileName, ValidationReport report)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"# Validation Report: {fileName}");
        builder.AppendLine();
        builder.AppendLine($"- File type: `{report.FileType}`");
        builder.AppendLine($"- Total records: `{report.TotalRecords}`");
        builder.AppendLine($"- Accepted records: `{report.AcceptedRecords}`");
        builder.AppendLine($"- Rejected records: `{report.RejectedRecords}`");
        builder.AppendLine($"- Warning count: `{report.WarningCount}`");
        builder.AppendLine();

        builder.AppendLine("## Summary by category");
        builder.AppendLine();

        foreach (var item in report.SummaryByCategory)
        {
            builder.AppendLine($"- {item.Category}: {item.ErrorCount} errors, {item.WarningCount} warnings");
        }

        if (report.BatchMessages.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("## Batch-level findings");
            builder.AppendLine();

            foreach (var message in report.BatchMessages)
            {
                builder.AppendLine($"- [{message.Severity}] {message.Message}");
            }
        }

        var topRejectedRecords = report.RecordResults
            .Where(result => result.HasErrors)
            .Take(10)
            .ToArray();

        if (topRejectedRecords.Length > 0)
        {
            builder.AppendLine();
            builder.AppendLine("## Rejected records");
            builder.AppendLine();

            foreach (var result in topRejectedRecords)
            {
                builder.AppendLine($"### Record {result.Record.RecordNumber} ({result.Record.DocumentId ?? "No Document ID"})");

                foreach (var message in result.Messages.Where(message => message.Severity == ValidationSeverity.Error))
                {
                    builder.AppendLine($"- {message.FieldName ?? "record"}: {message.Message}");
                }

                var warningMessages = result.Messages.Where(message => message.Severity == ValidationSeverity.Warning).ToArray();
                if (warningMessages.Length > 0)
                {
                    builder.AppendLine("- Warnings:");

                    foreach (var warningMessage in warningMessages)
                    {
                        builder.AppendLine($"  - {warningMessage.Message}");
                    }
                }

                builder.AppendLine();
            }
        }

        return builder.ToString().TrimEnd();
    }
}
