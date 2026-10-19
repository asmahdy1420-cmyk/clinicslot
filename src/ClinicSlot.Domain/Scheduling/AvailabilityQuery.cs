using ClinicSlot.Domain.Practitioners;

namespace ClinicSlot.Domain.Scheduling;

/// <summary>Everything the availability calculator needs; it performs no I/O itself.</summary>
/// <param name="TimeZone">The clinic time zone the weekly schedule is expressed in.</param>
/// <param name="Schedule">The practitioner's weekly working hours and breaks.</param>
/// <param name="From">First local date to compute, inclusive.</param>
/// <param name="To">Last local date to compute, inclusive.</param>
/// <param name="Duration">Length of the service being booked.</param>
/// <param name="Buffer">Time that must stay free after the service.</param>
/// <param name="Step">Distance between candidate start times.</param>
/// <param name="NotBefore">Earliest instant a slot may start (usually "now").</param>
/// <param name="TimeOff">Blocks during which the practitioner is unavailable.</param>
/// <param name="Busy">Existing appointments, each including its own trailing buffer.</param>
public sealed record AvailabilityQuery(
    TimeZoneInfo TimeZone,
    WeeklySchedule Schedule,
    DateOnly From,
    DateOnly To,
    TimeSpan Duration,
    TimeSpan Buffer,
    TimeSpan Step,
    DateTimeOffset NotBefore,
    IReadOnlyCollection<UtcInterval> TimeOff,
    IReadOnlyCollection<UtcInterval> Busy);

/// <summary>A bookable slot. Start and End carry the clinic's local UTC offset.</summary>
public sealed record AvailableSlot(DateOnly Date, DateTimeOffset Start, DateTimeOffset End);
