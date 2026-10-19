using System.Net;
using ClinicSlot.Application.Contracts;

namespace ClinicSlot.IntegrationTests;

public class BookingApiTests(ClinicSlotApiFactory factory) : IClassFixture<ClinicSlotApiFactory>
{
    // Wednesday 11 March 2026, 09:00 in London (GMT, so also 09:00 UTC).
    private static readonly DateTimeOffset Wednesday0900 = new(2026, 3, 11, 9, 0, 0, TimeSpan.Zero);

    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Happy_path_from_availability_to_booking()
    {
        var scenario = await _client.SeedAsync();

        var before = await _client.AvailabilityAsync(scenario, "2026-03-11", "2026-03-11");
        var day = Assert.Single(before.Days);
        Assert.Equal(Wednesday0900, day.Slots[0].Start);
        Assert.Equal("Europe/London", before.TimeZone);

        var booked = await _client.BookAsync(scenario, day.Slots[0].Start);
        Assert.Equal(HttpStatusCode.Created, booked.StatusCode);
        var appointment = await booked.ReadAsync<AppointmentResponse>();
        Assert.Equal($"/api/appointments/{appointment.Id}", booked.Headers.Location?.OriginalString);
        Assert.Equal("Booked", appointment.Status);
        Assert.Equal(Wednesday0900.AddMinutes(30), appointment.End);

        var fetched = await (await _client.GetAsync($"/api/appointments/{appointment.Id}")).ReadAsync<AppointmentResponse>();
        Assert.Equal(appointment, fetched);

        // 09:00 + 30 minutes + 10 minute buffer blocks until 09:40, so the next 15-minute slot is 09:45.
        var after = await _client.AvailabilityAsync(scenario, "2026-03-11", "2026-03-11");
        Assert.Equal(Wednesday0900.AddMinutes(45), after.Days[0].Slots[0].Start);
    }

    [Fact]
    public async Task Booking_the_same_slot_twice_returns_409_problem_details()
    {
        var scenario = await _client.SeedAsync();
        Assert.Equal(HttpStatusCode.Created, (await _client.BookAsync(scenario, Wednesday0900)).StatusCode);

        var second = await _client.BookAsync(scenario, Wednesday0900, "omar@example.com");

        var problem = await second.ReadProblemAsync(HttpStatusCode.Conflict);
        Assert.Equal("Slot unavailable", problem.Title);
        Assert.Equal(409, problem.Status);
    }

    [Fact]
    public async Task Booking_inside_another_appointments_buffer_returns_409()
    {
        var scenario = await _client.SeedAsync();
        Assert.Equal(HttpStatusCode.Created, (await _client.BookAsync(scenario, Wednesday0900)).StatusCode);

        // 09:30 is free on the schedule but still inside the 10 minute buffer of the 09:00 booking.
        var overlapping = await _client.BookAsync(scenario, Wednesday0900.AddMinutes(30), "omar@example.com");

        await overlapping.ReadProblemAsync(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Concurrent_requests_for_one_slot_produce_exactly_one_booking()
    {
        var scenario = await _client.SeedAsync();
        var start = Wednesday0900.AddHours(2);

        var responses = await Task.WhenAll(Enumerable.Range(0, 8).Select(i =>
            factory.CreateClient().BookAsync(scenario, start, $"patient{i}@example.com")));

        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Created);
        Assert.All(
            responses.Where(r => r.StatusCode != HttpStatusCode.Created),
            r => Assert.Equal(HttpStatusCode.Conflict, r.StatusCode));
    }

    [Fact]
    public async Task Booking_during_the_lunch_break_returns_422()
    {
        var scenario = await _client.SeedAsync();

        var response = await _client.BookAsync(scenario, new DateTimeOffset(2026, 3, 11, 12, 15, 0, TimeSpan.Zero));

        var problem = await response.ReadProblemAsync(HttpStatusCode.UnprocessableEntity);
        Assert.Contains("not offered", problem.Detail, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Cancelling_inside_the_cutoff_window_returns_422()
    {
        var scenario = await _client.SeedAsync();
        // Monday 2 March 14:00 is six hours after "now", inside the 24 hour cutoff.
        var soon = await (await _client.BookAsync(scenario, ClinicSlotApiFactory.Now.AddHours(6))).ReadAsync<AppointmentResponse>();

        var response = await _client.PostAsync($"/api/appointments/{soon.Id}/cancel", null);

        var problem = await response.ReadProblemAsync(HttpStatusCode.UnprocessableEntity);
        Assert.Contains("24 hours", problem.Detail, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Cancelling_outside_the_cutoff_frees_the_slot()
    {
        var scenario = await _client.SeedAsync();
        var appointment = await (await _client.BookAsync(scenario, Wednesday0900)).ReadAsync<AppointmentResponse>();

        var cancelled = await (await _client.PostAsync($"/api/appointments/{appointment.Id}/cancel", null))
            .ReadAsync<AppointmentResponse>();

        Assert.Equal("Cancelled", cancelled.Status);
        Assert.Equal(HttpStatusCode.Created, (await _client.BookAsync(scenario, Wednesday0900, "omar@example.com")).StatusCode);
    }

    [Fact]
    public async Task Rescheduling_onto_an_overlapping_time_succeeds_and_releases_the_old_time()
    {
        var scenario = await _client.SeedAsync();
        var appointment = await (await _client.BookAsync(scenario, Wednesday0900)).ReadAsync<AppointmentResponse>();

        var response = await _client.PostAsync(
            $"/api/appointments/{appointment.Id}/reschedule", new RescheduleAppointmentRequest(Wednesday0900.AddMinutes(15)));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var moved = await response.ReadAsync<AppointmentResponse>();
        Assert.Equal(Wednesday0900.AddMinutes(15), moved.Start);
        Assert.Equal(appointment.Version + 1, moved.Version);

        var availability = await _client.AvailabilityAsync(scenario, "2026-03-11", "2026-03-11");
        Assert.DoesNotContain(availability.Days[0].Slots, s => s.Start == Wednesday0900);
        Assert.Contains(availability.Days[0].Slots, s => s.Start == Wednesday0900.AddHours(1));
    }

    [Fact]
    public async Task Rescheduling_into_another_booking_returns_409()
    {
        var scenario = await _client.SeedAsync();
        var first = await (await _client.BookAsync(scenario, Wednesday0900)).ReadAsync<AppointmentResponse>();
        Assert.Equal(HttpStatusCode.Created, (await _client.BookAsync(scenario, Wednesday0900.AddHours(1), "omar@example.com")).StatusCode);

        var response = await _client.PostAsync(
            $"/api/appointments/{first.Id}/reschedule", new RescheduleAppointmentRequest(Wednesday0900.AddMinutes(45)));

        await response.ReadProblemAsync(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Unknown_appointment_returns_404_problem_details()
    {
        var response = await _client.GetAsync($"/api/appointments/{Guid.NewGuid()}");

        await response.ReadProblemAsync(HttpStatusCode.NotFound);
    }
}
