using ClinicSlot.Domain.Appointments;
using ClinicSlot.Domain.Clinics;
using ClinicSlot.Domain.Patients;
using ClinicSlot.Domain.Practitioners;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicSlot.Infrastructure.Persistence.Configurations;

internal sealed class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.ToTable("Appointments");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();
        builder.Property(a => a.Status).HasConversion<string>().HasMaxLength(16);
        builder.Property(a => a.Version).IsConcurrencyToken();

        builder.HasIndex(a => new { a.PractitionerId, a.StartUtc });

        builder.HasOne<Clinic>().WithMany().HasForeignKey(a => a.ClinicId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Practitioner>().WithMany().HasForeignKey(a => a.PractitionerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ClinicService>().WithMany().HasForeignKey(a => a.ServiceId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Patient>().WithMany().HasForeignKey(a => a.PatientId).OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(a => a.Claims)
            .WithOne()
            .HasForeignKey(c => c.AppointmentId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(a => a.Claims).HasField("_claims").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

/// <summary>
/// The double-booking guarantee: the primary key (PractitionerId, SlotStartUtc) means two
/// appointments can never hold the same five-minute unit, regardless of isolation level or
/// how many API instances race on the same slot.
/// </summary>
internal sealed class SlotClaimConfiguration : IEntityTypeConfiguration<SlotClaim>
{
    public void Configure(EntityTypeBuilder<SlotClaim> builder)
    {
        builder.ToTable("SlotClaims");
        builder.HasKey(c => new { c.PractitionerId, c.SlotStartUtc });
        builder.HasIndex(c => c.AppointmentId);
    }
}
