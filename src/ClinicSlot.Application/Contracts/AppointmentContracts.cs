namespace ClinicSlot.Application.Contracts;

public sealed record PatientDto(string FullName, string Email, string? Phone = null);

public sealed record BookAppointmentRequest(Guid PractitionerId, Guid ServiceId, DateTimeOffset Start, PatientDto Patient);

public sealed record RescheduleAppointmentRequest(DateTimeOffset Start);

public sealed record AppointmentResponse(
    Guid Id,
    Guid ClinicId,
    Guid PractitionerId,
    Guid ServiceId,
    Guid PatientId,
    DateTimeOffset Start,
    DateTimeOffset End,
    string Status,
    int Version);
