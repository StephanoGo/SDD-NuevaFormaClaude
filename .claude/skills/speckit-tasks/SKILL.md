---
name: speckit-tasks
description: Descomponer una feature especificada y planificada en tareas trazables ordenadas por dependencia.
disable-model-invocation: true
metadata:
  opencode/slash: "true"
---

Crear la lista de tareas de implementación para $ARGUMENTS (o la feature actual si se omite).

1. Leer `spec.md`, `plan.md`, `data-model.md`, contratos, checklists y la estructura actual del proyecto.
2. Si la spec tiene decisiones de comportamiento sin resolver o faltan artefactos de diseño requeridos, reportar que aún no se pueden hacer tareas fiables; no rellenar los vacíos.
3. Crear `tasks.md` con tareas cortas y atómicas con casilla. Ordenar las dependencias desde datos/dominio, pasando por aplicación/persistencia, API/UI y pruebas según corresponda.
4. Enlazar cada tarea con sus IDs de requisito y criterios de aceptación. Incluir trabajo de pruebas significativo y verificación final con `dotnet build`/`dotnet test`.
5. Mantener todas las tareas sin marcar. No modificar `src/` ni `tests/`.
6. Reportar el número de tareas, los requisitos sin cubrir y el siguiente comando.
