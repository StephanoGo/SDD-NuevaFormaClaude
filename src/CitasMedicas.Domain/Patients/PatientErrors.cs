using CitasMedicas.Domain.Common;

namespace CitasMedicas.Domain.Patients;

public static class PatientErrors
{
    public static readonly Error DniInvalid = Error.Validation("PATIENT_DNI_INVALID", "DNI inválido");
    public static readonly Error EmailInvalid = Error.Validation("PATIENT_EMAIL_INVALID", "Email inválido");
    public static Error RequiredField(string fieldName) => Error.Validation("PATIENT_REQUIRED_FIELD", $"El campo {fieldName} es obligatorio");
    public static readonly Error DniDuplicate = Error.Conflict("PATIENT_DNI_DUPLICATE", "DNI ya registrado");
    public static readonly Error SearchCriteriaTooShort = Error.Validation("SEARCH_CRITERIA_TOO_SHORT", "Criterio de búsqueda demasiado corto");
    public static readonly Error NotFound = Error.NotFound("PATIENT_NOT_FOUND", "Paciente no encontrado");
}
