using ClinicSlot.Domain.Clinics;
using ClinicSlot.Domain.Patients;
using ClinicSlot.Domain.Practitioners;
using ClinicSlot.Infrastructure.Persistence.Conversions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicSlot.Infrastructure.Persistence.Configurations;

internal sealed class ClinicConfiguration : IEntityTypeConfiguration<Clinic>
{
    public void Configure(EntityTypeBuilder<Clinic> builder)
    {
        builder.ToTable("Clinics");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();
        builder.Property(c => c.Name).HasMaxLength(200).IsRequired();
        builder.Property(c => c.TimeZoneId).HasMaxLength(64).IsRequired();
    }
}

internal sealed class ClinicServiceConfiguration : IEntityTypeConfiguration<ClinicService>
{
    public void Configure(EntityTypeBuilder<ClinicService> builder)
    {
        builder.ToTable("Services");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();
        builder.Property(s => s.Name).HasMaxLength(200).IsRequired();
        builder.HasOne<Clinic>().WithMany().HasForeignKey(s => s.ClinicId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class PractitionerConfiguration : IEntityTypeConfiguration<Practitioner>
{
    public void Configure(EntityTypeBuilder<Practitioner> builder)
    {
        builder.ToTable("Practitioners");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();
        builder.Property(p => p.Name).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Schedule)
            .HasColumnName("WeeklyScheduleJson")
            .HasConversion(new WeeklyScheduleConverter(), new WeeklyScheduleComparer())
            .IsRequired();
        builder.HasOne<Clinic>().WithMany().HasForeignKey(p => p.ClinicId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class TimeOffConfiguration : IEntityTypeConfiguration<TimeOff>
{
    public void Configure(EntityTypeBuilder<TimeOff> builder)
    {
        builder.ToTable("TimeOffs");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();
        builder.Property(t => t.Reason).HasMaxLength(500);
        builder.HasIndex(t => new { t.PractitionerId, t.StartUtc });
        builder.HasOne<Practitioner>().WithMany().HasForeignKey(t => t.PractitionerId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.ToTable("Patients");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();
        builder.Property(p => p.FullName).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Email).HasMaxLength(254).IsRequired();
        builder.Property(p => p.Phone).HasMaxLength(32);
        builder.HasIndex(p => p.Email).IsUnique();
    }
}
