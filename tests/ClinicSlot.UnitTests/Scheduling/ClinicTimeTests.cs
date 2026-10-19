using ClinicSlot.Domain.Scheduling;
using static ClinicSlot.UnitTests.TestZones;

namespace ClinicSlot.UnitTests.Scheduling;

public class ClinicTimeTests
{
    [Fact]
    public void A_local_time_inside_the_spring_forward_gap_moves_to_the_end_of_the_gap()
    {
        // 01:30 does not exist in London on 29 March 2026; the first valid local time is 02:00 BST.
        var instant = ClinicTime.ToUtc(new DateOnly(2026, 3, 29), new TimeOnly(1, 30), London);

        Assert.Equal(Utc(2026, 3, 29, 1), instant);
    }

    [Fact]
    public void An_ambiguous_local_time_resolves_earlier_for_starts_and_later_for_ends()
    {
        var date = new DateOnly(2026, 10, 25);
        var time = new TimeOnly(1, 30);

        Assert.Equal(Utc(2026, 10, 25, 0, 30), ClinicTime.ToUtc(date, time, London));
        Assert.Equal(Utc(2026, 10, 25, 1, 30), ClinicTime.ToUtc(date, time, London, isRangeEnd: true));
    }
}
