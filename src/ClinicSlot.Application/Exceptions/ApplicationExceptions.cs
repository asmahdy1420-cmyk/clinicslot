namespace ClinicSlot.Application.Exceptions;

/// <summary>The requested resource does not exist (HTTP 404).</summary>
public sealed class NotFoundException(string resource, Guid id)
    : Exception($"{resource} '{id}' was not found.");

/// <summary>The request conflicts with the current state of the data (HTTP 409).</summary>
public class ConflictException : Exception
{
    public ConflictException(string message)
        : base(message)
    {
    }

    public ConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

/// <summary>The practitioner's time is already taken by another appointment.</summary>
public sealed class SlotConflictException : ConflictException
{
    public const string DefaultMessage = "The requested time overlaps another appointment for this practitioner.";

    public SlotConflictException()
        : base(DefaultMessage)
    {
    }

    public SlotConflictException(Exception innerException)
        : base(DefaultMessage, innerException)
    {
    }
}
