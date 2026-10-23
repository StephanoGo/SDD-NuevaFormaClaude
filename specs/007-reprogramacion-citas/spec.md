# Feature: Reprogramación de citas

- **Módulo propietario:** Citas.
- **Módulos afectados:** Agenda, Atenciones y experiencia web.
- **Rama propuesta:** `codex/007-reprogramacion-citas` (propuesta; no implica que exista en Git).
- **Fecha:** 2026-10-23.
- **Estado:** especificación aprobada.
- **Solicitud original:** “Crear la feature 007-reprogramacion-citas para permitir que recepción cambie el horario de una cita pendiente desde la web. Conservar el paciente y el motivo de consulta. El horario destino debe estar disponible y ser futuro según la zona horaria de la clínica. Liberar el bloque anterior y reservar el nuevo como una sola operación: si el cambio falla, conservar intacta la cita original. Rechazar la reprogramación de citas atendidas o canceladas y evitar reservas duplicadas ante solicitudes concurrentes. Quiero trazabilidad del cambio de horario y de su motivo. Revisa las specs y el código existentes, pregunta por las decisiones de negocio pendientes y crea únicamente el spec.md de esta etapa. Usa IDs de requisitos que no colisionen con los RF-CIT existentes. Propón la rama codex/007-reprogramacion-citas.”

## Contexto

El módulo Citas ya reserva y cancela citas con estados `Pending`, `Attended` y `Cancelled`; la cancelación libera su bloque. Los bloques son `Available` o `Reserved`, y Agenda aplica la zona `America/Lima`. La cita actual vincula paciente, motivo y bloque; el código protege reserva, cancelación y atención ante concurrencia. La web existente permite buscar, consultar y cancelar citas. No hay todavía función de reprogramación ni historial de cambios horarios.

## Necesidad y alcance

Recepción podrá cambiar desde la web el bloque de una cita pendiente, conservando paciente, motivo de consulta e identificador. El destino debe ser un bloque disponible y futuro en la zona horaria de la clínica. La sustitución del bloque y la actualización de la cita deben ser atómicas: ante rechazo o fallo, la cita y su reserva originales permanecen intactas. El cambio debe guardar trazabilidad del horario anterior y nuevo, su motivo y cuándo ocurrió; el historial será consultable en el detalle de la cita. Se admiten cambios repetidos.

## Fuera de alcance

- Cambiar paciente o motivo clínico de consulta.
- Reprogramar citas atendidas o canceladas.
- Cambiar el médico de la cita.
- Notificaciones, autenticación/roles, cancelación, ni cambios en el contenido clínico de la atención.
- Definir diseño técnico, almacenamiento, API o estrategia de concurrencia; se resolverán en la etapa de plan.

## Aclaraciones

**Confirmadas por el solicitante**

- Aprobada por el propietario del proyecto el 2026-10-23, incluidas las aclaraciones registradas en esta sección.

- Se conserva el mismo médico.
- Puede reprogramarse una cita pendiente aunque su horario original ya haya pasado; el destino sí debe ser futuro.
- El motivo del cambio es obligatorio y tiene máximo 500 caracteres.
- Se conserva el ID de la cita; se permiten reprogramaciones múltiples y el detalle muestra el historial completo.
- Elegir el mismo bloque se rechaza sin cambios. Si otra operación modifica la cita concurrentemente, se rechaza y no se sobrescribe el estado vigente.
- “Futuro” significa que el inicio del bloque destino es estrictamente posterior al momento actual en la zona horaria de la clínica (`America/Lima`).
- Ante timeout o pérdida de respuesta, la web no reintenta automáticamente; vuelve a consultar el detalle para conocer el estado confirmado.
- Si la cita pendiente referencia un bloque inexistente o cuyo estado no es `Reserved`, se rechaza el cambio sin reparar datos automáticamente.
- El historial registra la hora como instante UTC y la interfaz la presenta en la zona horaria de la clínica.
- Tras éxito se actualizan detalle, listado y disponibilidad; ante conflicto, la interfaz invita a revisar el horario vigente.

**Pendientes**

- Ninguna decisión de comportamiento pendiente. La línea base propuesta de 16 comprobaciones de calidad no está confirmada, pero no bloquea la feature.

## Requisitos funcionales

- **RF-REPRO-01:** Recepción puede solicitar el cambio de horario de una cita pendiente desde la web, indicando bloque destino y motivo obligatorio de hasta 500 caracteres; se conservan paciente, motivo de consulta e ID.
- **RF-REPRO-02:** Solo se acepta un destino existente, disponible, del mismo médico y con inicio estrictamente futuro según la zona horaria de la clínica (`America/Lima`); se rechazan destino igual al actual, inexistente, reservado o pasado sin modificar la cita ni sus bloques.
- **RF-REPRO-03:** La sustitución libera el bloque anterior y reserva el nuevo como una única operación; cualquier fallo conserva íntegros el horario y la reserva anteriores. Solicitudes concurrentes no pueden duplicar reservas ni sobrescribir un cambio/cancelación/atención concurrente.
- **RF-REPRO-04:** Se rechaza reprogramar una cita atendida o cancelada sin alterar cita ni agenda. Una cita pendiente cuyo bloque original ya pasó sí puede reprogramarse si el destino cumple las reglas.
- **RF-REPRO-05:** Cada cambio aceptado registra horario anterior, horario nuevo, motivo del cambio y fecha/hora; el detalle de la cita muestra el historial completo, incluyendo cambios sucesivos, sin alterar el motivo de consulta y presentando la fecha/hora en la zona de la clínica.
- **RF-REPRO-06:** La interfaz permite seleccionar y revisar el destino y motivo antes de confirmar; comunica resultado y conflictos en español, evita envíos duplicados mientras procesa y actualiza detalle, listado y disponibilidad tras éxito. Ante timeout, consulta el detalle sin reintentar automáticamente.

## Criterios de aceptación

| Dado | Cuando | Entonces |
|---|---|---|
| Una cita pendiente y un destino futuro disponible del mismo médico | Recepción confirma el nuevo horario con motivo válido | Conserva ID, paciente y motivo de consulta; la cita refiere el destino, el bloque anterior queda disponible y el nuevo reservado (RF-REPRO-01, 02, 03) |
| Una cita pendiente con horario original pasado | Se solicita un destino válido futuro | Se permite el cambio sujeto a las mismas reglas de disponibilidad y atomicidad (RF-REPRO-04) |
| Destino inexistente, reservado, pasado o igual al bloque actual | Se confirma la reprogramación | Se rechaza y cita/bloques quedan sin cambios (RF-REPRO-02) |
| Motivo del cambio vacío o de más de 500 caracteres | Se confirma la solicitud | Se rechaza sin modificar cita ni agenda (RF-REPRO-01) |
| Cita pendiente y falla cualquier parte del cambio | Se intenta confirmar | Se conserva la cita original y su reserva; no queda destino reservado por esta operación (RF-REPRO-03) |
| Dos solicitudes compiten por el mismo destino, o reprogramación compite con reserva, cancelación, atención u otra reprogramación de la misma cita | Se procesan concurrentemente | No hay reserva duplicada ni estado inconsistente; la operación perdedora se rechaza sin sobrescribir la cita vigente (RF-REPRO-03) |
| Cita atendida o cancelada | Se solicita reprogramarla | Se rechaza sin cambiar cita, bloques ni datos de atención (RF-REPRO-04) |
| Una reprogramación se completa | Se consulta el detalle de la cita | Se mantienen ID, paciente y motivo de consulta, y se presenta cada cambio con horario anterior/nuevo, motivo del cambio y fecha/hora (RF-REPRO-05) |
| Recepción prepara un cambio en web | Revisa y confirma la operación | Puede verificar destino y motivo antes de confirmar; ve estados y errores comprensibles en español y no puede enviar duplicados durante el procesamiento (RF-REPRO-06) |
| Una cita pendiente cuyo bloque de origen no existe o no está `Reserved` | Recepción solicita reprogramarla | El sistema rechaza la operación, reporta la inconsistencia y no modifica la cita, los bloques ni el historial; no repara datos automáticamente (RF-REPRO-03; AC-REPRO-09) |
| La respuesta a una solicitud se pierde o agota el tiempo de espera | La interfaz detecta el timeout | Consulta nuevamente el detalle sin reenviar automáticamente la reprogramación y presenta el estado confirmado. Si la consulta también falla, informa que no pudo confirmar el resultado sin afirmar éxito ni fracaso (RF-REPRO-06; AC-REPRO-10) |

## Referencias

- Reglas vigentes: [módulo Citas](../modulos/citas.md), [Agenda](../modulos/agenda.md), [experiencia web](../modulos/experiencia-web.md).
- Contratos compartidos vigentes: [API y persistencia](../001-citas-medicas-core/contracts/api-contracts.md).
- Implementación relevante revisada: `Appointment`, `ScheduleSlot`, `AppointmentService`, `IAppointmentRepository`, `InMemoryAppointmentRepository` y `AppointmentEndpoints`.
