using ErpDataImportValidator.Domain.Models;

namespace ErpDataImportValidator.Application.Abstractions;

public interface IMarkdownValidationReportBuilder
{
    string Build(string fileName, ValidationReport report);
}
