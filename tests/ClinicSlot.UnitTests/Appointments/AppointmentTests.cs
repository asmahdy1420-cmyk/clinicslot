using ClinicSlot.Domain.Appointments;
using ClinicSlot.Domain.Common;
using static ClinicSlot.UnitTests.TestZones;

namespace ClinicSlot.UnitTests.Appointments;

public class AppointmentTests
{
    private static readonly DateTimeOffset Now = Utc(2026, 3, 2, 8);
    private static readonly TimeSpan Cutoff = TimeSpan.FromHours(24);

    [Fact]
    public void Booking_claims_every_five_minute_unit_of_duration_plus_buffer()
    {
        var appointment = Book(Utc(2026, 3, 10, 9), durationMinutes: 30, bufferMinutes: 10);

        Assert.Equal(8, appointment.Claims.Count);
        Assert.Equal(Utc(2026, 3, 10, 9), appointment.Claims.Min(c => c.SlotStartUtc));
        Assert.Equal(Utc(2026, 3, 10, 9, 35), appointment.Claims.Max(c => c.SlotStartUtc));
        Assert.Equal(Utc(2026, 3, 10, 9, 40), appointment.BlockedUntilUtc);
    }

    [Fact]
    public void Booking_rejects_a_start_off_the_five_minute_grid()
    {
        Assert.Throws<DomainValidationException>(() => Book(Utc(2026, 3, 10, 9, 7)));
    }

    [Fact]
    public void Booking_rejects_a_start_in_the_past()
    {
        Assert.Throws<DomainRuleException>(() => Book(Now.AddHours(-1)));
    }

    [Fact]
    public void Cancelling_outside_the_cutoff_releases_all_claims()
    {
        var appointment = Book(Now.AddHours(25));

        appointment.Cancel(Now, Cutoff);

        Assert.Equal(AppointmentStatus.Cancelled, appointment.Status);
        Assert.Empty(appointment.Claims);
        Assert.Equal(2, appointment.Version);
    }

    [Fact]
    public void Cancelling_inside_the_cutoff_is_refused()
    {
        var appointment = Book(Now.AddHours(23));

        var error = Assert.Throws<DomainRuleException>(() => appointment.Cancel(Now, Cutoff));

        Assert.Contains("24 hours", error.Message, StringComparison.Ordinal);
        Assert.Equal(AppointmentStatus.Booked, appointment.Status);
    }

    [Fact]
    public void Rescheduling_inside_the_cutoff_is_refused()
    {
        var appointment = Book(Now.AddHours(2));

        Assert.Throws<DomainRuleException>(() => appointment.Reschedule(Now.AddDays(3), Now, Cutoff));
    }

    [Fact]
    public void Rescheduling_onto_an_overlapping_time_keeps_shared_claims_instead_of_reinserting_them()
    {
        var appointment = Book(Utc(2026, 3, 10, 9), durationMinutes: 30);
        var shared = appointment.Claims.Single(c => c.SlotStartUtc == Utc(2026, 3, 10, 9, 15));

        appointment.Reschedule(Utc(2026, 3, 10, 9, 15), Now, Cutoff);

        Assert.Equal(6, appointment.Claims.Count);
        Assert.Equal(Utc(2026, 3, 10, 9, 15), appointment.Claims.Min(c => c.SlotStartUtc));
        Assert.Same(shared, appointment.Claims.Single(c => c.SlotStartUtc == Utc(2026, 3, 10, 9, 15)));
        Assert.Equal(Utc(2026, 3, 10, 9, 45), appointment.EndUtc);
    }

    [Fact]
    public void Cancelled_appointments_cannot_be_changed_again()
    {
        var appointment = Book(Now.AddDays(3));
        appointment.Cancel(Now, Cutoff);

        Assert.Throws<DomainRuleException>(() => appointment.Cancel(Now, Cutoff));
    }

    private static Appointment Book(DateTimeOffset start, int durationMinutes = 30, int bufferMinutes = 0) =>
        Appointment.Book(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            start,
            TimeSpan.FromMinutes(durationMinutes),
            TimeSpan.FromMinutes(bufferMinutes),
            Now);
}
