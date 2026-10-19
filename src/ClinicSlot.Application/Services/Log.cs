using Microsoft.Extensions.Logging;

namespace ClinicSlot.Application.Services;

internal static partial class Log
{
    [LoggerMessage(EventId = 1000, Level = LogLevel.Information,
        Message = "Booked appointment {AppointmentId} with practitioner {PractitionerId} from {StartUtc:O} to {EndUtc:O}")]
    public static partial void AppointmentBooked(ILogger logger, Guid appointmentId, Guid practitionerId, DateTimeOffset startUtc, DateTimeOffset endUtc);

    [LoggerMessage(EventId = 1001, Level = LogLevel.Information,
        Message = "Rescheduled appointment {AppointmentId} to {StartUtc:O}")]
    public static partial void AppointmentRescheduled(ILogger logger, Guid appointmentId, DateTimeOffset startUtc);

    [LoggerMessage(EventId = 1002, Level = LogLevel.Information,
        Message = "Cancelled appointment {AppointmentId}")]
    public static partial void AppointmentCancelled(ILogger logger, Guid appointmentId);

    [LoggerMessage(EventId = 1003, Level = LogLevel.Warning,
        Message = "Slot conflict for practitioner {PractitionerId} at {StartUtc:O}")]
    public static partial void SlotConflict(ILogger logger, Guid practitionerId, DateTimeOffset startUtc);

    [LoggerMessage(EventId = 1100, Level = LogLevel.Information,
        Message = "Created clinic {ClinicId} in time zone {TimeZone}")]
    public static partial void ClinicCreated(ILogger logger, Guid clinicId, string timeZone);
}
