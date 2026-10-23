# Modelo de datos: Reprogramación de citas

## Entidades y propiedad

| Entidad | Propiedad | Datos y relaciones |
|---|---|---|
| `Appointment` | Citas | Mantiene ID, paciente, motivo de consulta y estado; cambia su `ScheduleSlotId` al bloque vigente. Estados existentes: `Pending`, `Attended`, `Cancelled`. |
| `ScheduleSlot` | Agenda | Sin campos nuevos. Origen pasa `Reserved` → `Available`; destino pasa `Available` → `Reserved`. Ambos del mismo médico. |
| `AppointmentReschedule` (nombre lógico) | Citas | Evento histórico append-only: cita, bloque anterior, bloque nuevo, motivo de cambio (obligatorio, hasta 500 caracteres) y fecha/hora del cambio. |

## Invariantes

- Cada cita pendiente tiene exactamente un bloque vigente reservado; cada bloque reservado pertenece a como máximo una cita, conforme a índice único existente de `Appointments.ScheduleSlotId`.
- Reprogramación cambia la FK de la cita y ambos estados de bloque dentro de una transacción indivisible. Ante fallo no se agrega evento ni se modifica ningún estado.
- Cita, paciente y motivo de consulta conservan su identidad/valor; cada evento apunta al horario anterior y nuevo de esa transición.
- Bloque destino distinto, disponible, futuro local y del mismo médico; bloque fuente debe ser el bloque vigente reservado de la cita.
- Solo transición desde `Pending`; reprogramación, cancelación y atención compiten por la misma condición de estado persistido.
- El historial no se edita ni elimina por esta funcionalidad; la lectura se ordena por fecha/hora ascendente y, en empate, ID de evento.

## Esquema SQL propuesto

Tabla `AppointmentReschedules` con PK `Id`; FK requerida `AppointmentId` → `Appointments.Id`, `PreviousScheduleSlotId` y `NewScheduleSlotId` → `ScheduleSlots.Id`; `Reason` requerido con longitud máxima 500; `ChangedAt` requerido como timestamp UTC. Índice compuesto por `AppointmentId`, `ChangedAt`, `Id` para historial ordenado. No imponer unicidad de `AppointmentId`: una cita admite varios eventos. Las FKs son restrictivas; no se eliminan registros referenciados. La interfaz convierte `ChangedAt` a `America/Lima` para presentarlo.

## Migración

Migración EF Core 10 nueva, aditiva y no destructiva: crea tabla, FKs e índice; no altera filas existentes ni hace backfill porque no se inventa historial previo. Mantener citas y reservas actuales intactas y sin seed. Probar upgrade sobre esquema de migración anterior con datos existentes; prueba SQL solamente en la base dedicada validada. El modelo/índice único vigente de `Appointments.ScheduleSlotId` se conserva.
