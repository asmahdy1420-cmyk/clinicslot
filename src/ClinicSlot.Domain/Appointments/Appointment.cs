using ClinicSlot.Domain.Common;
using ClinicSlot.Domain.Scheduling;

namespace ClinicSlot.Domain.Appointments;

public sealed class Appointment
{
    private readonly List<SlotClaim> _claims = [];

    private Appointment()
    {
    }

    public Guid Id { get; private set; }

    public Guid ClinicId { get; private set; }

    public Guid PractitionerId { get; private set; }

    public Guid ServiceId { get; private set; }

    public Guid PatientId { get; private set; }

    public DateTimeOffset StartUtc { get; private set; }

    public DateTimeOffset EndUtc { get; private set; }

    /// <summary>End of the trailing buffer; the practitioner is busy until this instant.</summary>
    public DateTimeOffset BlockedUntilUtc { get; private set; }

    public AppointmentStatus Status { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset? CancelledAtUtc { get; private set; }

    /// <summary>Optimistic concurrency token, incremented on every change.</summary>
    public int Version { get; private set; }

    public IReadOnlyCollection<SlotClaim> Claims => _claims;

    public TimeSpan Duration => EndUtc - StartUtc;

    public TimeSpan Buffer => BlockedUntilUtc - EndUtc;

    public UtcInterval BlockedInterval => new(StartUtc, BlockedUntilUtc);

    public static Appointment Book(
        Guid clinicId,
        Guid practitionerId,
        Guid serviceId,
        Guid patientId,
        DateTimeOffset start,
        TimeSpan duration,
        TimeSpan buffer,
        DateTimeOffset now)
    {
        Guard.NotEmpty(clinicId, nameof(clinicId));
        Guard.NotEmpty(practitionerId, nameof(practitionerId));
        Guard.NotEmpty(serviceId, nameof(serviceId));
        Guard.NotEmpty(patientId, nameof(patientId));

        if (duration <= TimeSpan.Zero || buffer < TimeSpan.Zero || !SlotGrid.IsAligned(duration) || !SlotGrid.IsAligned(buffer))
        {
            throw new DomainValidationException($"Duration and buffer must be multiples of {SlotGrid.UnitMinutes} minutes.");
        }

        EnsureValidStart(start, now);

        var appointment = new Appointment
        {
            Id = Guid.NewGuid(),
            ClinicId = clinicId,
            PractitionerId = practitionerId,
            ServiceId = serviceId,
            PatientId = patientId,
            Status = AppointmentStatus.Booked,
            CreatedAtUtc = now.ToUniversalTime(),
            Version = 1,
        };
        appointment.Place(start.ToUniversalTime(), duration, buffer);
        return appointment;
    }

    /// <summary>Throws unless the appointment is booked and outside the change cutoff.</summary>
    public void EnsureCanChange(DateTimeOffset now, TimeSpan cutoff)
    {
        if (Status != AppointmentStatus.Booked)
        {
            throw new DomainRuleException("Only booked appointments can be changed.");
        }

        if (now > StartUtc - cutoff)
        {
            throw new DomainRuleException(
                $"Appointments cannot be cancelled or rescheduled within {cutoff.TotalHours:0.##} hours of their start.");
        }
    }

    public void Cancel(DateTimeOffset now, TimeSpan cutoff)
    {
        EnsureCanChange(now, cutoff);

        Status = AppointmentStatus.Cancelled;
        CancelledAtUtc = now.ToUniversalTime();
        _claims.Clear();
        Version++;
    }

    public void Reschedule(DateTimeOffset newStart, DateTimeOffset now, TimeSpan cutoff)
    {
        EnsureCanChange(now, cutoff);
        EnsureValidStart(newStart, now);

        Place(newStart.ToUniversalTime(), Duration, Buffer);
        Version++;
    }

    private static void EnsureValidStart(DateTimeOffset start, DateTimeOffset now)
    {
        if (!SlotGrid.IsAligned(start))
        {
            throw new DomainValidationException($"Start time must be on a {SlotGrid.UnitMinutes}-minute boundary.");
        }

        if (start <= now)
        {
            throw new DomainRuleException("Appointments must start in the future.");
        }
    }

    /// <summary>
    /// Sets the times and reconciles the claim set. Claims that stay valid are kept rather
    /// than deleted and re-inserted, so a reschedule that overlaps the old time never
    /// collides with itself on the unique key.
    /// </summary>
    private void Place(DateTimeOffset startUtc, TimeSpan duration, TimeSpan buffer)
    {
        StartUtc = startUtc;
        EndUtc = startUtc + duration;
        BlockedUntilUtc = EndUtc + buffer;

        var wanted = SlotGrid.Units(BlockedInterval).ToHashSet();
        _claims.RemoveAll(claim => !wanted.Contains(claim.SlotStartUtc));

        var held = _claims.Select(claim => claim.SlotStartUtc).ToHashSet();
        foreach (var unit in wanted.Where(unit => !held.Contains(unit)).Order())
        {
            _claims.Add(new SlotClaim(Id, PractitionerId, unit));
        }
    }
}
