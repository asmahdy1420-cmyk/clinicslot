using System.Text.Json;
using ClinicSlot.Domain.Practitioners;
using ClinicSlot.Domain.Scheduling;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ClinicSlot.Infrastructure.Persistence.Conversions;

/// <summary>
/// Persists a weekly schedule as a small JSON document on the practitioner row. The schedule
/// is always read and replaced as a whole, so a child table would add joins without benefit.
/// Persistence records keep the JSON shape independent of the domain classes.
/// </summary>
internal static class WeeklyScheduleJson
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static string Serialize(WeeklySchedule schedule) =>
        JsonSerializer.Serialize(
            schedule.Days.Select(d => new DayRecord(
                (int)d.Day,
                d.Hours.Start,
                d.Hours.End,
                d.Breaks.Select(b => new RangeRecord(b.Start, b.End)).ToList())).ToList(),
            Options);

    public static WeeklySchedule Deserialize(string json)
    {
        var days = JsonSerializer.Deserialize<List<DayRecord>>(json, Options) ?? [];
        return new WeeklySchedule(days.Select(d => new WorkingDay(
            (DayOfWeek)d.Day,
            d.Start,
            d.End,
            d.Breaks.Select(b => new LocalTimeRange(b.Start, b.End)))));
    }

    private sealed record DayRecord(int Day, TimeOnly Start, TimeOnly End, List<RangeRecord> Breaks);

    private sealed record RangeRecord(TimeOnly Start, TimeOnly End);
}

internal sealed class WeeklyScheduleConverter() : ValueConverter<WeeklySchedule, string>(
    schedule => WeeklyScheduleJson.Serialize(schedule),
    json => WeeklyScheduleJson.Deserialize(json));

internal sealed class WeeklyScheduleComparer() : ValueComparer<WeeklySchedule>(
    (left, right) => WeeklyScheduleJson.Serialize(left!) == WeeklyScheduleJson.Serialize(right!),
    schedule => WeeklyScheduleJson.Serialize(schedule).GetHashCode(StringComparison.Ordinal),
    schedule => WeeklyScheduleJson.Deserialize(WeeklyScheduleJson.Serialize(schedule)));
