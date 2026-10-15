namespace CitasMedicas.Application.Patients;

public sealed record RegisterPatientRequest(
    string Dni,
    string FirstName,
    string LastName,
    string Phone,
    string? Email);

public sealed record PatientResponse(
    int Id,
    string Dni,
    string FirstName,
    string LastName,
    string Phone,
    string? Email);
