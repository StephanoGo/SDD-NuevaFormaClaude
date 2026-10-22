# Contratos: Consulta y cancelación de citas

## HTTP

- `GET /api/appointments?patientId={id}&doctorId={id}&date=yyyy-MM-dd`: requiere uno o más filtros; filtros combinables. `200` con lista de `AppointmentResponse`; incluye todos los estados, orden ascendente por fecha/hora/ID. Sin filtro o formato inválido: `400 VALIDATION_FORMAT`.
- `POST /api/appointments/{appointmentId}/cancellation`: `200` con `AppointmentResponse` en estado Cancelled. `404 APPOINTMENT_NOT_FOUND`; `409 APPOINTMENT_NOT_PENDING` cuando no está Pending.

## Repositorio

Añadir `ListAppointmentsAsync(int? patientId, int? doctorId, DateOnly? date, ct)` y `CancelAsync(Appointment, ct)`. CancelAsync CAS Pending->Cancelled y libera slot Reserved->Available atómicamente; estado cambiado por otra petición devuelve `APPOINTMENT_NOT_PENDING` sin efectos. El detalle/listado reutiliza un DTO de cita coherente con contrato actual.
