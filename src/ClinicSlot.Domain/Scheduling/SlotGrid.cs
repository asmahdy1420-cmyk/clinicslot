namespace ClinicSlot.Domain.Scheduling;

/// <summary>
/// The fixed five-minute grid every schedule boundary, duration and booking sits on.
/// Because all intervals are aligned to it, two intervals overlap exactly when they
/// share at least one grid unit, which is what the slot-claim ledger relies on.
/// </summary>
public static class SlotGrid
{
    public const int UnitMinutes = 5;

    public static readonly TimeSpan Unit = TimeSpan.FromMinutes(UnitMinutes);

    public static bool IsAligned(TimeSpan span) => span.Ticks % Unit.Ticks == 0;

    public static bool IsAligned(TimeOnly time) => time.Ticks % Unit.Ticks == 0;

    public static bool IsAligned(DateTimeOffset instant) => instant.UtcTicks % Unit.Ticks == 0;

    public static IEnumerable<DateTimeOffset> Units(UtcInterval interval)
    {
        for (var cursor = interval.Start; cursor < interval.End; cursor += Unit)
        {
            yield return cursor;
        }
    }
}
