# Persistencia SQL Server para la demo clínica

- **Módulo:** Persistencia (transversal; afecta Pacientes, Agenda, Citas y Atenciones)
- **Rama:** `003-persistencia-sql-server`
- **Fecha:** 2026-10-15
- **Estado:** Specified

## Contexto y decisiones

Reemplazar la persistencia en memoria por SQL Server local para la demo de una clínica: entidades y catálogos con FK, sin roles ni usuarios, sin seed al iniciar y esquema creado/actualizado al arrancar con migraciones versionadas que no borran datos. Motor confirmado: **SQL Server 2025 Developer en localhost**, base `BD_MedicinaGen`, autenticación integrada de Windows (sustituye 2022 Standard). No recrear ni borrar bases ni reescribir la persistencia existente.

- EF Core 10 para persistencia relacional y migraciones versionadas (excepción aprobada); `System.Security.Cryptography.Pkcs` 9.0.21 solo por seguridad del transitivo.
- Médicos y especialidades: alta, edición y desactivación; nunca eliminación.
- La desactivación se bloquea con citas futuras no terminadas; `Attended`/`Cancelled` terminan; bloques `Available` sin cita no bloquean.
- Listados ocultan inactivos por defecto; filtro explícito los incluye.
- Médico de especialidad inactiva no crea agenda ni reserva.
- Atención persistida en tabla propia vinculada a la cita; máximo una por cita.
- Si SQL Server no está disponible o falla una migración, la aplicación no inicia.

## Alcance

- Persistir pacientes, médicos, especialidades, bloques, citas y atenciones en SQL Server local, conservando relaciones y operaciones atómicas.
- Crear/actualizar el esquema al iniciar con migraciones versionadas y preservando datos; sin seed al iniciar (fixtures solo en tests).
- Alta/edición/desactivación de médicos y especialidades por API, sin eliminación; campos requeridos no vacíos, especialidad del médico existente y activa, sin formatos/longitudes/unicidad extra.

## Fuera de alcance

- Múltiples clínicas o sedes.
- Usuarios, autenticación, autorización o tabla de roles.
- Eliminar médicos o especialidades.
- Cargar datos de demostración al iniciar.

## Aclaraciones pendientes

- `BD_MedicinaGen_Tests` es la base dedicada de pruebas: preexistente, provista por `CLINIC_TEST_CONNECTION_STRING` y confirmada vacía para AC-PER-04. Si no está disponible o no se confirma vacía, las pruebas SQL se reportan bloqueadas sin limpiar la base ni usar `BD_MedicinaGen`.
- Solo existe la migración inicial `202610150001_InitialClinicSchema`; la prueba de actualización con datos (AC-PER-15) queda pendiente hasta una migración posterior real.

## Requisitos funcionales

- **RF-PER-01:** Almacenar y recuperar pacientes, médicos, especialidades, bloques, citas y atenciones en SQL Server 2025 Developer local `BD_MedicinaGen` con autenticación integrada de Windows.
- **RF-PER-02:** Mantener integridad con relaciones de base de datos; conservar atomicidad y concurrencia de reservas y atenciones.
- **RF-PER-03:** Al iniciar, aplicar migraciones versionadas para crear o actualizar el esquema sin borrar datos; si la conexión o una migración falla, impedir el inicio.
- **RF-PER-04:** No insertar datos ficticios ni seed al iniciar; los datos de prueba solo en tests.
- **RF-PER-05:** Permitir crear y editar médicos y especialidades, y desactivar cualquiera de ellos; no eliminarlos.
- **RF-PER-06:** Persistir la atención como registro relacionado con la cita, con diagnóstico y receta según el módulo Atenciones.
- **RF-PER-07:** No incorporar usuarios, autenticación, autorización ni roles.
- **RF-PER-08:** Crear/editar médicos y especialidades con campos obligatorios y no vacíos; la especialidad del médico debe existir y estar activa; sin formatos, longitudes ni unicidad adicionales.
- **RF-PER-09:** Desactivar médicos y especialidades sin borrarlos; rechazar si hay citas futuras que no estén `Attended` ni `Cancelled`; bloques `Available` sin cita no bloquean.
- **RF-PER-10:** Los listados excluyen inactivos por defecto y permiten incluirlos con opción explícita.
- **RF-PER-11:** Máximo una atención por cita; concluir una cita ya atendida no crea otra.
- **RF-PER-12:** Un médico de especialidad inactiva no puede crear bloques de agenda ni reservar citas.

## Criterios de aceptación

AC-PER-01–15 viven en [`tasks.md`](tasks.md#criterios-de-aceptación). Bloqueados sin base dedicada vacía: AC-PER-04/T002. Pendiente hasta una migración posterior real: AC-PER-15. Detalle operativo en [`plan.md`](plan.md) y [`data-model.md`](data-model.md).
