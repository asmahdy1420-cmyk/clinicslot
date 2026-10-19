using ClinicSlot.Application.Contracts;

namespace ClinicSlot.Application.Services;

public sealed class AvailabilityService(SchedulingContext context, TimeProvider timeProvider)
{
    public async Task<AvailabilityResponse> GetAsync(
        Guid practitionerId,
        AvailabilityRequest request,
        CancellationToken cancellationToken)
    {
        var (clinic, practitioner) = await context.LoadPractitionerAsync(practitionerId, cancellationToken);
        var service = await context.LoadServiceAsync(request.ServiceId, clinic, cancellationToken);

        var slots = await context.CalculateAsync(
            clinic,
            practitioner,
            request.From,
            request.To,
            service.Duration,
            service.Buffer,
            timeProvider.GetUtcNow(),
            includeAppointments: true,
            excludeAppointmentId: null,
            cancellationToken);

        var days = slots
            .GroupBy(slot => slot.Date)
            .Select(group => new AvailabilityDay(
                group.Key,
                group.Select(slot => new SlotDto(slot.Start, slot.End)).ToList()))
            .ToList();

        return new AvailabilityResponse(practitioner.Id, service.Id, clinic.TimeZoneId, days);
    }
}
