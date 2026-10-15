# Checklist UX: cliente de catálogos

| Comprobación | Estado | Referencia |
|---|---|---|
| El cliente web obtiene médicos/especialidades persistidos desde API y no utiliza datos hardcodeados. | Requerido | RF-PER-01, RF-PER-04; `app.js` contiene médicos predefinidos. |
| La API permite alta, edición y desactivación, sin operación de eliminación. | Requerido | RF-PER-05; contrato de catálogos. |
| Los listados ocultan inactivos por defecto e incluyen al usar `includeInactive=true`. | Requerido | RF-PER-10. |
| Los errores API y estados vacíos se presentan sin alterar el contrato compartido. | Verificar en integración | Los flujos administrativos son API; no se requiere panel administrativo nuevo. |

Este checklist no agrega comportamiento de producto fuera de los contratos aprobados.
