---
name: speckit-analyze
description: Auditar de forma cruzada los artefactos SDD de una feature y bloquear la implementación hasta que sean coherentes.
disable-model-invocation: true
metadata:
  opencode/slash: "true"
---

Analizar $ARGUMENTS (o la feature actual) como una puerta de implementación estricta.

Comparar `spec.md`, `plan.md`, `data-model.md`, contratos, checklists, `tasks.md`, la constitución, las specs de los módulos afectados y el código/pruebas relevantes. Comprobar que:

- Las aclaraciones que afectan al comportamiento están resueltas.
- Cada RF tiene cobertura de aceptación, un diseño/contrato compatible, una tarea y pruebas planificadas.
- Ningún endpoint, campo de datos, invariante, dependencia o comportamiento aparece solo en el diseño/tareas sin respaldo en la spec.
- Los límites de módulo, las transiciones de estado, los códigos de error y la terminología entre artefactos concuerdan.
- La tecnología propuesta y el diseño por capas obedecen la constitución.
- Las comprobaciones de calidad cubren los casos de fallo y las reglas críticas de dominio/UX.

Devolver `BLOCKED` con cada hallazgo y el artefacto/requisito a corregir, o `READY FOR IMPLEMENTATION` con las notas no bloqueantes. No modificar `src/`, `tests/` ni reescribir silenciosamente requisitos aprobados. Un resultado bloqueado prohíbe `/speckit-implement` hasta que se corrijan los artefactos y el análisis pase.
