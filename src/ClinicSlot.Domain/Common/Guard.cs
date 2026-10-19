namespace ClinicSlot.Domain.Common;

internal static class Guard
{
    public static string NotBlank(string? value, string name, int maxLength = 200)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainValidationException($"{name} is required.");
        }

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
        {
            throw new DomainValidationException($"{name} must be at most {maxLength} characters.");
        }

        return trimmed;
    }

    public static void NotEmpty(Guid value, string name)
    {
        if (value == Guid.Empty)
        {
            throw new DomainValidationException($"{name} is required.");
        }
    }
}
