using ClinicSlot.Domain.Appointments;
using ClinicSlot.Domain.Clinics;
using ClinicSlot.Domain.Patients;
using ClinicSlot.Domain.Practitioners;

namespace ClinicSlot.Application.Abstractions;

public interface IClinicRepository
{
    Task<Clinic?> FindAsync(Guid id, CancellationToken cancellationToken);

    void Add(Clinic clinic);
}

public interface IClinicServiceRepository
{
    Task<ClinicService?> FindAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<ClinicService>> ListByClinicAsync(Guid clinicId, CancellationToken cancellationToken);

    void Add(ClinicService service);
}

public interface IPractitionerRepository
{
    Task<Practitioner?> FindAsync(Guid id, CancellationToken cancellationToken);

    void Add(Practitioner practitioner);
}

public interface ITimeOffRepository
{
    Task<IReadOnlyList<TimeOff>> ListOverlappingAsync(
        Guid practitionerId,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        CancellationToken cancellationToken);

    void Add(TimeOff timeOff);
}

public interface IPatientRepository
{
    Task<Patient?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken);

    void Add(Patient patient);
}

public interface IAppointmentRepository
{
    /// <summary>Loads an appointment together with its slot claims.</summary>
    Task<Appointment?> FindAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Booked appointments whose blocked interval (duration plus buffer) overlaps the range.</summary>
    Task<IReadOnlyList<Appointment>> ListBlockingAsync(
        Guid practitionerId,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        Guid? excludeAppointmentId,
        CancellationToken cancellationToken);

    void Add(Appointment appointment);
}

public interface IUnitOfWork
{
    /// <summary>
    /// Persists all pending changes atomically. Implementations translate storage-level
    /// uniqueness and concurrency failures into <see cref="Exceptions.ConflictException"/>.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
