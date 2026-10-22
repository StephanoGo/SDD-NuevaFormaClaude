# Módulo: Historia Clínica

**Estado:** implementado. **Prioridad:** principal para el demo. Detalle de la entrega: [`004-historia-clinica`](../004-historia-clinica/spec.md).

## Necesidad y objetivo

Consultar en una ficha de paciente el historial cronológico de atenciones ya registradas. La historia reutiliza los datos de Atenciones y no duplica información clínica.

## Decisiones confirmadas por el usuario

- Cada entrada muestra motivo, diagnóstico, receta, médico, especialidad y fecha/horario de la cita.
- El orden es cronológico por fecha y hora del bloque de cita; no se captura una fecha real separada de atención.
- Los datos se consultan desde SQL Server actual.
- La primera entrega no incorpora autenticación ni roles; conserva el comportamiento actual de la aplicación.
- Antecedentes, alergias, signos vitales, adjuntos y correcciones quedan fuera de alcance.

## Requisitos funcionales

- **RF-HC-01:** Consultar las atenciones concluidas de un paciente existente.
- **RF-HC-02:** Presentar motivo, diagnóstico, receta, médico, especialidad, fecha y horario, ordenados cronológicamente por la cita.
- **RF-HC-03:** Devolver historial vacío para paciente existente sin atenciones; excluir citas pendientes y canceladas.
- **RF-HC-04:** Rechazar la consulta de un paciente inexistente con el error estable de paciente no encontrado.
- **RF-HC-05:** Ofrecer desde la web búsqueda/selección de paciente y consulta de su historia, con estado vacío cuando no haya atenciones.

## Criterios de aceptación

| Dado | Cuando | Entonces |
|---|---|---|
| Paciente con atenciones concluidas | Se consulta su historia | Se devuelven entradas con motivo, diagnóstico, receta, médico, especialidad y horario (RF-HC-01, 02) |
| Paciente existente sin atenciones | Se consulta su historia | Se devuelve una lista vacía y la web informa que aún no tiene atenciones (RF-HC-03, 05) |
| Paciente con citas pendientes o canceladas | Se consulta su historia | Esas citas no aparecen como atenciones (RF-HC-03) |
| La historia tiene varias atenciones | Se consulta | Se ordenan por fecha y hora de cita, de más reciente a más antigua (RF-HC-02) |
| El paciente no existe | Se consulta su historia | La API devuelve 404 `PATIENT_NOT_FOUND` (RF-HC-04) |

## Interfaces y responsabilidades

El contrato HTTP y de repositorio está en [`004-historia-clinica/contracts`](../004-historia-clinica/contracts/). Citas y Atenciones son fuentes de datos; Historia Clínica es una consulta de lectura.
