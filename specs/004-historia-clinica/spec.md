# Feature: Consulta del historial clínico

- **Módulo propietario:** Historia Clínica.
- **Módulos afectados:** Pacientes, Atenciones, Citas y experiencia web.
- **Rama:** `feature/clinical-history`.
- **Fecha:** 2026-10-19.
- **Estado:** especificación aprobada.
- **Solicitud original:** “Quiero implementar todo eso”.

## Necesidad

Consultar en una ficha de paciente las atenciones clínicas ya registradas, como flujo principal del demo.

## Decisiones confirmadas

Mostrar motivo, diagnóstico, receta, médico, especialidad y fecha/horario de la cita; ordenar por fecha/hora de la cita, de más reciente a más antigua; usar la persistencia SQL Server existente; no agregar autenticación/roles en esta entrega. No incorporar antecedentes, alergias, signos vitales, adjuntos ni edición clínica.

## Requisitos funcionales

- **RF-HC-01:** Consultar atenciones concluidas de un paciente existente.
- **RF-HC-02:** Cada entrada presenta motivo, diagnóstico, receta, médico, especialidad, fecha y horario de cita, en orden descendente por fecha/hora de cita.
- **RF-HC-03:** Excluir citas pendientes/canceladas y devolver lista vacía para paciente existente sin atenciones.
- **RF-HC-04:** Responder 404 `PATIENT_NOT_FOUND` si el paciente no existe.
- **RF-HC-05:** La web permite seleccionar/buscar un paciente y consultar la historia; muestra estado vacío o error comprensible.

## Aceptación

| Dado | Cuando | Entonces |
|---|---|---|
| Paciente con más de una atención | Consulta su historia | Recibe las atenciones en orden descendente, con todos los datos acordados (RF-HC-01, 02) |
| Paciente sin atenciones | Consulta su historia | Recibe lista vacía y estado vacío en web (RF-HC-03, 05) |
| Paciente tiene citas no concluidas | Consulta su historia | No se muestran como atenciones (RF-HC-03) |
| Paciente inexistente | Consulta su historia | Recibe 404 `PATIENT_NOT_FOUND` (RF-HC-04) |
