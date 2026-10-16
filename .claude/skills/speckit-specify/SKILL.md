---
name: speckit-specify
description: Crear o refinar la especificación de una feature de módulo usando el flujo SDD del proyecto.
disable-model-invocation: true
metadata:
  opencode/slash: "true"
---

Crear o refinar una especificación funcional para: $ARGUMENTS

1. Leer `CLAUDE.md`, `.specify/memory/constitution.md`, `specs/README.md`, la spec del módulo afectado y el código relevante.
2. Mantener la solicitud original del usuario textualmente en la spec de la feature. Identificar el módulo propietario y los módulos afectados.
3. Preguntar por las decisiones de negocio que cambian el comportamiento. Nunca rellenar vacíos con reglas inventadas. Registrar las preguntas sin responder y mantener la spec en borrador hasta resolverlas.
4. Crear `specs/modulos/<module>/<feature-slug>/spec.md` con nombre de la feature, módulo, nombre de rama, fecha, estado, solicitud original, alcance, fuera de alcance, aclaraciones, requisitos funcionales con IDs estables prefijados por módulo y criterios de aceptación Dado/Cuando/Entonces mapeados a esos IDs.
5. Mantener los requisitos visibles para el usuario y verificables; no incluir arquitectura ni decisiones de implementación en los requisitos funcionales.
6. No modificar `src/` ni `tests/`, y no crear artefactos de fases posteriores. Reportar la ruta de la spec, las aclaraciones abiertas y el siguiente comando.
