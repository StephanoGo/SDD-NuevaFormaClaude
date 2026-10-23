# Contratos: Reprogramación de citas

Los formatos generales, convenciones de IDs, ProblemDetails y contratos existentes de consulta/reserva/cancelación/atención se mantienen según [`../../001-citas-medicas-core/contracts/api-contracts.md`](../../001-citas-medicas-core/contracts/api-contracts.md). Este documento especifica solo las interfaces nuevas o ampliadas por la feature.

## HTTP

### Reprogramar cita

`POST /api/appointments/{appointmentId}/rescheduling`

Request JSON:

```json
{
  "scheduleSlotId": 25,
  "reason": "El paciente solicita otro horario"
}
```

`scheduleSlotId`: entero positivo, bloque destino. `reason`: texto requerido, tras trim no vacío y hasta 500 caracteres.

Respuestas:

| HTTP | Código/condición | Resultado |
|---|---|---|
| 200 | Éxito | `AppointmentResponse` vigente, ampliado con `reschedules` (array cronológico). Cada elemento incluye `previousSlot`, `newSlot`, `reason`, `changedAt`. |
| 400 | `VALIDATION_FORMAT` | ID o JSON con formato inválido. |
| 400 | `APPOINTMENT_RESCHEDULE_REASON_REQUIRED` | Motivo vacío. |
| 400 | `APPOINTMENT_RESCHEDULE_REASON_TOO_LONG` | Motivo excede 500 caracteres. |
| 404 | `APPOINTMENT_NOT_FOUND` | Cita no existe. |
| 404 | `SLOT_NOT_FOUND` | Destino no existe. |
| 409 | `APPOINTMENT_NOT_PENDING` | Cita atendida/cancelada. |
| 409 | `SLOT_NOT_AVAILABLE` | Destino reservado, pasado o no pertenece al médico de la cita. |
| 409 | `APPOINTMENT_RESCHEDULE_CONFLICT` | Destino es el bloque actual o la cita/bloques cambiaron concurrentemente. No hay cambios parciales. |

Si el bloque de origen no existe o no está `Reserved`, también se devuelve `409 APPOINTMENT_RESCHEDULE_CONFLICT`, con detalle que informa la inconsistencia; no se repara ni modifica ningún dato.

La forma de las fechas/horas de los bloques reutiliza `SlotSummaryResponse` vigente. `changedAt` se serializa ISO 8601 con offset UTC y la web lo presenta en `America/Lima`. El historial también se incluye al consultar `GET /api/appointments/{appointmentId}`; paciente y `reason` de la respuesta principal conservan su significado actual de motivo clínico. Ante timeout, el cliente no repite automáticamente la escritura y consulta el detalle para conocer el estado confirmado.

## Repositorio

Extender `IAppointmentRepository`, conforme a las reglas comunes de `Result`, cancelación y atomicidad del contrato compartido:

| Operación | Entrada | Salida | Semántica |
|---|---|---|---|
| `RescheduleAsync` | Cita objetivo/estado esperado, bloque destino y motivo normalizado, timestamp UTC | `Result<Appointment>` | Operación indivisible que valida estado persistido Pending, origen Reserved vigente, destino Available/con inicio futuro/del mismo médico/distinto, cambia FK y estados de bloques, agrega evento. Cualquier conflicto/fallo revierte todo; origen inconsistente se rechaza sin reparación. |
| `GetRescheduleHistoryAsync` | `appointmentId` | Lista de eventos | Lectura ordenada ascendente por `ChangedAt`, `Id`; vacía si no hay eventos. Se agrega al DTO de detalle mediante Application. |

Mapeo de conflictos a códigos HTTP según tabla anterior. Las implementaciones SQL y en memoria deben satisfacer idéntica semántica observable; las pruebas SQL verifican carreras entre instancias/solicitudes reales.
