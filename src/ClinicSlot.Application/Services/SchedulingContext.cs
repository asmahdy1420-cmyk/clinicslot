using ClinicSlot.Application.Abstractions;
using ClinicSlot.Application.Exceptions;
using ClinicSlot.Domain.Clinics;
using ClinicSlot.Domain.Common;
using ClinicSlot.Domain.Practitioners;
using ClinicSlot.Domain.Scheduling;

namespace ClinicSlot.Application.Services;

/// <summary>Loads the clinic and practitioner a scheduling operation needs and builds calculator queries.</summary>
public sealed class SchedulingContext(
    IClinicRepository clinics,
    IPractitionerRepository practitioners,
    IClinicServiceRepository services,
    ITimeOffRepository timeOffs,
    IAppointmentRepository appointments)
{
    public async Task<(Clinic Clinic, Practitioner Practitioner)> LoadPractitionerAsync(
        Guid practitionerId,
        CancellationToken cancellationToken)
    {
        var practitioner = await practitioners.FindAsync(practitionerId, cancellationToken)
            ?? throw new NotFoundException("Practitioner", practitionerId);
        var clinic = await clinics.FindAsync(practitioner.ClinicId, cancellationToken)
            ?? throw new NotFoundException("Clinic", practitioner.ClinicId);
        return (clinic, practitioner);
    }

    public async Task<ClinicService> LoadServiceAsync(Guid serviceId, Clinic clinic, CancellationToken cancellationToken)
    {
        var service = await services.FindAsync(serviceId, cancellationToken)
            ?? throw new NotFoundException("Service", serviceId);
        if (service.ClinicId != clinic.Id)
        {
            throw new DomainRuleException("The service is not offered by the practitioner's clinic.");
        }

        return service;
    }

    /// <summary>
    /// Computes slots for a local date range. Time off and appointments are loaded for a
    /// UTC window padded by a day on each side, which covers every possible UTC offset.
    /// </summary>
    public async Task<IReadOnlyList<AvailableSlot>> CalculateAsync(
        Clinic clinic,
        Practitioner practitioner,
        DateOnly from,
        DateOnly to,
        TimeSpan duration,
        TimeSpan buffer,
        DateTimeOffset now,
        bool includeAppointments,
        Guid? excludeAppointmentId,
        CancellationToken cancellationToken)
    {
        var windowStart = new DateTimeOffset(from.AddDays(-1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var windowEnd = new DateTimeOffset(to.AddDays(2).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

        var off = await timeOffs.ListOverlappingAsync(practitioner.Id, windowStart, windowEnd, cancellationToken);

        IReadOnlyCollection<UtcInterval> busy = [];
        if (includeAppointments)
        {
            var booked = await appointments.ListBlockingAsync(
                practitioner.Id, windowStart, windowEnd, excludeAppointmentId, cancellationToken);
            busy = booked.Select(a => a.BlockedInterval).ToList();
        }

        var query = new AvailabilityQuery(
            clinic.GetTimeZone(),
            practitioner.Schedule,
            from,
            to,
            duration,
            buffer,
            clinic.SlotInterval,
            now,
            off.Select(t => t.Interval).ToList(),
            busy);

        return AvailabilityCalculator.Calculate(query);
    }

    /// <summary>
    /// Verifies that <paramref name="startUtc"/> is a slot the schedule offers (working hours,
    /// breaks, time off, slot grid), then that no other appointment holds the time. The second
    /// check gives a fast, friendly 409; the slot-claim key in the database is the real guarantee.
    /// </summary>
    public async Task EnsureBookableAsync(
        Clinic clinic,
        Practitioner practitioner,
        DateTimeOffset startUtc,
        TimeSpan duration,
        TimeSpan buffer,
        DateTimeOffset now,
        Guid? excludeAppointmentId,
        CancellationToken cancellationToken)
    {
        var zone = clinic.GetTimeZone();
        var localDate = ClinicTime.LocalDate(startUtc, zone);

        var offered = await CalculateAsync(
            clinic, practitioner, localDate, localDate, duration, buffer, now,
            includeAppointments: false, excludeAppointmentId: null, cancellationToken);

        if (!offered.Any(slot => slot.Start == startUtc))
        {
            throw new DomainRuleException(
                "The requested start time is not offered by the practitioner's schedule (working hours, breaks, time off or slot interval).");
        }

        var blocking = new UtcInterval(startUtc, startUtc + duration + buffer);
        var overlapping = await appointments.ListBlockingAsync(
            practitioner.Id, blocking.Start, blocking.End, excludeAppointmentId, cancellationToken);

        if (overlapping.Count > 0)
        {
            throw new SlotConflictException();
        }
    }
}
