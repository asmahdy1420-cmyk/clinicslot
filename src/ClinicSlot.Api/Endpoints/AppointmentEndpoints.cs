using ClinicSlot.Api.Http;
using ClinicSlot.Application.Contracts;
using ClinicSlot.Application.Services;

namespace ClinicSlot.Api.Endpoints;

internal static class AppointmentEndpoints
{
    public static IEndpointRouteBuilder MapAppointmentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/appointments").WithTags("Appointments");

        group.MapPost("/", async (BookAppointmentRequest request, BookingService booking, CancellationToken ct) =>
            {
                var appointment = await booking.BookAsync(request, ct);
                return TypedResults.Created($"/api/appointments/{appointment.Id}", appointment);
            })
            .Validate<BookAppointmentRequest>()
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .WithName("BookAppointment");

        group.MapGet("/{appointmentId:guid}", async (Guid appointmentId, BookingService booking, CancellationToken ct) =>
                TypedResults.Ok(await booking.GetAsync(appointmentId, ct)))
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithName("GetAppointment");

        group.MapPost("/{appointmentId:guid}/cancel", async (Guid appointmentId, BookingService booking, CancellationToken ct) =>
                TypedResults.Ok(await booking.CancelAsync(appointmentId, ct)))
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .WithName("CancelAppointment");

        group.MapPost("/{appointmentId:guid}/reschedule", async (
                Guid appointmentId, RescheduleAppointmentRequest request, BookingService booking, CancellationToken ct) =>
                TypedResults.Ok(await booking.RescheduleAsync(appointmentId, request, ct)))
            .Validate<RescheduleAppointmentRequest>()
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .WithName("RescheduleAppointment");

        return app;
    }
}
