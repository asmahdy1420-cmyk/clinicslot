namespace ClinicSlot.UnitTests;

internal static class TestZones
{
    /// <summary>Observes DST: GMT (+00:00) in winter, BST (+01:00) from the last Sunday of March.</summary>
    public static TimeZoneInfo London { get; } = TimeZoneInfo.FindSystemTimeZoneById("Europe/London");

    public static DateTimeOffset Utc(int year, int month, int day, int hour, int minute = 0) =>
        new(year, month, day, hour, minute, 0, TimeSpan.Zero);
}
