using ClinicSlot.Domain.Common;

namespace ClinicSlot.Domain.Scheduling;

/// <summary>
/// Computes free slots. The algorithm, per local date:
/// 1. take the working hours for that weekday and cut the breaks out, giving windows;
/// 2. resolve each window's wall-clock boundaries to UTC instants (DST-aware);
/// 3. walk candidate starts from the window start in <see cref="AvailabilityQuery.Step"/>
///    increments on the UTC line, keeping a candidate when the service fits in the window,
///    it starts after NotBefore, it does not touch time off, and the service plus its
///    buffer does not overlap any busy interval.
/// Walking on the UTC line means a 60-minute service always lasts 60 real minutes, even
/// across a DST transition, while wall-clock alignment is kept because real-world offset
/// changes are multiples of the step.
/// </summary>
public static class AvailabilityCalculator
{
    public static IReadOnlyList<AvailableSlot> Calculate(AvailabilityQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.To < query.From)
        {
            throw new DomainValidationException("The range end date must not be before its start date.");
        }

        if (query.Duration <= TimeSpan.Zero || query.Step <= TimeSpan.Zero || query.Buffer < TimeSpan.Zero)
        {
            throw new DomainValidationException("Duration and step must be positive and buffer must not be negative.");
        }

        var slots = new List<AvailableSlot>();

        for (var date = query.From; date <= query.To; date = date.AddDays(1))
        {
            var day = query.Schedule.For(date.DayOfWeek);
            if (day is null)
            {
                continue;
            }

            foreach (var window in day.WorkingWindows())
            {
                var windowStart = ClinicTime.ToUtc(date, window.Start, query.TimeZone);
                var windowEnd = ClinicTime.ToUtc(date, window.End, query.TimeZone, isRangeEnd: true);

                for (var start = windowStart; start + query.Duration <= windowEnd; start += query.Step)
                {
                    if (start < query.NotBefore)
                    {
                        continue;
                    }

                    var service = new UtcInterval(start, start + query.Duration);
                    if (query.TimeOff.Any(off => off.Overlaps(service)))
                    {
                        continue;
                    }

                    var blocking = new UtcInterval(start, service.End + query.Buffer);
                    if (query.Busy.Any(busy => busy.Overlaps(blocking)))
                    {
                        continue;
                    }

                    slots.Add(new AvailableSlot(
                        date,
                        ClinicTime.ToLocal(service.Start, query.TimeZone),
                        ClinicTime.ToLocal(service.End, query.TimeZone)));
                }
            }
        }

        return slots;
    }
}
