using CitasMedicas.Domain.Common;

namespace CitasMedicas.Domain.Appointments;

public static class AppointmentErrors
{
    public static readonly Error ReasonRequired = Error.Validation("APPOINTMENT_REASON_REQUIRED", "Motivo obligatorio");
    public static readonly Error DiagnosisRequired = Error.Validation("ATTENTION_DIAGNOSIS_REQUIRED", "Diagnóstico obligatorio");
    public static readonly Error NotPending = Error.Conflict("APPOINTMENT_NOT_PENDING", "La cita no admite atención");
    public static readonly Error NotFound = Error.NotFound("APPOINTMENT_NOT_FOUND", "Cita no encontrada");
}
