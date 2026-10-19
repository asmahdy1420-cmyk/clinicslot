using ClinicSlot.Domain.Appointments;
using ClinicSlot.Domain.Clinics;
using ClinicSlot.Domain.Practitioners;
using ClinicSlot.Domain.Scheduling;

namespace ClinicSlot.Application.Contracts;

internal static class Mappings
{
    public static ClinicResponse ToResponse(this Clinic clinic) =>
        new(clinic.Id, clinic.Name, clinic.TimeZoneId, clinic.SlotIntervalMinutes, clinic.ChangeCutoffHours);

    public static ServiceResponse ToResponse(this ClinicService service) =>
        new(service.Id, service.ClinicId, service.Name, service.DurationMinutes, service.BufferMinutes);

    public static PractitionerResponse ToResponse(this Practitioner practitioner) =>
        new(practitioner.Id, practitioner.ClinicId, practitioner.Name, practitioner.Schedule.ToDto());

    public static TimeOffResponse ToResponse(this TimeOff timeOff) =>
        new(timeOff.Id, timeOff.PractitionerId, timeOff.StartUtc, timeOff.EndUtc, timeOff.Reason);

    public static AppointmentResponse ToResponse(this Appointment appointment, TimeZoneInfo zone) =>
        new(
            appointment.Id,
            appointment.ClinicId,
            appointment.PractitionerId,
            appointment.ServiceId,
            appointment.PatientId,
            ClinicTime.ToLocal(appointment.StartUtc, zone),
            ClinicTime.ToLocal(appointment.EndUtc, zone),
            appointment.Status.ToString(),
            appointment.Version);

    public static IReadOnlyList<WorkingDayDto> ToDto(this WeeklySchedule schedule) =>
        schedule.Days
            .Select(day => new WorkingDayDto(
                day.Day,
                day.Hours.Start,
                day.Hours.End,
                day.Breaks.Select(b => new BreakDto(b.Start, b.End)).ToList()))
            .ToList();

    public static WeeklySchedule ToSchedule(this IEnumerable<WorkingDayDto> days) =>
        new(days.Select(day => new WorkingDay(
            day.Day,
            day.Start,
            day.End,
            (day.Breaks ?? []).Select(b => new LocalTimeRange(b.Start, b.End)))));
}
