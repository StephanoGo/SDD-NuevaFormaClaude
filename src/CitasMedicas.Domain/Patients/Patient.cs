using CitasMedicas.Domain.Common;

namespace CitasMedicas.Domain.Patients;

public sealed class Patient
{
    public int Id { get; private set; }
    public Dni Dni { get; private set; }
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public string Phone { get; private set; }
    public Email? Email { get; private set; }

    public string FullName => $"{FirstName} {LastName}".Trim();

    private Patient(int id, Dni dni, string firstName, string lastName, string phone, Email? email)
    {
        Id = id;
        Dni = dni;
        FirstName = firstName;
        LastName = lastName;
        Phone = phone;
        Email = email;
    }

    public static Result<Patient> Create(string? dniRaw, string? firstName, string? lastName, string? phone, string? emailRaw)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            return PatientErrors.RequiredField("firstName");

        if (string.IsNullOrWhiteSpace(lastName))
            return PatientErrors.RequiredField("lastName");

        if (string.IsNullOrWhiteSpace(phone))
            return PatientErrors.RequiredField("phone");

        var dniResult = Dni.Create(dniRaw);
        if (dniResult.IsFailure)
            return dniResult.Error;

        var emailResult = Email.Create(emailRaw);
        if (emailResult.IsFailure)
            return emailResult.Error;

        return new Patient(
            id: 0,
            dni: dniResult.Value,
            firstName: firstName.Trim(),
            lastName: lastName.Trim(),
            phone: phone.Trim(),
            email: emailResult.Value
        );
    }

    internal void AssignId(int id)
    {
        if (Id != 0)
            throw new InvalidOperationException("Entity already has an Id assigned.");
        if (id <= 0)
            throw new ArgumentOutOfRangeException(nameof(id), "Id must be greater than zero.");
        Id = id;
    }

    internal static Patient Rehydrate(int id, string dni, string firstName, string lastName, string phone, string? email)
    {
        var dniVo = Dni.Create(dni).Value;
        var emailVo = Email.Create(email).Value;
        return new Patient(id, dniVo, firstName, lastName, phone, emailVo);
    }
}
