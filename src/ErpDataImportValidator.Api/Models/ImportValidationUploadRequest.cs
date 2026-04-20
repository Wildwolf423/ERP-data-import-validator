using System.ComponentModel.DataAnnotations;
using ErpDataImportValidator.Domain.Enums;

namespace ErpDataImportValidator.Api.Models;

public sealed class ImportValidationUploadRequest
{
    [Required(ErrorMessage = "Please select a CSV or XML file to validate.")]
    public IFormFile? File { get; init; }

    /// <summary>
    /// When true, the API returns a preview of parsed records in the response.
    /// </summary>
    public bool IncludePreview { get; init; } = true;

    /// <summary>
    /// Maximum number of preview records to include in the response. Allowed range: 0 to 50.
    /// </summary>
    [Range(0, 50, ErrorMessage = "Preview limit must be between 0 and 50 records.")]
    public int PreviewLimit { get; init; } = 5;

    /// <summary>
    /// Optional validation mode override. Use Relaxed mode to downgrade certain business-rule findings to warnings.
    /// </summary>
    public ValidationMode? Mode { get; init; }
}
