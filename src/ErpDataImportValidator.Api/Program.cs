using System.Text.Json.Serialization;
using System.Reflection;
using ErpDataImportValidator.Application.Abstractions;
using ErpDataImportValidator.Application.Services;
using ErpDataImportValidator.Domain.Settings;
using ErpDataImportValidator.Infrastructure.Parsing;
using ErpDataImportValidator.Infrastructure.Reporting;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services.Configure<ValidationSettings>(builder.Configuration.GetSection("ValidationSettings"));

builder.Services.AddScoped<IImportValidationService, ImportValidationService>();
builder.Services.AddScoped<IImportParser, CsvImportParser>();
builder.Services.AddScoped<IImportParser, XmlImportParser>();
builder.Services.AddScoped<IMarkdownValidationReportBuilder, MarkdownValidationReportBuilder>();
builder.Services.AddScoped<IInvalidRecordExportBuilder, InvalidRecordCsvExportBuilder>();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "ERP Data Import Validator API",
        Version = "v1",
        Description = "Portfolio-ready ERP-style import validation API for CSV and XML business documents."
    });

    options.SupportNonNullableReferenceTypes();

    var xmlFileName = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlFilePath = Path.Combine(AppContext.BaseDirectory, xmlFileName);

    if (File.Exists(xmlFilePath))
    {
        options.IncludeXmlComments(xmlFilePath, includeControllerXmlComments: true);
    }
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/", () => Results.Redirect("/swagger"))
    .ExcludeFromDescription();

app.Run();
