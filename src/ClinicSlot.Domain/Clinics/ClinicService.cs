using ClinicSlot.Domain.Common;
using ClinicSlot.Domain.Scheduling;

namespace ClinicSlot.Domain.Clinics;

/// <summary>Something a clinic offers, such as "Initial consultation, 30 minutes".</summary>
public sealed class ClinicService
{
    public const int MaxDurationMinutes = 480;
    public const int MaxBufferMinutes = 120;

    private ClinicService()
    {
        Name = string.Empty;
    }

    public Guid Id { get; private set; }

    public Guid ClinicId { get; private set; }

    public string Name { get; private set; }

    public int DurationMinutes { get; private set; }

    /// <summary>Clean-up or notes time kept free after each appointment of this service.</summary>
    public int BufferMinutes { get; private set; }

    public TimeSpan Duration => TimeSpan.FromMinutes(DurationMinutes);

    public TimeSpan Buffer => TimeSpan.FromMinutes(BufferMinutes);

    public static ClinicService Create(Guid clinicId, string name, int durationMinutes, int bufferMinutes)
    {
        Guard.NotEmpty(clinicId, nameof(clinicId));

        if (durationMinutes is < SlotGrid.UnitMinutes or > MaxDurationMinutes
            || !SlotGrid.IsAligned(TimeSpan.FromMinutes(durationMinutes)))
        {
            throw new DomainValidationException(
                $"Duration must be between {SlotGrid.UnitMinutes} and {MaxDurationMinutes} minutes and a multiple of {SlotGrid.UnitMinutes}.");
        }

        if (bufferMinutes is < 0 or > MaxBufferMinutes || !SlotGrid.IsAligned(TimeSpan.FromMinutes(bufferMinutes)))
        {
            throw new DomainValidationException(
                $"Buffer must be between 0 and {MaxBufferMinutes} minutes and a multiple of {SlotGrid.UnitMinutes}.");
        }

        return new ClinicService
        {
            Id = Guid.NewGuid(),
            ClinicId = clinicId,
            Name = Guard.NotBlank(name, "Name"),
            DurationMinutes = durationMinutes,
            BufferMinutes = bufferMinutes,
        };
    }
}
