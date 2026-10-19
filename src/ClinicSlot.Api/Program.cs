using System.Text.Json.Serialization;
using ClinicSlot.Api.Endpoints;
using ClinicSlot.Api.Http;
using ClinicSlot.Application;
using ClinicSlot.Infrastructure;
using ClinicSlot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.SerializerOptions.Converters.Add(new TimeOnlyJsonConverter());
});

// Surface body and query binding failures through the exception handler so clients get a
// ProblemDetails response that says what was wrong instead of an empty 400.
builder.Services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = true);

builder.Services.AddProblemDetails(options =>
    options.CustomizeProblemDetails = context =>
        context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier);
builder.Services.AddExceptionHandler<ExceptionToProblemDetailsHandler>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
    options.SwaggerDoc("v1", new() { Title = "ClinicSlot API", Version = "v1" }));

builder.Services.AddApplication();
builder.Services.AddInfrastructure(provider =>
    provider.GetRequiredService<IConfiguration>().GetConnectionString("ClinicSlot")
    ?? throw new InvalidOperationException("Connection string 'ClinicSlot' is not configured."));
builder.Services.AddHealthChecks().AddDbContextCheck<ClinicSlotDbContext>("database");

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Configuration.GetValue("Swagger:Enabled", app.Environment.IsDevelopment()))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (app.Configuration.GetValue("Database:MigrateOnStartup", true))
{
    using var scope = app.Services.CreateScope();
    scope.ServiceProvider.GetRequiredService<ClinicSlotDbContext>().Database.Migrate();
}

app.MapHealthChecks("/health");
app.MapClinicEndpoints();
app.MapPractitionerEndpoints();
app.MapAppointmentEndpoints();

app.Run();

/// <summary>Entry point; public so integration tests can host the API in memory.</summary>
public partial class Program;
