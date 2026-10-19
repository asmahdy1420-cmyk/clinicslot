using ClinicSlot.Domain.Common;
using ClinicSlot.Domain.Practitioners;
using ClinicSlot.Domain.Scheduling;

namespace ClinicSlot.UnitTests.Practitioners;

public class WorkingDayTests
{
    [Fact]
    public void Working_windows_are_the_hours_with_breaks_cut_out()
    {
        var day = new WorkingDay(
            DayOfWeek.Monday,
            new TimeOnly(8, 0),
            new TimeOnly(18, 0),
            [Range(15, 0, 15, 15), Range(12, 0, 13, 0)]);

        var windows = day.WorkingWindows();

        Assert.Equal(
            [Range(8, 0, 12, 0), Range(13, 0, 15, 0), Range(15, 15, 18, 0)],
            windows);
    }

    [Fact]
    public void A_break_outside_working_hours_is_rejected()
    {
        Assert.Throws<DomainValidationException>(() =>
            new WorkingDay(DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(17, 0), [Range(16, 30, 17, 30)]));
    }

    [Fact]
    public void Overlapping_breaks_are_rejected()
    {
        Assert.Throws<DomainValidationException>(() =>
            new WorkingDay(
                DayOfWeek.Monday,
                new TimeOnly(9, 0),
                new TimeOnly(17, 0),
                [Range(12, 0, 13, 0), Range(12, 30, 13, 30)]));
    }

    [Fact]
    public void A_weekday_may_appear_only_once_in_a_schedule()
    {
        var monday = new WorkingDay(DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(12, 0));

        Assert.Throws<DomainValidationException>(() => new WeeklySchedule([monday, monday]));
    }

    private static LocalTimeRange Range(int startHour, int startMinute, int endHour, int endMinute) =>
        new(new TimeOnly(startHour, startMinute), new TimeOnly(endHour, endMinute));
}
