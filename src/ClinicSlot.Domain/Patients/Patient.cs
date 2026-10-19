using ClinicSlot.Domain.Common;

namespace ClinicSlot.Domain.Patients;

/// <summary>
/// Patients are identified minimally: a name, an email used as the natural key,
/// and an optional phone number. No clinical data is stored.
/// </summary>
public sealed class Patient
{
    private Patient()
    {
        FullName = string.Empty;
        Email = string.Empty;
    }

    public Guid Id { get; private set; }

    public string FullName { get; private set; }

    public string Email { get; private set; }

    public string? Phone { get; private set; }

    public static Patient Create(string fullName, string email, string? phone)
    {
        return new Patient
        {
            Id = Guid.NewGuid(),
            FullName = Guard.NotBlank(fullName, "Full name"),
            Email = NormalizeEmail(email),
            Phone = string.IsNullOrWhiteSpace(phone) ? null : Guard.NotBlank(phone, "Phone", 32),
        };
    }

    public static string NormalizeEmail(string email)
    {
        var normalized = Guard.NotBlank(email, "Email", 254).ToLowerInvariant();
        var at = normalized.IndexOf('@', StringComparison.Ordinal);
        if (at <= 0 || at == normalized.Length - 1)
        {
            throw new DomainValidationException("Email is not a valid address.");
        }

        return normalized;
    }
}
