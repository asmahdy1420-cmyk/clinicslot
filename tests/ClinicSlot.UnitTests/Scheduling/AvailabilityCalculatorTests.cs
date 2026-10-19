using ClinicSlot.Domain.Common;
using ClinicSlot.Domain.Practitioners;
using ClinicSlot.Domain.Scheduling;
using static ClinicSlot.UnitTests.TestZones;

namespace ClinicSlot.UnitTests.Scheduling;

public class AvailabilityCalculatorTests
{
    // Monday 23 March 2026 is before the UK DST change, so London local time equals UTC.
    private static readonly DateOnly Monday = new(2026, 3, 23);

    [Fact]
    public void Generates_slots_across_working_hours_on_the_step_grid()
    {
        var slots = Calculate(Day(DayOfWeek.Monday, "09:00", "12:00"), duration: 30, step: 30);

        Assert.Equal(6, slots.Count);
        Assert.Equal(Utc(2026, 3, 23, 9), slots[0].Start);
        Assert.Equal(Utc(2026, 3, 23, 11, 30), slots[^1].Start);
        Assert.Equal(Utc(2026, 3, 23, 12), slots[^1].End);
    }

    [Fact]
    public void Excludes_breaks_and_never_lets_a_service_run_into_one()
    {
        var day = Day(DayOfWeek.Monday, "09:00", "13:00", ("11:00", "12:00"));

        var slots = Calculate(day, duration: 60, step: 30);

        Assert.Equal(
            ["09:00", "09:30", "10:00", "12:00"],
            slots.Select(s => s.Start.ToString("HH:mm")).ToArray());
    }

    [Fact]
    public void Drops_candidates_that_would_overrun_the_end_of_the_day()
    {
        var slots = Calculate(Day(DayOfWeek.Monday, "09:00", "10:45"), duration: 30, step: 15);

        Assert.Equal(Utc(2026, 3, 23, 10, 15), slots[^1].Start);
        Assert.Equal(6, slots.Count);
    }

    [Fact]
    public void Respects_the_trailing_buffer_of_an_existing_appointment()
    {
        // Existing 09:00-09:30 appointment with a 10 minute buffer blocks until 09:40.
        var busy = new UtcInterval(Utc(2026, 3, 23, 9), Utc(2026, 3, 23, 9, 40));

        var slots = Calculate(Day(DayOfWeek.Monday, "09:00", "11:00"), duration: 30, step: 10, busy: [busy]);

        Assert.Equal(Utc(2026, 3, 23, 9, 40), slots[0].Start);
    }

    [Fact]
    public void Keeps_the_new_service_buffer_clear_of_the_next_appointment()
    {
        var busy = new UtcInterval(Utc(2026, 3, 23, 10), Utc(2026, 3, 23, 10, 30));

        var slots = Calculate(
            Day(DayOfWeek.Monday, "09:00", "11:00"), duration: 30, step: 15, buffer: 15, busy: [busy]);

        // 09:15 + 30 min + 15 min buffer ends exactly at 10:00, which is allowed (half-open).
        Assert.Equal(
            ["09:00", "09:15", "10:30"],
            slots.Select(s => s.Start.ToString("HH:mm")).ToArray());
    }

    [Fact]
    public void Excludes_time_off()
    {
        var off = new UtcInterval(Utc(2026, 3, 23, 12), Utc(2026, 3, 23, 17));

        var slots = Calculate(Day(DayOfWeek.Monday, "09:00", "17:00"), duration: 60, step: 60, timeOff: [off]);

        Assert.Equal(3, slots.Count);
        Assert.All(slots, s => Assert.True(s.End <= off.Start));
    }

    [Fact]
    public void Skips_slots_that_start_before_now()
    {
        var slots = Calculate(
            Day(DayOfWeek.Monday, "09:00", "12:00"), duration: 30, step: 30, notBefore: Utc(2026, 3, 23, 10, 20));

        Assert.Equal(Utc(2026, 3, 23, 10, 30), slots[0].Start);
    }

    [Fact]
    public void Returns_nothing_for_days_without_working_hours()
    {
        var slots = Calculate(Day(DayOfWeek.Tuesday, "09:00", "17:00"), duration: 30, step: 30);

        Assert.Empty(slots);
    }

    [Fact]
    public void Keeps_wall_clock_hours_across_the_spring_forward_weekend()
    {
        var schedule = new WeeklySchedule(
        [
            new WorkingDay(DayOfWeek.Saturday, T("09:00"), T("10:00")),
            new WorkingDay(DayOfWeek.Sunday, T("09:00"), T("10:00")),
        ]);

        var slots = AvailabilityCalculator.Calculate(Query(schedule, new(2026, 3, 28), new(2026, 3, 29), 60, 60));

        Assert.Equal(2, slots.Count);
        Assert.Equal(new DateTimeOffset(2026, 3, 28, 9, 0, 0, TimeSpan.Zero), slots[0].Start);
        Assert.Equal(new DateTimeOffset(2026, 3, 29, 9, 0, 0, TimeSpan.FromHours(1)), slots[1].Start);
        Assert.Equal(Utc(2026, 3, 29, 8), slots[1].Start.ToUniversalTime());
    }

    [Fact]
    public void Spring_forward_gap_shortens_a_night_shift_by_one_real_hour()
    {
        // 00:00-04:00 local on 29 March 2026: clocks jump 01:00 -> 02:00, so only 3 real hours exist.
        var schedule = new WeeklySchedule([new WorkingDay(DayOfWeek.Sunday, T("00:00"), T("04:00"))]);
        var date = new DateOnly(2026, 3, 29);

        var slots = AvailabilityCalculator.Calculate(Query(schedule, date, date, 60, 60));

        Assert.Equal(
            ["00:00+00:00", "02:00+01:00", "03:00+01:00"],
            slots.Select(s => s.Start.ToString("HH:mmzzz")).ToArray());
        Assert.All(slots, s => Assert.Equal(TimeSpan.FromHours(1), s.End - s.Start));
    }

    [Fact]
    public void Fall_back_overlap_offers_the_repeated_hour_twice()
    {
        // 00:00-04:00 local on 25 October 2026: 01:00-02:00 happens twice, giving 5 real hours.
        var schedule = new WeeklySchedule([new WorkingDay(DayOfWeek.Sunday, T("00:00"), T("04:00"))]);
        var date = new DateOnly(2026, 10, 25);

        var slots = AvailabilityCalculator.Calculate(Query(schedule, date, date, 60, 60));

        Assert.Equal(
            ["00:00+01:00", "01:00+01:00", "01:00+00:00", "02:00+00:00", "03:00+00:00"],
            slots.Select(s => s.Start.ToString("HH:mmzzz")).ToArray());
    }

    [Fact]
    public void Rejects_an_inverted_date_range()
    {
        var schedule = new WeeklySchedule([Day(DayOfWeek.Monday, "09:00", "10:00")]);

        Assert.Throws<DomainValidationException>(() =>
            AvailabilityCalculator.Calculate(Query(schedule, Monday, Monday.AddDays(-1), 30, 30)));
    }

    private static List<AvailableSlot> Calculate(
        WorkingDay day,
        int duration,
        int step,
        int buffer = 0,
        DateTimeOffset? notBefore = null,
        IReadOnlyCollection<UtcInterval>? busy = null,
        IReadOnlyCollection<UtcInterval>? timeOff = null)
    {
        var query = Query(new WeeklySchedule([day]), Monday, Monday, duration, step, buffer, notBefore, busy, timeOff);
        return AvailabilityCalculator.Calculate(query).ToList();
    }

    private static AvailabilityQuery Query(
        WeeklySchedule schedule,
        DateOnly from,
        DateOnly to,
        int duration,
        int step,
        int buffer = 0,
        DateTimeOffset? notBefore = null,
        IReadOnlyCollection<UtcInterval>? busy = null,
        IReadOnlyCollection<UtcInterval>? timeOff = null) =>
        new(
            London,
            schedule,
            from,
            to,
            TimeSpan.FromMinutes(duration),
            TimeSpan.FromMinutes(buffer),
            TimeSpan.FromMinutes(step),
            notBefore ?? Utc(2026, 1, 1, 0),
            timeOff ?? [],
            busy ?? []);

    private static WorkingDay Day(DayOfWeek day, string start, string end, params (string Start, string End)[] breaks) =>
        new(day, T(start), T(end), breaks.Select(b => new LocalTimeRange(T(b.Start), T(b.End))));

    private static TimeOnly T(string value) => TimeOnly.ParseExact(value, "HH:mm");
}
