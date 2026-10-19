using ClinicSlot.Api.Http;
using ClinicSlot.Application.Contracts;
using ClinicSlot.Application.Services;

namespace ClinicSlot.Api.Endpoints;

internal static class ClinicEndpoints
{
    public static IEndpointRouteBuilder MapClinicEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/clinics").WithTags("Clinics");

        group.MapPost("/", async (CreateClinicRequest request, ClinicDirectoryService directory, CancellationToken ct) =>
            {
                var clinic = await directory.CreateClinicAsync(request, ct);
                return TypedResults.Created($"/api/clinics/{clinic.Id}", clinic);
            })
            .Validate<CreateClinicRequest>()
            .WithName("CreateClinic");

        group.MapGet("/{clinicId:guid}", async (Guid clinicId, ClinicDirectoryService directory, CancellationToken ct) =>
                TypedResults.Ok(await directory.GetClinicAsync(clinicId, ct)))
            .WithName("GetClinic");

        group.MapPost("/{clinicId:guid}/services", async (
                Guid clinicId, CreateServiceRequest request, ClinicDirectoryService directory, CancellationToken ct) =>
            {
                var service = await directory.AddServiceAsync(clinicId, request, ct);
                return TypedResults.Created($"/api/clinics/{clinicId}/services/{service.Id}", service);
            })
            .Validate<CreateServiceRequest>()
            .WithName("AddService");

        group.MapGet("/{clinicId:guid}/services", async (Guid clinicId, ClinicDirectoryService directory, CancellationToken ct) =>
                TypedResults.Ok(await directory.ListServicesAsync(clinicId, ct)))
            .WithName("ListServices");

        group.MapPost("/{clinicId:guid}/practitioners", async (
                Guid clinicId, CreatePractitionerRequest request, ClinicDirectoryService directory, CancellationToken ct) =>
            {
                var practitioner = await directory.AddPractitionerAsync(clinicId, request, ct);
                return TypedResults.Created($"/api/practitioners/{practitioner.Id}", practitioner);
            })
            .Validate<CreatePractitionerRequest>()
            .WithName("AddPractitioner");

        return app;
    }
}
