using ErpDataImportValidator.Domain.Enums;

namespace ErpDataImportValidator.Domain.Models;

public sealed record RecordValidationResult(
    ImportDocumentRecord Record,
    IReadOnlyCollection<ValidationMessage> Messages)
{
    public bool HasErrors => Messages.Any(message => message.Severity == ValidationSeverity.Error);

    public bool HasWarnings => Messages.Any(message => message.Severity == ValidationSeverity.Warning);
}
