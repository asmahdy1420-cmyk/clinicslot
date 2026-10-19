namespace ClinicSlot.Application.Contracts;

public sealed record AvailabilityRequest(Guid ServiceId, DateOnly From, DateOnly To)
{
    public const int MaxRangeDays = 31;
}

public sealed record SlotDto(DateTimeOffset Start, DateTimeOffset End);

public sealed record AvailabilityDay(DateOnly Date, IReadOnlyList<SlotDto> Slots);

public sealed record AvailabilityResponse(
    Guid PractitionerId,
    Guid ServiceId,
    string TimeZone,
    IReadOnlyList<AvailabilityDay> Days);
