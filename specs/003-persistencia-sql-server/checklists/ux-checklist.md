# Checklist UX: cliente de catálogos

| Comprobación | Estado | Referencia |
|---|---|---|
| El cliente web obtiene médicos desde API persistida, sin lista fija. | Cubierto en código; aceptación UX no evaluada aquí | RF-PER-01, RF-PER-04; `app.js` usa `apiClient.getDoctors()` y no contiene `DOCTORS` hardcodeados. |
| La API permite alta, edición y desactivación, sin eliminación. | Verificar respuestas en pruebas API | RF-PER-05; contrato de catálogos. |
| Los listados ocultan inactivos por defecto e incluyen con `includeInactive=true`. | Verificar en integración | RF-PER-10. |
| Errores API y estados vacíos sin alterar el contrato compartido. | Verificar en integración | Flujos administrativos son API; sin panel nuevo. |
| Gestión de catálogos expuesta por API, sin panel administrativo nuevo. | Cubierto por alcance | RF-PER-05 y contrato de catálogos. |

No agrega comportamiento fuera de los contratos aprobados. El cliente carga médicos desde la API y muestra opción vacía si no hay médicos. Sin conexión SQL válida o con estado no confirmado, se excluyen por filtro las pruebas SQL y se reportan bloqueadas (no omitidas por xUnit); con historial EF `dbo` reconocido se ejecutan las aplicables, excluyendo solo el caso de migración inicial. La base preexistente vacía es requisito exclusivo de AC-PER-04/T002; excluir ese caso no lo verifica.
