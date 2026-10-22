# Feature: Programación web de médicos y especialidades

- **Módulo propietario:** Experiencia web.
- **Módulos afectados:** catálogos de médicos/especialidades y agenda.
- **Rama:** `feature/web-catalog-management`.
- **Fecha:** 2026-10-19.
- **Estado:** especificación aprobada.
- **Solicitud original:** “Quiero implementar todo eso”.

## Necesidad y alcance

Gestionar especialidades y médicos desde la interfaz existente, sin requerir clientes API externos. Reutilizar los endpoints y reglas de catálogo existentes.

## Requisitos funcionales

- **RF-WEB-CAT-01:** Listar especialidades y médicos activos por defecto y permitir consultar también inactivos.
- **RF-WEB-CAT-02:** Crear y editar especialidades con validación de campos requeridos.
- **RF-WEB-CAT-03:** Crear y editar médicos con licencia, nombres, apellidos y especialidad activa.
- **RF-WEB-CAT-04:** Desactivar especialidades y médicos con confirmación; conservar filas, explicar conflictos de citas futuras y no ofrecer borrado físico.
- **RF-WEB-CAT-05:** Refrescar los catálogos usados por agenda/reservas tras altas, ediciones o desactivaciones exitosas.
- **RF-WEB-CAT-06:** Presentar carga, éxito, error, estados vacíos, errores API en español, prevenir envíos duplicados y funcionar desde 360 px.

## Aceptación

| Dado | Cuando | Entonces |
|---|---|---|
| Se abre gestión de catálogos | Se carga la vista | Lista catálogos activos, con opción para ver inactivos (RF-WEB-CAT-01) |
| Operador completa datos válidos | Crea/edita especialidad o médico | Se guarda mediante API y se actualiza el listado (RF-WEB-CAT-02, 03, 05) |
| Faltan datos requeridos o el servidor rechaza una operación | Se intenta guardar | Se explica el error en español y no se muestra éxito falso (RF-WEB-CAT-02, 03, 06) |
| Operador desactiva un registro | Confirma la acción | Se conserva como inactivo; conflictos se explican; no hay opción de eliminación (RF-WEB-CAT-04) |
| Se usa la vista en móvil | Se interactúa con catálogos | Sigue siendo usable desde 360 px y muestra estados/evita doble envío (RF-WEB-CAT-06) |
