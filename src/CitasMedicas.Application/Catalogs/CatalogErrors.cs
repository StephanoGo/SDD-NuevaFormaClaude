using CitasMedicas.Domain.Common;

namespace CitasMedicas.Application.Catalogs;

public static class CatalogErrors
{
    public static readonly Error SpecialtyNameRequired = Error.Validation("SPECIALTY_NAME_REQUIRED", "El nombre de la especialidad es obligatorio");
    public static readonly Error SpecialtyNotFound = Error.NotFound("SPECIALTY_NOT_FOUND", "Especialidad no encontrada");
    public static readonly Error SpecialtyFutureAppointmentsExist = Error.Conflict("SPECIALTY_FUTURE_APPOINTMENTS_EXIST", "La especialidad tiene citas futuras pendientes");
    public static readonly Error DoctorLicenseRequired = Error.Validation("DOCTOR_LICENSE_REQUIRED", "El número de licencia es obligatorio");
    public static readonly Error DoctorFirstNameRequired = Error.Validation("DOCTOR_FIRST_NAME_REQUIRED", "El nombre del médico es obligatorio");
    public static readonly Error DoctorLastNameRequired = Error.Validation("DOCTOR_LAST_NAME_REQUIRED", "El apellido del médico es obligatorio");
    public static readonly Error DoctorSpecialtyRequired = Error.Validation("DOCTOR_SPECIALTY_REQUIRED", "La especialidad es obligatoria");
    public static readonly Error DoctorNotFound = Error.NotFound("DOCTOR_NOT_FOUND", "Médico no encontrado");
    public static readonly Error DoctorSpecialtyNotActive = Error.Conflict("DOCTOR_SPECIALTY_NOT_ACTIVE", "La especialidad no está activa");
    public static readonly Error DoctorFutureAppointmentsExist = Error.Conflict("DOCTOR_FUTURE_APPOINTMENTS_EXIST", "El médico tiene citas futuras pendientes");
}
