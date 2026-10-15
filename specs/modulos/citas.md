# Módulo: Citas

**Estado:** implementado. **Origen:** HU-03 / RF-015–021 de la entrega Core.

## Objetivo y alcance

Reservar un bloque disponible para un paciente con un motivo de consulta. El registro clínico posterior pertenece a Atenciones.

## Reglas y aceptación

- Una reserva válida crea una cita pendiente y marca el bloque como reservado en una operación atómica.
- Rechazar bloque no disponible o iniciado, paciente o bloque inexistente y motivo vacío.
- Ante solicitudes concurrentes sobre el mismo bloque, confirmar como máximo una.
- La interfaz ofrece bloques disponibles y confirma la reserva; errores de conflicto se explican al usuario.
- La consulta por identificador devuelve cita, paciente, médico y horario asociados.

## Requisitos y criterios

- **RF-CIT-01**: Reservar bloque disponible para paciente existente y motivo no vacío.
- **RF-CIT-02**: Confirmar en una operación atómica la cita Pendiente y el bloque Reservado.
- **RF-CIT-03**: Rechazar bloque no disponible, iniciado o inexistente; una carrera concurrente produce como máximo una reserva.
- **RF-CIT-04**: Consultar una cita junto con paciente, médico y horario.

| Dado | Cuando | Entonces |
| --- | --- | --- |
| Paciente existente y bloque futuro disponible | Reservo con motivo válido | Se crea una cita Pendiente y el bloque queda Reservado (RF-CIT-01, 02) |
| Dos solicitudes compiten por el mismo bloque | Se procesan concurrentemente | Solo una puede reservarlo (RF-CIT-03) |
| Existe la cita reservada | La consulto por ID | Recibo cita y entidades relacionadas (RF-CIT-04) |

**Verificación:** pruebas de reserva, estados, errores y concurrencia; `dotnet build` y `dotnet test`.
