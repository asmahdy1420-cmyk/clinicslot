using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ClinicSlot.Api.Http;
using ClinicSlot.Application.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace ClinicSlot.IntegrationTests;

/// <summary>Thin typed helpers over the HTTP API so tests read as scenarios.</summary>
internal static class ApiClient
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(), new TimeOnlyJsonConverter() },
    };

    public static async Task<T> ReadAsync<T>(this HttpResponseMessage response)
    {
        var value = await response.Content.ReadFromJsonAsync<T>(Json);
        Assert.NotNull(value);
        return value;
    }

    public static async Task<HttpResponseMessage> PostAsync<T>(this HttpClient client, string url, T body) =>
        await client.PostAsJsonAsync(url, body, Json);

    public static async Task<ProblemDetails> ReadProblemAsync(this HttpResponseMessage response, HttpStatusCode expected)
    {
        Assert.Equal(expected, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        return await response.ReadAsync<ProblemDetails>();
    }

    /// <summary>
    /// Creates a London clinic (15 minute grid, 24 hour change cutoff), a 30 minute service with
    /// a 10 minute buffer, and a practitioner working weekdays 09:00-17:00 with lunch 12:00-13:00.
    /// </summary>
    public static async Task<Scenario> SeedAsync(this HttpClient client)
    {
        var clinicResponse = await client.PostAsync("/api/clinics", new CreateClinicRequest("Harley Street Physio", "Europe/London", 15, 24));
        Assert.Equal(HttpStatusCode.Created, clinicResponse.StatusCode);
        var clinic = await clinicResponse.ReadAsync<ClinicResponse>();

        var serviceResponse = await client.PostAsync(
            $"/api/clinics/{clinic.Id}/services", new CreateServiceRequest("Assessment", 30, 10));
        Assert.Equal(HttpStatusCode.Created, serviceResponse.StatusCode);
        var service = await serviceResponse.ReadAsync<ServiceResponse>();

        var weekdays = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday };
        var hours = weekdays
            .Select(day => new WorkingDayDto(day, new TimeOnly(9, 0), new TimeOnly(17, 0), [new BreakDto(new TimeOnly(12, 0), new TimeOnly(13, 0))]))
            .ToList();
        var practitionerResponse = await client.PostAsync(
            $"/api/clinics/{clinic.Id}/practitioners", new CreatePractitionerRequest("Dr Layla Haddad", hours));
        Assert.Equal(HttpStatusCode.Created, practitionerResponse.StatusCode);
        var practitioner = await practitionerResponse.ReadAsync<PractitionerResponse>();

        return new Scenario(clinic, service, practitioner);
    }

    public static Task<HttpResponseMessage> BookAsync(this HttpClient client, Scenario scenario, DateTimeOffset start, string email = "sara@example.com") =>
        client.PostAsync(
            "/api/appointments",
            new BookAppointmentRequest(scenario.Practitioner.Id, scenario.Service.Id, start, new PatientDto("Sara Ali", email)));

    public static async Task<AvailabilityResponse> AvailabilityAsync(this HttpClient client, Scenario scenario, string from, string to)
    {
        var response = await client.GetAsync(
            $"/api/practitioners/{scenario.Practitioner.Id}/availability?serviceId={scenario.Service.Id}&from={from}&to={to}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.ReadAsync<AvailabilityResponse>();
    }
}

internal sealed record Scenario(ClinicResponse Clinic, ServiceResponse Service, PractitionerResponse Practitioner);
