# Feature: Consulta y cancelación de citas

- **Módulo propietario:** Citas.
- **Módulos afectados:** Agenda, Atenciones, Historia Clínica y experiencia web.
- **Rama:** `feature/appointment-query-cancellation`.
- **Fecha:** 2026-10-19.
- **Estado:** especificación aprobada.
- **Solicitud original:** “Quiero implementar todo eso”.

## Necesidad y alcance

Permitir localizar citas por paciente, médico y/o fecha, y cancelar citas pendientes desde la operación clínica.

## Decisiones confirmadas

- El listado acepta uno o más filtros opcionales `patientId`, `doctorId`, `date`; sin filtros responde `VALIDATION_FORMAT`.
- Los resultados incluyen todos los estados y se ordenan por fecha/hora de cita ascendente, después por ID de cita.
- Cancelar está permitido solo para citas `Pending`; cita pasa a `Cancelled` y su bloque `Reserved` vuelve a `Available`, atómicamente.
- Rechazar cancelación de citas `Attended` o `Cancelled` con `APPOINTMENT_NOT_PENDING` sin cambios.
- Mantener persistencia SQL y concurrencia protegida; no se crean campos ni tablas nuevas.

## Requisitos funcionales

- **RF-CIT-05:** Listar por uno o más de paciente, médico y fecha; validar filtros y devolver citas relacionadas con estado en orden cronológico ascendente.
- **RF-CIT-06:** Cancelar una cita pendiente de manera atómica y liberar el bloque para reutilización.
- **RF-CIT-07:** Rechazar consulta sin filtro y filtros con formato inválido con `VALIDATION_FORMAT`.
- **RF-CIT-08:** Rechazar cancelación de citas no pendientes con `APPOINTMENT_NOT_PENDING`, sin modificar cita ni bloque.
- **RF-CIT-09:** La web permite buscar/consultar citas y cancelar una pendiente con confirmación y actualización del estado/agenda; cuando filtra por paciente, solicita su DNI, no su ID interno.

## Aceptación

| Dado | Cuando | Entonces |
|---|---|---|
| Hay citas para paciente, médico o fecha | Se envía uno o más filtros válidos | Se devuelven coincidencias con entidades relacionadas y todos sus estados, ordenadas por horario (RF-CIT-05) |
| No se envía filtro o un filtro no se puede parsear | Se consulta el listado | Se devuelve `400 VALIDATION_FORMAT` (RF-CIT-07) |
| Cita pendiente con bloque reservado | Se cancela | La cita queda `Cancelled` y el bloque `Available`; ambos cambios se confirman juntos (RF-CIT-06) |
| Dos peticiones intentan cancelar la misma cita o cancelarla mientras se registra atención | Se ejecutan concurrentemente | Solo una transición de estado concluye y no queda el bloque en estado inconsistente (RF-CIT-06, 08) |
| Cita atendida o cancelada | Se solicita cancelar | Se responde conflicto `APPOINTMENT_NOT_PENDING`; datos y bloque no cambian (RF-CIT-08) |
| Operador busca una cita desde la web | Carga resultados | Puede consultar detalle y cancelar solo una pendiente tras confirmación (RF-CIT-09) |
| Operador busca por paciente | Ingresa el filtro de paciente | La interfaz solicita DNI y resuelve internamente el identificador, sin pedir el ID de base de datos (RF-CIT-09) |
