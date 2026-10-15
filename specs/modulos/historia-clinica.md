# Módulo: Historia Clínica

**Estado:** borrador bloqueado; requiere aclaraciones antes de asignar RF y pasar por plan.
**Módulo:** Historia Clínica. **Prioridad de producto:** principal para el demo.

## Necesidad original

La empresa basa su trabajo en la historia clínica. El demo debe centrar el flujo en consultar el historial clínico del paciente y reducir el protagonismo de tareas secundarias.

## Objetivo

Consultar en una ficha de paciente las atenciones clínicas que ya se registraron en el sistema.

## Alcance propuesto

- Mostrar las atenciones del paciente seleccionado en orden cronológico.
- Cada entrada presenta los datos ya capturados por Atenciones: motivo, diagnóstico y receta, junto con cita, médico y horario relacionados.
- Un paciente sin atenciones muestra un estado vacío; las citas pendientes no se presentan como atenciones concluidas.
- La historia consulta los registros de Atenciones y no crea una segunda copia del diagnóstico o la receta.

## Fuera de alcance propuesto

Antecedentes, alergias, signos vitales, adjuntos, correcciones de registros, control de acceso y persistencia durable no se agregan sin confirmación del negocio.

## Clarificaciones pendientes

1. ¿Qué datos debe mostrar una atención en la historia clínica, además de motivo, diagnóstico y receta?
2. ¿El orden cronológico usa fecha/hora de cita o hace falta registrar fecha/hora real de atención?
3. ¿La primera entrega del demo conserva datos en memoria o necesita persistencia durable?
4. ¿Qué rol puede consultar y registrar la historia? El sistema actual no implementa autenticación.

No hay RF aprobados ni criterios Given/When/Then todavía. No completar las respuestas por inferencia; `/speckit-analyze` debe bloquear hasta resolverlas.
