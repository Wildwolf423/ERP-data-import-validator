# ERP Data Import Validator

`ERP Data Import Validator` is a portfolio-quality ASP.NET Core Web API built with .NET 8 for validating structured business import files before they enter an ERP-style system.

The project focuses on a practical enterprise scenario: CSV and XML import batches are checked for structural issues, missing required fields, and ERP business-rule violations, then returned as a detailed validation report that operations teams, consultants, or integration developers can act on immediately.

## Why This Project Matters

ERP systems are highly sensitive to inbound data quality. A single malformed invoice import, duplicate document number, unsupported currency, or invalid supplier identifier can create failed imports, posting issues, or manual cleanup work.

This API is designed as a realistic pre-import control layer. It helps teams validate data before attempting import into Business Central, NAV, or another ERP platform.

## Business Use Case

Typical ERP integration scenarios involve inbound files for:

- supplier invoice imports
- vendor master-data updates
- operational batch loads from partner systems
- middleware-driven document synchronization

Before data reaches the ERP, this service checks whether the file is structurally valid and whether each record is acceptable under business rules such as allowed currencies, supplier ID format, valid document types, and location-specific constraints.

## Portfolio Value

This project is especially relevant for junior ERP and Business Central oriented roles because it demonstrates:

- backend business logic rather than tutorial-style CRUD only
- realistic CSV and XML import handling
- validation design that mirrors ERP integration work
- maintainable layering and separation of concerns
- operationally useful error reporting instead of vague pass/fail responses

## Architecture Overview

The solution uses a clean layered structure:

- `src/ErpDataImportValidator.Domain`
  Core enums, validation models, and configuration objects.
- `src/ErpDataImportValidator.Application`
  Application contracts, DTOs, mapping, and the main orchestration service for validation.
- `src/ErpDataImportValidator.Infrastructure`
  CSV parsing, XML parsing, markdown report generation, and invalid-record export building.
- `src/ErpDataImportValidator.Api`
  ASP.NET Core API endpoints, configuration binding, Swagger setup, and dependency injection.
- `tests/ErpDataImportValidator.UnitTests`
  Focused tests for parsing behavior and business-rule validation.
- `sample-data`
  Ready-to-use demo files for Swagger or CLI testing.

## Supported File Formats

- CSV
- XML

## Validation Categories

The API validates imports in three layers:

1. Structure validation
   - missing required CSV columns
   - malformed XML
   - invalid XML root structure
   - missing XML nodes
   - invalid decimal and date formats
   - empty CSV rows
2. Required field validation
   - `documentId`
   - `supplierId`
   - `supplierName`
   - `documentDate`
   - `amount`
   - `currency`
   - `location`
   - `documentType`
3. Business-rule validation
   - amount must be positive
   - currency must be allowed
   - document date must not be in the future
   - supplier ID must match a configured regex
   - duplicate document IDs in one batch are rejected
   - invalid location and document type combinations are flagged
   - suspiciously high amounts generate warnings

## Configuration

Validation settings are configurable in [appsettings.json](/C:/Users/mizer/Desktop/CV_projects/erp-data-import-validator/src/ErpDataImportValidator.Api/appsettings.json):

- allowed currencies
- valid document types
- valid locations
- supplier ID regex
- strict or relaxed validation mode
- suspicious amount warning threshold
- location and document type rules

This mirrors real ERP projects where validation rules vary between clients, departments, and countries.

## API Endpoints

- `POST /api/import-validation/csv`
- `POST /api/import-validation/xml`
- `GET /api/import-validation/sample-summary`

The upload endpoints accept `multipart/form-data` with:

- `file`
- `includePreview`
- `previewLimit`
- `mode` with optional values `Strict` or `Relaxed`

## Response Model

Each validation response includes:

- `totalRecords`
- `acceptedRecords`
- `rejectedRecords`
- `warningCount`
- batch-level validation messages
- record-level validation messages
- error summary by category
- optional preview records
- markdown summary report
- optional invalid-record CSV export content

## Sample Files

The repository includes four realistic sample files:

- [valid-supplier-invoices.csv](/C:/Users/mizer/Desktop/CV_projects/erp-data-import-validator/sample-data/valid-supplier-invoices.csv)
- [invalid-supplier-invoices.csv](/C:/Users/mizer/Desktop/CV_projects/erp-data-import-validator/sample-data/invalid-supplier-invoices.csv)
- [valid-supplier-invoices.xml](/C:/Users/mizer/Desktop/CV_projects/erp-data-import-validator/sample-data/valid-supplier-invoices.xml)
- [invalid-supplier-invoices.xml](/C:/Users/mizer/Desktop/CV_projects/erp-data-import-validator/sample-data/invalid-supplier-invoices.xml)

## Expected Validation Scenarios

The sample files are designed to demonstrate realistic outcomes:

- `valid-supplier-invoices.csv`
  Demonstrates a clean CSV import with accepted records only.
- `invalid-supplier-invoices.csv`
  Demonstrates duplicate document detection, invalid supplier ID format, missing required values, malformed dates and amounts, and non-blocking empty-row handling.
- `valid-supplier-invoices.xml`
  Demonstrates a valid XML batch using the expected `ImportBatch` and `Document` structure.
- `invalid-supplier-invoices.xml`
  Demonstrates missing XML elements, duplicate document IDs, unsupported values, future-date validation, and invalid location/document type combinations.

## How to Demo This Project

Use these exact steps from the repository root:

1. Run the API:

```bash
dotnet restore ErpDataImportValidator.sln
dotnet build ErpDataImportValidator.sln
dotnet run --project src/ErpDataImportValidator.Api/ErpDataImportValidator.Api.csproj
```

2. Open Swagger:

- `https://localhost:7070/swagger`
- or the URL shown by `dotnet run`

3. Call `GET /api/import-validation/sample-summary` first.
   This shows the available sample files and the active validation settings.

4. Test `POST /api/import-validation/csv` with:

- `sample-data/valid-supplier-invoices.csv`
  Expected outcome: accepted records only, no blocking errors.
- `sample-data/invalid-supplier-invoices.csv`
  Expected outcome: rejected records, duplicate detection, format errors, missing values, and category summaries.

5. Test `POST /api/import-validation/xml` with:

- `sample-data/valid-supplier-invoices.xml`
  Expected outcome: accepted records only.
- `sample-data/invalid-supplier-invoices.xml`
  Expected outcome: missing-node findings, future-date rejection, duplicate detection, and configuration-based rule failures.

6. In Swagger, leave `includePreview=true` and set `previewLimit=5`.
   This makes the demo easier to explain because the first records are visible directly in the response.

7. In a second run, set `mode=Relaxed` for an invalid file.
   This shows how some configuration-sensitive findings can move from errors to warnings.

## Run Instructions

### Visual Studio 2022

1. Open [ErpDataImportValidator.sln](/C:/Users/mizer/Desktop/CV_projects/erp-data-import-validator/ErpDataImportValidator.sln).
2. Set `ErpDataImportValidator.Api` as the startup project.
3. Press `F5` or `Ctrl+F5`.
4. Open `/swagger` if it does not launch automatically.

### dotnet CLI

Run these commands from the repository root:

```bash
dotnet restore ErpDataImportValidator.sln
dotnet build ErpDataImportValidator.sln
dotnet test ErpDataImportValidator.sln
dotnet run --project src/ErpDataImportValidator.Api/ErpDataImportValidator.Api.csproj
```

## Example API Usage

Validate an invalid CSV file:

```bash
curl -X POST "https://localhost:7070/api/import-validation/csv" \
  -F "file=@sample-data/invalid-supplier-invoices.csv" \
  -F "includePreview=true" \
  -F "previewLimit=5"
```

Validate an invalid XML file in relaxed mode:

```bash
curl -X POST "https://localhost:5038/api/import-validation/xml" \
  -F "file=@sample-data/invalid-supplier-invoices.xml" \
  -F "mode=Relaxed"
```

## Testing

The unit tests cover:

- CSV structure validation
- XML structure validation
- duplicate detection
- future-date validation
- allowed-currency validation
- supplier ID format validation

Run:

```bash
dotnet test ErpDataImportValidator.sln
```

## Optional Docker Support

The repository includes a [Dockerfile](/C:/Users/mizer/Desktop/CV_projects/erp-data-import-validator/Dockerfile) for containerized execution. Docker is optional; the project is intentionally easy to run directly with Visual Studio or the `dotnet` CLI.

## CV Project Description

Built a layered ASP.NET Core Web API in .NET 8 for validating ERP import files in CSV and XML before downstream processing.  
Implemented structural validation, required-field checks, configurable business rules, duplicate detection, and detailed validation reporting.  
Added realistic sample import files, focused unit tests, Swagger-based API exploration, and configuration-driven validation settings to reflect practical ERP integration work.  
Designed the solution around common Business Central and ERP backend concerns such as import control, data quality, and operational error reporting.

## LinkedIn-ready Project Summary

Built `ERP Data Import Validator`, a .NET 8 Web API that validates CSV and XML import files before they enter an ERP workflow. The project checks structure, required fields, and configurable business rules, then returns detailed JSON and markdown validation reports that reflect a realistic enterprise integration utility.

## Interview Talking Points

1. This project demonstrates how to stop bad inbound data before it reaches ERP posting or master-data import processes.
2. It shows clean separation between file parsing, business-rule validation, reporting, and API delivery, which is essential in maintainable ERP integrations.
3. It reflects real Business Central style work where CSV/XML imports, configurable validation, and clear operational feedback matter more than frontend complexity.
