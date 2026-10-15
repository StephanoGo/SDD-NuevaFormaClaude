using System.Text.RegularExpressions;
using CitasMedicas.Domain.Common;

namespace CitasMedicas.Domain.Patients;

public sealed partial record Email
{
    public string Value { get; }

    private Email(string value)
    {
        Value = value;
    }

    public static Result<Email?> Create(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return Result<Email?>.Success(null);

        var trimmed = raw.Trim();
        if (!EmailRegex().IsMatch(trimmed))
            return PatientErrors.EmailInvalid;

        return Result<Email?>.Success(new Email(trimmed));
    }

    public override string ToString() => Value;

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase)]
    private static partial Regex EmailRegex();
}
