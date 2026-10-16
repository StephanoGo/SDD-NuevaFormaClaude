---
name: speckit-implement
description: Implementar una feature aprobada desde sus tareas analizadas y verificarla con la suite de pruebas completa.
disable-model-invocation: true
metadata:
  opencode/slash: "true"
---

Implementar $ARGUMENTS (o la feature actual) solo después de que su último resultado de `/speckit-analyze` sea `READY FOR IMPLEMENTATION`.

1. Leer todos los artefactos de la feature e implementar solo las tareas sin marcar respaldadas por IDs de requisitos aprobados. Si la spec o el diseño son insuficientes, detenerse y volver al comando SDD correspondiente.
2. Este es el único comando de Speckit autorizado a crear o modificar archivos ejecutables en `src/` y `tests/`. Mantener los cambios dentro del alcance; añadir/actualizar pruebas para los casos de aceptación y checklist.
3. Ejecutar `dotnet build` y `dotnet test` desde la raíz del repositorio. Diagnosticar fallos; no marcar trabajo relacionado como completo salvo que ambos pasen.
4. Marcar una tarea `[x]` solo después de que su implementación y las pruebas relevantes pasen. Dejar las demás tareas sin marcar.
5. Reportar los archivos modificados, los IDs de tareas completadas y pendientes, los resultados de build/pruebas y cualquier bloqueo. No hacer commit ni publicar cambios.
