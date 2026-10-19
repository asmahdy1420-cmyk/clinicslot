using ClinicSlot.Application.Abstractions;
using ClinicSlot.Domain.Appointments;
using ClinicSlot.Domain.Clinics;
using ClinicSlot.Domain.Patients;
using ClinicSlot.Domain.Practitioners;
using ClinicSlot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ClinicSlot.Infrastructure.Repositories;

internal sealed class ClinicRepository(ClinicSlotDbContext db) : IClinicRepository
{
    public Task<Clinic?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        db.Clinics.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public void Add(Clinic clinic) => db.Clinics.Add(clinic);
}

internal sealed class ClinicServiceRepository(ClinicSlotDbContext db) : IClinicServiceRepository
{
    public Task<ClinicService?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        db.Services.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public async Task<IReadOnlyList<ClinicService>> ListByClinicAsync(Guid clinicId, CancellationToken cancellationToken) =>
        await db.Services.AsNoTracking()
            .Where(s => s.ClinicId == clinicId)
            .OrderBy(s => s.Name)
            .ToListAsync(cancellationToken);

    public void Add(ClinicService service) => db.Services.Add(service);
}

internal sealed class PractitionerRepository(ClinicSlotDbContext db) : IPractitionerRepository
{
    public Task<Practitioner?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        db.Practitioners.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public void Add(Practitioner practitioner) => db.Practitioners.Add(practitioner);
}

internal sealed class TimeOffRepository(ClinicSlotDbContext db) : ITimeOffRepository
{
    public async Task<IReadOnlyList<TimeOff>> ListOverlappingAsync(
        Guid practitionerId,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        CancellationToken cancellationToken) =>
        await db.TimeOffs.AsNoTracking()
            .Where(t => t.PractitionerId == practitionerId && t.StartUtc < toUtc && t.EndUtc > fromUtc)
            .ToListAsync(cancellationToken);

    public void Add(TimeOff timeOff) => db.TimeOffs.Add(timeOff);
}

internal sealed class PatientRepository(ClinicSlotDbContext db) : IPatientRepository
{
    public Task<Patient?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken) =>
        db.Patients.FirstOrDefaultAsync(p => p.Email == normalizedEmail, cancellationToken);

    public void Add(Patient patient) => db.Patients.Add(patient);
}

internal sealed class AppointmentRepository(ClinicSlotDbContext db) : IAppointmentRepository
{
    public Task<Appointment?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        db.Appointments.Include(a => a.Claims).FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Appointment>> ListBlockingAsync(
        Guid practitionerId,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        Guid? excludeAppointmentId,
        CancellationToken cancellationToken) =>
        await db.Appointments.AsNoTracking()
            .Where(a => a.PractitionerId == practitionerId
                && a.Status == AppointmentStatus.Booked
                && a.StartUtc < toUtc
                && a.BlockedUntilUtc > fromUtc
                && (excludeAppointmentId == null || a.Id != excludeAppointmentId))
            .OrderBy(a => a.StartUtc)
            .ToListAsync(cancellationToken);

    public void Add(Appointment appointment) => db.Appointments.Add(appointment);
}
