using ClinicSlot.Domain.Appointments;
using ClinicSlot.Domain.Clinics;
using ClinicSlot.Domain.Patients;
using ClinicSlot.Domain.Practitioners;
using ClinicSlot.Infrastructure.Persistence.Conversions;
using Microsoft.EntityFrameworkCore;

namespace ClinicSlot.Infrastructure.Persistence;

public sealed class ClinicSlotDbContext(DbContextOptions<ClinicSlotDbContext> options) : DbContext(options)
{
    public DbSet<Clinic> Clinics => Set<Clinic>();

    public DbSet<ClinicService> Services => Set<ClinicService>();

    public DbSet<Practitioner> Practitioners => Set<Practitioner>();

    public DbSet<TimeOff> TimeOffs => Set<TimeOff>();

    public DbSet<Patient> Patients => Set<Patient>();

    public DbSet<Appointment> Appointments => Set<Appointment>();

    public DbSet<SlotClaim> SlotClaims => Set<SlotClaim>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTimeOffset>().HaveConversion<UtcTicksConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ClinicSlotDbContext).Assembly);
    }
}
