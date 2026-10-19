using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClinicSlot.Application.Contracts;

namespace ClinicSlot.IntegrationTests;

public class ScheduleApiTests(ClinicSlotApiFactory factory) : IClassFixture<ClinicSlotApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Availability_keeps_local_opening_time_across_the_dst_change()
    {
        var scenario = await _client.SeedAsync();

        var availability = await _client.AvailabilityAsync(scenario, "2026-03-27", "2026-03-30");

        // Friday is GMT, the weekend has no hours, Monday is BST: both open at 09:00 local.
        Assert.Equal(
            [new DateOnly(2026, 3, 27), new DateOnly(2026, 3, 30)],
            availability.Days.Select(d => d.Date).ToArray());
        Assert.Equal(new DateTimeOffset(2026, 3, 27, 9, 0, 0, TimeSpan.Zero), availability.Days[0].Slots[0].Start);
        Assert.Equal(new DateTimeOffset(2026, 3, 30, 9, 0, 0, TimeSpan.FromHours(1)), availability.Days[1].Slots[0].Start);
        Assert.Equal(TimeSpan.FromHours(1), availability.Days[1].Slots[0].Start.Offset);
    }

    [Fact]
    public async Task Time_off_removes_slots_from_availability()
    {
        var scenario = await _client.SeedAsync();
        var off = await _client.PostAsync(
            $"/api/practitioners/{scenario.Practitioner.Id}/time-off",
            new CreateTimeOffRequest(
                new DateTimeOffset(2026, 3, 12, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 3, 13, 0, 0, 0, TimeSpan.Zero),
                "Conference"));
        Assert.Equal(HttpStatusCode.Created, off.StatusCode);

        var availability = await _client.AvailabilityAsync(scenario, "2026-03-11", "2026-03-13");

        Assert.Equal(
            [new DateOnly(2026, 3, 11), new DateOnly(2026, 3, 13)],
            availability.Days.Select(d => d.Date).ToArray());
    }

    [Fact]
    public async Task Invalid_booking_request_returns_validation_problem_details()
    {
        var response = await _client.PostAsync(
            "/api/appointments",
            new BookAppointmentRequest(Guid.Empty, Guid.NewGuid(), default, new PatientDto("", "not-an-email")));

        var problem = await response.ReadProblemAsync(HttpStatusCode.BadRequest);
        Assert.Equal(400, problem.Status);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var errors = body.RootElement.GetProperty("errors");
        Assert.True(errors.TryGetProperty("PractitionerId", out _));
        Assert.True(errors.TryGetProperty("Start", out _));
        Assert.True(errors.TryGetProperty("Patient.FullName", out _));
        Assert.True(errors.TryGetProperty("Patient.Email", out _));
    }

    [Fact]
    public async Task Unknown_time_zone_is_rejected_with_400()
    {
        var response = await _client.PostAsync("/api/clinics", new CreateClinicRequest("Nowhere Clinic", "Mars/Olympus_Mons"));

        await response.ReadProblemAsync(HttpStatusCode.BadRequest);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(body.RootElement.GetProperty("errors").TryGetProperty("TimeZone", out _));
    }

    [Fact]
    public async Task Overlapping_breaks_are_rejected_with_400()
    {
        var scenario = await _client.SeedAsync();
        var day = new WorkingDayDto(
            DayOfWeek.Monday,
            new TimeOnly(9, 0),
            new TimeOnly(17, 0),
            [new BreakDto(new TimeOnly(12, 0), new TimeOnly(13, 0)), new BreakDto(new TimeOnly(12, 30), new TimeOnly(13, 30))]);

        var response = await _client.PutAsJsonAsync(
            $"/api/practitioners/{scenario.Practitioner.Id}/schedule", new ReplaceScheduleRequest([day]), ApiClient.Json);

        var problem = await response.ReadProblemAsync(HttpStatusCode.BadRequest);
        Assert.Contains("overlap", problem.Detail, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Availability_range_longer_than_31_days_is_rejected()
    {
        var scenario = await _client.SeedAsync();

        var response = await _client.GetAsync(
            $"/api/practitioners/{scenario.Practitioner.Id}/availability?serviceId={scenario.Service.Id}&from=2026-03-01&to=2026-05-01");

        await response.ReadProblemAsync(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Malformed_json_returns_problem_details_naming_the_bad_value()
    {
        var scenario = await _client.SeedAsync();
        using var content = new StringContent(
            """{"workingHours":[{"day":"Monday","start":"9am","end":"17:00"}]}""",
            System.Text.Encoding.UTF8,
            "application/json");

        var response = await _client.PutAsync($"/api/practitioners/{scenario.Practitioner.Id}/schedule", content);

        var problem = await response.ReadProblemAsync(HttpStatusCode.BadRequest);
        Assert.Contains("'9am' is not a valid time of day", problem.Detail, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Health_endpoint_reports_healthy()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }
}
