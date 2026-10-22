# Módulo: Citas

**Estado:** reserva, consultas y cancelación implementadas. **Detalle:** [`005-citas-consulta-cancelacion`](../005-citas-consulta-cancelacion/spec.md). **Origen:** HU-03 / RF-015–021 de la entrega Core.

## Objetivo y alcance

Reservar un bloque disponible para un paciente con un motivo de consulta. El registro clínico posterior pertenece a Atenciones.

## Reglas y aceptación

- Una reserva válida crea una cita pendiente y marca el bloque como reservado en una operación atómica.
- Rechazar bloque no disponible o iniciado, paciente o bloque inexistente y motivo vacío.
- Ante solicitudes concurrentes sobre el mismo bloque, confirmar como máximo una.
- La interfaz ofrece bloques disponibles y confirma la reserva; errores de conflicto se explican al usuario.
- La consulta por identificador devuelve cita, paciente, médico y horario asociados.
- Listar citas mediante uno o más filtros de paciente, médico y fecha; al menos un filtro es obligatorio. Incluir citas de todos los estados, en orden cronológico de cita.
- Cancelar solo citas pendientes; la operación atómica marca la cita Cancelled y libera su bloque a Available. No se cancelan citas atendidas ni ya canceladas.

## Requisitos y criterios

- **RF-CIT-01**: Reservar bloque disponible para paciente existente y motivo no vacío.
- **RF-CIT-02**: Confirmar en una operación atómica la cita Pendiente y el bloque Reservado.
- **RF-CIT-03**: Rechazar bloque no disponible, iniciado o inexistente; una carrera concurrente produce como máximo una reserva.
- **RF-CIT-04**: Consultar una cita junto con paciente, médico y horario.
- **RF-CIT-05**: Listar citas filtradas por paciente, médico y/o fecha; rechazar consultas sin filtros y devolver resultados ordenados por fecha/hora ascendente.
- **RF-CIT-06**: Cancelar una cita pendiente de forma atómica, cambiarla a Cancelled y liberar el bloque a Available; impedir cancelación de citas no pendientes.

| Dado | Cuando | Entonces |
| --- | --- | --- |
| Paciente existente y bloque futuro disponible | Reservo con motivo válido | Se crea una cita Pendiente y el bloque queda Reservado (RF-CIT-01, 02) |
| Dos solicitudes compiten por el mismo bloque | Se procesan concurrentemente | Solo una puede reservarlo (RF-CIT-03) |
| Existe la cita reservada | La consulto por ID | Recibo cita y entidades relacionadas (RF-CIT-04) |
| Se proporciona uno o más filtros de búsqueda | Consulto las citas | Recibo todas las coincidencias, ordenadas cronológicamente e incluyendo estado (RF-CIT-05) |
| No se proporciona ningún filtro | Consulto las citas | La API rechaza la consulta con `VALIDATION_FORMAT` (RF-CIT-05) |
| Cita pendiente con bloque reservado | La cancelo | La cita queda Cancelled y el bloque disponible en una operación atómica (RF-CIT-06) |
| Cita atendida o cancelada | Intento cancelarla | Se rechaza con `APPOINTMENT_NOT_PENDING` y no se altera el bloque (RF-CIT-06) |

**Verificación:** pruebas de reserva, estados, errores y concurrencia; `dotnet build` y `dotnet test`.
