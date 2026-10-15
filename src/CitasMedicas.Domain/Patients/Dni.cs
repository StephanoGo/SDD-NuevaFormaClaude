using CitasMedicas.Domain.Common;

namespace CitasMedicas.Domain.Patients;

public sealed record Dni
{
    public string Value { get; }

    private Dni(string value)
    {
        Value = value;
    }

    public static Result<Dni> Create(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return PatientErrors.DniInvalid;

        var trimmed = raw.Trim();
        if (trimmed.Length != 8)
            return PatientErrors.DniInvalid;

        for (var i = 0; i < 8; i++)
        {
            char c = trimmed[i];
            if (c < '0' || c > '9')
                return PatientErrors.DniInvalid;
        }

        return new Dni(trimmed);
    }

    public override string ToString() => Value;
}
