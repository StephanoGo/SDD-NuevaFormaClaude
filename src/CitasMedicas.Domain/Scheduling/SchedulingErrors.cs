using CitasMedicas.Domain.Common;

namespace CitasMedicas.Domain.Scheduling;

public static class SchedulingErrors
{
    public static readonly Error InvalidTimeRange = Error.Validation("SLOT_INVALID_TIME_RANGE", "Rango horario inválido");
    public static readonly Error InPast = Error.Validation("SLOT_IN_PAST", "No se permiten bloques en el pasado");
    public static readonly Error Overlap = Error.Conflict("SLOT_OVERLAP", "Bloque solapado");
    public static readonly Error SlotNotFound = Error.NotFound("SLOT_NOT_FOUND", "Bloque de agenda no encontrado");
    public static readonly Error SlotNotAvailable = Error.Conflict("SLOT_NOT_AVAILABLE", "Horario no disponible");
}
