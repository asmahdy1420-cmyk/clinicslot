using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ClinicSlot.Infrastructure.Persistence.Conversions;

/// <summary>
/// Stores instants as UTC ticks. SQLite has no native timestamp type and EF cannot compare
/// DateTimeOffset text columns, while an integer keeps ordering, range queries and the
/// slot-claim primary key exact and cheap.
/// </summary>
internal sealed class UtcTicksConverter() : ValueConverter<DateTimeOffset, long>(
    value => value.UtcTicks,
    ticks => new DateTimeOffset(ticks, TimeSpan.Zero));
