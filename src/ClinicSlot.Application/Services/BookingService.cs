using ClinicSlot.Application.Abstractions;
using ClinicSlot.Application.Contracts;
using ClinicSlot.Application.Exceptions;
using ClinicSlot.Domain.Appointments;
using ClinicSlot.Domain.Patients;
using Microsoft.Extensions.Logging;

namespace ClinicSlot.Application.Services;

public sealed class BookingService(
    SchedulingContext context,
    IAppointmentRepository appointments,
    IPatientRepository patients,
    IClinicRepository clinics,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<BookingService> logger)
{
    public async Task<AppointmentResponse> BookAsync(BookAppointmentRequest request, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var (clinic, practitioner) = await context.LoadPractitionerAsync(request.PractitionerId, cancellationToken);
        var service = await context.LoadServiceAsync(request.ServiceId, clinic, cancellationToken);
        var start = request.Start.ToUniversalTime();

        try
        {
            await context.EnsureBookableAsync(
                clinic, practitioner, start, service.Duration, service.Buffer, now,
                excludeAppointmentId: null, cancellationToken);

            var patient = await FindOrCreatePatientAsync(request.Patient, cancellationToken);
            var appointment = Appointment.Book(
                clinic.Id, practitioner.Id, service.Id, patient.Id, start, service.Duration, service.Buffer, now);

            appointments.Add(appointment);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            Log.AppointmentBooked(logger, appointment.Id, practitioner.Id, appointment.StartUtc, appointment.EndUtc);
            return appointment.ToResponse(clinic.GetTimeZone());
        }
        catch (SlotConflictException)
        {
            Log.SlotConflict(logger, practitioner.Id, start);
            throw;
        }
    }

    public async Task<AppointmentResponse> GetAsync(Guid appointmentId, CancellationToken cancellationToken)
    {
        var appointment = await RequireAsync(appointmentId, cancellationToken);
        var clinic = await clinics.FindAsync(appointment.ClinicId, cancellationToken)
            ?? throw new NotFoundException("Clinic", appointment.ClinicId);
        return appointment.ToResponse(clinic.GetTimeZone());
    }

    public async Task<AppointmentResponse> CancelAsync(Guid appointmentId, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var appointment = await RequireAsync(appointmentId, cancellationToken);
        var clinic = await clinics.FindAsync(appointment.ClinicId, cancellationToken)
            ?? throw new NotFoundException("Clinic", appointment.ClinicId);

        appointment.Cancel(now, clinic.ChangeCutoff);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        Log.AppointmentCancelled(logger, appointment.Id);
        return appointment.ToResponse(clinic.GetTimeZone());
    }

    public async Task<AppointmentResponse> RescheduleAsync(
        Guid appointmentId,
        RescheduleAppointmentRequest request,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var appointment = await RequireAsync(appointmentId, cancellationToken);
        var (clinic, practitioner) = await context.LoadPractitionerAsync(appointment.PractitionerId, cancellationToken);
        var start = request.Start.ToUniversalTime();

        // Rule order matters: a locked appointment reports the cutoff, not a slot problem.
        appointment.EnsureCanChange(now, clinic.ChangeCutoff);

        try
        {
            await context.EnsureBookableAsync(
                clinic, practitioner, start, appointment.Duration, appointment.Buffer, now,
                excludeAppointmentId: appointment.Id, cancellationToken);

            appointment.Reschedule(start, now, clinic.ChangeCutoff);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (SlotConflictException)
        {
            Log.SlotConflict(logger, practitioner.Id, start);
            throw;
        }

        Log.AppointmentRescheduled(logger, appointment.Id, appointment.StartUtc);
        return appointment.ToResponse(clinic.GetTimeZone());
    }

    private async Task<Appointment> RequireAsync(Guid appointmentId, CancellationToken cancellationToken) =>
        await appointments.FindAsync(appointmentId, cancellationToken)
        ?? throw new NotFoundException("Appointment", appointmentId);

    private async Task<Patient> FindOrCreatePatientAsync(PatientDto dto, CancellationToken cancellationToken)
    {
        var email = Patient.NormalizeEmail(dto.Email);
        var existing = await patients.FindByEmailAsync(email, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var patient = Patient.Create(dto.FullName, email, dto.Phone);
        patients.Add(patient);
        return patient;
    }
}
