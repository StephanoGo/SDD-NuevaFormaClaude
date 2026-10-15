using CitasMedicas.Domain.Common;

namespace CitasMedicas.Domain.Doctors;

public static class DoctorErrors
{
    public static readonly Error NotAvailable = Error.Conflict("DOCTOR_NOT_AVAILABLE", "Médico no disponible para agenda");
}
