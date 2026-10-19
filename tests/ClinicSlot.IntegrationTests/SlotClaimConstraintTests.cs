using ClinicSlot.Application.Abstractions;
using ClinicSlot.Application.Exceptions;
using ClinicSlot.Domain.Appointments;
using ClinicSlot.Domain.Clinics;
using ClinicSlot.Domain.Patients;
using ClinicSlot.Domain.Practitioners;
using ClinicSlot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ClinicSlot.IntegrationTests;

/// <summary>
/// Skips the application-level overlap check entirely and writes straight through the
/// persistence layer, proving the slot-claim key alone prevents double booking.
/// </summary>
public class SlotClaimConstraintTests(ClinicSlotApiFactory factory) : IClassFixture<ClinicSlotApiFactory>
{
    private static readonly DateTimeOffset Start = new(2026, 3, 11, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Database_rejects_overlapping_claims_even_without_the_precheck()
    {
        var (clinic, practitioner, service) = await SeedAsync();

        await SaveAsync(Book(clinic, practitioner, service, Start, "first@example.com"));

        // Starts 20 minutes later: overlaps the first appointment's last 10 minutes and buffer.
        var overlapping = Book(clinic, practitioner, service, Start.AddMinutes(20), "second@example.com");
        await Assert.ThrowsAsync<SlotConflictException>(() => SaveAsync(overlapping));

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ClinicSlotDbContext>();
        Assert.Equal(1, await db.Appointments.CountAsync(a => a.PractitionerId == practitioner.Id));
    }

    private static (Appointment Appointment, Patient Patient) Book(
        Clinic clinic, Practitioner practitioner, ClinicService service, DateTimeOffset start, string email)
    {
        var patient = Patient.Create("Test Patient", email, null);
        var appointment = Appointment.Book(
            clinic.Id, practitioner.Id, service.Id, patient.Id, start, service.Duration, service.Buffer, ClinicSlotApiFactory.Now);
        return (appointment, patient);
    }

    private async Task SaveAsync((Appointment Appointment, Patient Patient) booking)
    {
        using var scope = factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<IPatientRepository>().Add(booking.Patient);
        scope.ServiceProvider.GetRequiredService<IAppointmentRepository>().Add(booking.Appointment);
        await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(CancellationToken.None);
    }

    private async Task<(Clinic, Practitioner, ClinicService)> SeedAsync()
    {
        var clinic = Clinic.Create("Constraint Clinic", "Europe/London", 15, 24);
        var practitioner = Practitioner.Create(
            clinic.Id,
            "Dr Test",
            new WeeklySchedule([new WorkingDay(DayOfWeek.Wednesday, new TimeOnly(9, 0), new TimeOnly(17, 0))]));
        var service = ClinicService.Create(clinic.Id, "Assessment", 30, 10);

        using var scope = factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<IClinicRepository>().Add(clinic);
        scope.ServiceProvider.GetRequiredService<IPractitionerRepository>().Add(practitioner);
        scope.ServiceProvider.GetRequiredService<IClinicServiceRepository>().Add(service);
        await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(CancellationToken.None);
        return (clinic, practitioner, service);
    }
}
