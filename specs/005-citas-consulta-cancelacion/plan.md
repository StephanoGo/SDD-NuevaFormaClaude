# Plan: Consulta y cancelación de citas

## Diseño

Extender `IAppointmentRepository`, servicio de citas y `AppointmentEndpoints`. Listado proyecta citas con paciente, médico/especialidad y bloque. La consulta acepta al menos un filtro opcional paciente/médico/fecha, todos combinables. Cancelación agrega transición de dominio `Pending -> Cancelled` y operación compare-and-set atómica que modifica cita y bloque. SQL usa transacción; memoria usa sección crítica. La interfaz añade una vista de búsqueda/listado, detalle y acción de cancelación con confirmación; actualización posterior refresca cita y horario.

## Invariantes y errores

Solo la cita pendiente puede cancelarse; atención y cancelación compiten por el estado persistido Pending; una sola gana. En cancelación exitosa el bloque debe estar Reserved y vuelve a Available en la misma transacción. Si no se gana la transición: `APPOINTMENT_NOT_PENDING`; ningún efecto parcial. Sin filtros: `VALIDATION_FORMAT`.

## Persistencia/verificación

Sin cambio de esquema ni migración. Probar combinaciones de filtros, orden y vacío, consulta inválida, cancelación/reutilización, estados terminales y concurrencia atención-cancelación; HTTP/UI; build/test.
