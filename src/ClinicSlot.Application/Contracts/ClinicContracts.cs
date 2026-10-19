namespace ClinicSlot.Application.Contracts;

public sealed record CreateClinicRequest(
    string Name,
    string TimeZone,
    int SlotIntervalMinutes = 15,
    int ChangeCutoffHours = 24);

public sealed record ClinicResponse(
    Guid Id,
    string Name,
    string TimeZone,
    int SlotIntervalMinutes,
    int ChangeCutoffHours);

public sealed record CreateServiceRequest(string Name, int DurationMinutes, int BufferMinutes = 0);

public sealed record ServiceResponse(Guid Id, Guid ClinicId, string Name, int DurationMinutes, int BufferMinutes);
