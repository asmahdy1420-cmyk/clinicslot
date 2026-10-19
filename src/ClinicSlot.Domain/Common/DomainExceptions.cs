namespace ClinicSlot.Domain.Common;

/// <summary>Raised when input breaks an invariant of a domain object (maps to HTTP 400).</summary>
public sealed class DomainValidationException(string message) : Exception(message);

/// <summary>Raised when a valid request breaks a business rule (maps to HTTP 422).</summary>
public sealed class DomainRuleException(string message) : Exception(message);
