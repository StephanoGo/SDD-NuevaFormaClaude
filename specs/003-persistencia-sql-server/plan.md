# Plan: Persistencia SQL Server para la demo clínica

## Arquitectura y decisiones

- Mantener las capas existentes; `CitasMedicas.Infrastructure` compone la persistencia SQL. Dominio y aplicación conservan reglas e invariantes.
- EF Core 10 con proveedor SQL Server y migraciones versionadas, ejecutadas al iniciar antes de servir peticiones (excepción aprobada a la constitución, limitada a esto). `System.Security.Cryptography.Pkcs` 9.0.21 se fija solo como corrección de seguridad del transitivo. No añadir otras tecnologías.
- Motor objetivo: SQL Server 2025 Developer en localhost, `BD_MedicinaGen`, autenticación integrada de Windows. No cambiar de motor ni añadir credenciales de usuario.
- Migraciones aditivas/no destructivas: no borrar datos ni tablas; transformaciones conservan valores previos.
- Si la conexión o una migración falla, propagar error de arranque; sin fallback en memoria.
- Reserva de bloque e inserción de cita en una transacción con compare-and-set de `Available`; registro de atención en transacción condicionada a cita `Pending`. Probar aislamiento/concurrencia en SQL Server, no asumirlo.
- La API conserva sus contratos y añade alta/edición de médicos y especialidades, filtro `includeInactive` y desactivación bloqueada por citas futuras no terminadas (`Attended`/`Cancelled` terminan; bloques `Available` libres no bloquean). Desactivación y reserva se coordinan. Especialidad inactiva impide agenda/reserva. Sin endpoints de eliminación.

## Módulos afectados

- **Pacientes:** persistir y conservar unicidad/búsqueda.
- **Agenda:** persistir médicos, especialidades y bloques; mantener actividad y no solapamiento.
- **Citas:** persistir cita y bloque con reserva atómica.
- **Atenciones:** persistir atención en tabla propia, coordinada con el cambio de estado de la cita.
- **Experiencia web:** médicos desde la API; catálogos gestionados por API.
- **Infraestructura:** conexión, migraciones, repositorios SQL y fallo de inicio.

## Propiedad e invariantes

- `Patient`: DNI único. `Specialty`: catálogo con estado, se conserva al desactivar. `Doctor` referencia especialidad activa y mantiene su estado.
- `ScheduleSlot`: referencia médico; el solapamiento por médico/fecha se evita en el repositorio (no basta la FK).
- `Appointment`: referencia paciente y bloque; el médico se obtiene por el bloque.
- `Attention`: cardinalidad uno a cero/uno por cita; diagnóstico requerido, receta opcional; alta y cambio de estado se confirman o revierten juntos.
- Sin tabla de clínica, roles ni usuarios; sin seeds en migraciones ni al iniciar.

## Errores y operación

- Errores de negocio conservan `Result` y códigos HTTP; las violaciones de unicidad/estado se traducen a errores estables.
- Errores de conexión/migración son de infraestructura de arranque, no de negocio.
- No borrar tablas ni truncar datos en migraciones.

## Verificación

- Pruebas funcionales en SQL Server 2025 Developer local: relaciones/FK, una atención por cita, consultas, persistencia entre reinicios; no sustituir por mocks/in-memory.
- Probar carrera de reservas y de dos atenciones para la misma cita; sin estados parciales.
- **Pruebas SQL:** base dedicada preexistente `BD_MedicinaGen_Tests` vía `CLINIC_TEST_CONNECTION_STRING`; el harness valida `localhost`, catálogo exacto y estado (vacío sin historial, o historial `dbo` reconocido) antes de `MigrateAsync`. Nunca `BD_MedicinaGen`/`master`, sin fallback, sin crear/borrar/limpiar bases. Datos únicos por ejecución y conservados.
- **Selección:** sin conexión válida → solo pruebas no SQL, SQL reportadas bloqueadas (no omitidas). Con historial `dbo` reconocido → pruebas SQL aplicables excluyendo por filtro `MigrateAsync_OnFreshDatabase_CreatesEmptySchemaWithoutUserOrRoleTables`; esa exclusión no verifica T002. Con base vacía → ejecutar por separado el caso inicial (AC-PER-04/T002).
- **AC-PER-15:** solo con una migración posterior real sobre base con migración previa y datos; reaplicar la inicial no es actualización. T011 pendiente mientras solo exista la inicial.
- Fallo de arranque por conexión/migración sin redirigir a `master`/`BD_MedicinaGen`; inicio limpio sin datos de negocio; pruebas HTTP de catálogos, filtro, bloqueo por citas futuras y no eliminación.

## Interfaces

- No cambian los contratos HTTP compartidos: ver [`../001-citas-medicas-core/contracts/api-contracts.md`](../001-citas-medicas-core/contracts/api-contracts.md).
- Catálogos y repositorios: ver [`contracts/persistence-and-catalog-contracts.md`](contracts/persistence-and-catalog-contracts.md).
