using ClinicSlot.Api.Http;
using ClinicSlot.Application.Contracts;
using ClinicSlot.Application.Services;

namespace ClinicSlot.Api.Endpoints;

internal static class PractitionerEndpoints
{
    public static IEndpointRouteBuilder MapPractitionerEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/practitioners/{practitionerId:guid}").WithTags("Practitioners");

        group.MapGet("/", async (Guid practitionerId, ClinicDirectoryService directory, CancellationToken ct) =>
                TypedResults.Ok(await directory.GetPractitionerAsync(practitionerId, ct)))
            .WithName("GetPractitioner");

        group.MapPut("/schedule", async (
                Guid practitionerId, ReplaceScheduleRequest request, ClinicDirectoryService directory, CancellationToken ct) =>
                TypedResults.Ok(await directory.ReplaceScheduleAsync(practitionerId, request, ct)))
            .Validate<ReplaceScheduleRequest>()
            .WithName("ReplaceSchedule");

        group.MapPost("/time-off", async (
                Guid practitionerId, CreateTimeOffRequest request, ClinicDirectoryService directory, CancellationToken ct) =>
            {
                var timeOff = await directory.AddTimeOffAsync(practitionerId, request, ct);
                return TypedResults.Created($"/api/practitioners/{practitionerId}/time-off/{timeOff.Id}", timeOff);
            })
            .Validate<CreateTimeOffRequest>()
            .WithName("AddTimeOff");

        group.MapGet("/availability", async (
                Guid practitionerId, [AsParameters] AvailabilityRequest request, AvailabilityService availability, CancellationToken ct) =>
                TypedResults.Ok(await availability.GetAsync(practitionerId, request, ct)))
            .Validate<AvailabilityRequest>()
            .WithName("GetAvailability")
            .WithSummary("Free slots for a service between two local dates (inclusive), in the clinic time zone.");

        return app;
    }
}
