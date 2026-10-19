namespace ClinicSlot.Application.Contracts;

public sealed record BreakDto(TimeOnly Start, TimeOnly End);

public sealed record WorkingDayDto(DayOfWeek Day, TimeOnly Start, TimeOnly End, IReadOnlyList<BreakDto>? Breaks = null);

public sealed record CreatePractitionerRequest(string Name, IReadOnlyList<WorkingDayDto> WorkingHours);

public sealed record ReplaceScheduleRequest(IReadOnlyList<WorkingDayDto> WorkingHours);

public sealed record PractitionerResponse(Guid Id, Guid ClinicId, string Name, IReadOnlyList<WorkingDayDto> WorkingHours);

public sealed record CreateTimeOffRequest(DateTimeOffset Start, DateTimeOffset End, string? Reason = null);

public sealed record TimeOffResponse(Guid Id, Guid PractitionerId, DateTimeOffset Start, DateTimeOffset End, string? Reason);
