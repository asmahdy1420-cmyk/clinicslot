using System.Text.Json;
using ClinicSlot.Application.Exceptions;
using ClinicSlot.Domain.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace ClinicSlot.Api.Http;

/// <summary>Maps domain and application exceptions to RFC 7807 problem responses.</summary>
internal sealed partial class ExceptionToProblemDetailsHandler(
    IProblemDetailsService problemDetails,
    ILogger<ExceptionToProblemDetailsHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title) = exception switch
        {
            DomainValidationException => (StatusCodes.Status400BadRequest, "Invalid request"),
            BadHttpRequestException badRequest => (badRequest.StatusCode, "Malformed request"),
            NotFoundException => (StatusCodes.Status404NotFound, "Resource not found"),
            SlotConflictException => (StatusCodes.Status409Conflict, "Slot unavailable"),
            ConflictException => (StatusCodes.Status409Conflict, "Conflict"),
            DomainRuleException => (StatusCodes.Status422UnprocessableEntity, "Business rule violated"),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred"),
        };

        if (status == StatusCodes.Status500InternalServerError)
        {
            LogUnhandled(logger, exception, httpContext.Request.Method, httpContext.Request.Path);
        }

        httpContext.Response.StatusCode = status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = status == StatusCodes.Status500InternalServerError ? null : Describe(exception),
            },
        });
    }

    /// <summary>For binding failures, include the JSON reader's reason (e.g. which field failed).</summary>
    private static string Describe(Exception exception) =>
        exception is BadHttpRequestException { InnerException: JsonException json }
            ? $"{exception.Message} {json.Message}"
            : exception.Message;

    [LoggerMessage(EventId = 5000, Level = LogLevel.Error, Message = "Unhandled exception for {Method} {Path}")]
    private static partial void LogUnhandled(ILogger logger, Exception exception, string method, string path);
}
