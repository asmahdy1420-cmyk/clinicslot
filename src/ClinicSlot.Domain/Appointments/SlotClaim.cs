namespace ClinicSlot.Domain.Appointments;

/// <summary>
/// One five-minute grid unit of a practitioner's time held by an appointment
/// (its duration plus buffer). The persistence layer keys claims by
/// (PractitionerId, SlotStartUtc), so the database itself rejects any overlap.
/// </summary>
public sealed class SlotClaim
{
    internal SlotClaim(Guid appointmentId, Guid practitionerId, DateTimeOffset slotStartUtc)
    {
        AppointmentId = appointmentId;
        PractitionerId = practitionerId;
        SlotStartUtc = slotStartUtc;
    }

    private SlotClaim()
    {
    }

    public Guid AppointmentId { get; private set; }

    public Guid PractitionerId { get; private set; }

    public DateTimeOffset SlotStartUtc { get; private set; }
}
