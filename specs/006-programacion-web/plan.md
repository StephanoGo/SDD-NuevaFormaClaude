# Plan: Gestión web de catálogos clínicos

## Diseño

Añadir una sección SPA para gestión de especialidades y médicos, usando `/api/specialties` y `/api/doctors` existentes. API client incorpora alta/edición/desactivación. Formularios accesibles incluyen selección de especialidad activa; listado permite mostrar inactivos y evita DELETE. Tras cambios, refrescar listas y selectores de Agenda/Reserva. Los endpoints y reglas backend no se alteran.

## Persistencia y verificación

Persistencia SQL existente, sin cambios de esquema. Pruebas unitarias/HTTP existentes garantizan contratos; añadir cobertura web automatizable si la solución tiene harness compatible y ejecutar build/test. Revisar responsive, carga, vacío, errores y confirmación.
