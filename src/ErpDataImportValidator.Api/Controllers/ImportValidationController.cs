using ErpDataImportValidator.Api.Models;
using ErpDataImportValidator.Application.Abstractions;
using ErpDataImportValidator.Application.DTOs;
using ErpDataImportValidator.Application.Extensions;
using ErpDataImportValidator.Domain.Enums;
using ErpDataImportValidator.Domain.Settings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ErpDataImportValidator.Api.Controllers;

/// <summary>
/// Validates ERP-style import files before they are loaded into downstream business processes.
/// </summary>
[ApiController]
[Route("api/import-validation")]
public sealed class ImportValidationController : ControllerBase
{
    private readonly IImportValidationService _importValidationService;
    private readonly IWebHostEnvironment _environment;
    private readonly IOptions<ValidationSettings> _validationSettings;
    private readonly ILogger<ImportValidationController> _logger;

    public ImportValidationController(
        IImportValidationService importValidationService,
        IWebHostEnvironment environment,
        IOptions<ValidationSettings> validationSettings,
        ILogger<ImportValidationController> logger)
    {
        _importValidationService = importValidationService;
        _environment = environment;
        _validationSettings = validationSettings;
        _logger = logger;
    }

    /// <summary>
    /// Uploads a CSV file and returns a detailed validation report.
    /// </summary>
    /// <remarks>
    /// Use the sample file <c>sample-data/invalid-supplier-invoices.csv</c> to demonstrate structure issues,
    /// duplicate document detection, invalid supplier formats, and business-rule failures.
    /// </remarks>
    [HttpPost("csv")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ImportValidationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public Task<ActionResult<ImportValidationResponseDto>> ValidateCsv(
        [FromForm] ImportValidationUploadRequest request,
        CancellationToken cancellationToken)
    {
        return ValidateAsync(request, ImportFileType.Csv, cancellationToken);
    }

    /// <summary>
    /// Uploads an XML file and returns a detailed validation report.
    /// </summary>
    /// <remarks>
    /// Use the sample file <c>sample-data/invalid-supplier-invoices.xml</c> to demonstrate missing nodes,
    /// unsupported values, duplicate document detection, and future-date validation.
    /// </remarks>
    [HttpPost("xml")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ImportValidationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public Task<ActionResult<ImportValidationResponseDto>> ValidateXml(
        [FromForm] ImportValidationUploadRequest request,
        CancellationToken cancellationToken)
    {
        return ValidateAsync(request, ImportFileType.Xml, cancellationToken);
    }

    /// <summary>
    /// Returns the bundled sample files and the active validation settings summary.
    /// </summary>
    [HttpGet("sample-summary")]
    [ProducesResponseType(typeof(SampleSummaryDto), StatusCodes.Status200OK)]
    public ActionResult<SampleSummaryDto> GetSampleSummary()
    {
        var repositoryRoot = Path.GetFullPath(Path.Combine(_environment.ContentRootPath, "..", ".."));
        var sampleDataPath = Path.Combine(repositoryRoot, "sample-data");

        var sampleFiles = new List<SampleFileDto>();

        if (Directory.Exists(sampleDataPath))
        {
            foreach (var filePath in Directory.GetFiles(sampleDataPath).OrderBy(path => path))
            {
                var extension = Path.GetExtension(filePath);
                var fileType = string.Equals(extension, ".xml", StringComparison.OrdinalIgnoreCase)
                    ? ImportFileType.Xml
                    : ImportFileType.Csv;

                sampleFiles.Add(new SampleFileDto(
                    Path.GetFileName(filePath),
                    fileType,
                    BuildDescription(Path.GetFileName(filePath)),
                    Path.GetRelativePath(repositoryRoot, filePath).Replace('\\', '/')));
            }
        }

        return Ok(new SampleSummaryDto(
            sampleFiles,
            _validationSettings.Value.ToSummaryDto()));
    }

    private async Task<ActionResult<ImportValidationResponseDto>> ValidateAsync(
        ImportValidationUploadRequest request,
        ImportFileType fileType,
        CancellationToken cancellationToken)
    {
        if (request.File is null || request.File.Length == 0)
        {
            return BadRequest(new ValidationProblemDetails(CreateValidationDictionary("file", "Please upload a non-empty file to start validation."))
            {
                Title = "Request validation failed.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        if (!HasExpectedExtension(request.File.FileName, fileType))
        {
            _logger.LogWarning(
                "Upload rejected for {FileName}. Expected endpoint for {ExpectedFileType}.",
                request.File.FileName,
                fileType);

            return BadRequest(new ValidationProblemDetails(CreateValidationDictionary(
                "file",
                $"The uploaded file '{request.File.FileName}' does not match the expected {fileType} format for this endpoint."))
            {
                Title = "Request validation failed.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        await using var stream = request.File.OpenReadStream();
        var response = await _importValidationService.ValidateAsync(
            stream,
            request.File.FileName,
            fileType,
            new ImportValidationExecutionOptions(request.IncludePreview, request.PreviewLimit, request.Mode),
            cancellationToken);

        return Ok(response);
    }

    private static bool HasExpectedExtension(string fileName, ImportFileType fileType)
    {
        var extension = Path.GetExtension(fileName);

        return fileType switch
        {
            ImportFileType.Csv => string.Equals(extension, ".csv", StringComparison.OrdinalIgnoreCase),
            ImportFileType.Xml => string.Equals(extension, ".xml", StringComparison.OrdinalIgnoreCase),
            _ => false
        };
    }

    private static Dictionary<string, string[]> CreateValidationDictionary(string key, string message)
    {
        return new Dictionary<string, string[]>
        {
            [key] = [message]
        };
    }

    private static string BuildDescription(string fileName)
    {
        return fileName switch
        {
            "valid-supplier-invoices.csv" => "Sample CSV batch with valid supplier invoice rows.",
            "invalid-supplier-invoices.csv" => "Sample CSV batch containing duplicate IDs, invalid supplier IDs, and business-rule errors.",
            "valid-supplier-invoices.xml" => "Sample XML batch with valid supplier invoice records.",
            "invalid-supplier-invoices.xml" => "Sample XML batch containing structural gaps and business-rule errors.",
            _ => "Sample import file."
        };
    }
}
