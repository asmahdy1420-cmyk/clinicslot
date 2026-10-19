namespace ClinicSlot.Domain.Scheduling;

/// <summary>Converts clinic wall-clock times to instants, resolving DST gaps and overlaps.</summary>
public static class ClinicTime
{
    /// <summary>
    /// Resolves a local date and time in <paramref name="zone"/> to a UTC instant.
    /// A time that does not exist (spring-forward gap) moves forward to the first valid
    /// minute after the gap. A time that occurs twice (fall-back overlap) resolves to the
    /// earlier instant for a range start and the later instant for a range end, so a
    /// working window never shrinks because of a transition.
    /// </summary>
    public static DateTimeOffset ToUtc(DateOnly date, TimeOnly time, TimeZoneInfo zone, bool isRangeEnd = false)
    {
        var local = date.ToDateTime(time, DateTimeKind.Unspecified);

        // Gaps are at most a few hours, so walking minute by minute is cheap and exact.
        while (zone.IsInvalidTime(local))
        {
            local = local.AddMinutes(1);
        }

        TimeSpan offset;
        if (zone.IsAmbiguousTime(local))
        {
            var offsets = zone.GetAmbiguousTimeOffsets(local);
            offset = isRangeEnd ? offsets.Min() : offsets.Max();
        }
        else
        {
            offset = zone.GetUtcOffset(local);
        }

        return new DateTimeOffset(local, offset).ToUniversalTime();
    }

    public static DateTimeOffset ToLocal(DateTimeOffset instant, TimeZoneInfo zone) =>
        TimeZoneInfo.ConvertTime(instant, zone);

    public static DateOnly LocalDate(DateTimeOffset instant, TimeZoneInfo zone) =>
        DateOnly.FromDateTime(ToLocal(instant, zone).DateTime);
}
