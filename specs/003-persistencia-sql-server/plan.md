# Plan: Persistencia SQL Server para la demo clínica

## Arquitectura y decisiones

- Mantener la aplicación por capas y sustituir los repositorios en memoria mediante la composición de `CitasMedicas.Infrastructure`; dominio y aplicación continúan siendo propietarios de reglas e invariantes.
- Usar EF Core 10 con proveedor SQL Server y migraciones EF versionadas, ejecutadas al iniciar antes de servir peticiones. Excepción a la constitución limitada a la necesidad aprobada: persistencia relacional SQL Server y migraciones versionadas. `System.Security.Cryptography.Pkcs` 9.0.21 se fija solo como corrección de seguridad autorizada para la dependencia transitiva del proveedor. No añadir otras tecnologías/paquetes por esa excepción.
- Configurar `BD_MedicinaGen` en localhost con autenticación integrada de Windows. No incluir credenciales ni mecanismos de autenticación de usuario.
- Si conexión, permisos o aplicación de migraciones falla, propagar error de arranque; no activar fallback en memoria.
- Las migraciones son aditivas/no destructivas respecto de los datos existentes. Los cambios que requieran transformación de datos deben conservar los valores anteriores.
- Usar una transacción SQL para reservar bloque e insertar cita; proteger el compare-and-set del estado Available. Registrar la atención en transacción y condicionar la transición a cita Pending. Aplicar y probar aislamiento/concurrencia en SQL Server, no asumir que los locks en memoria actuales bastan.
- La API existente conserva sus contratos funcionales. La API añade gestión de médicos y especialidades, filtro `includeInactive` y desactivación condicionada a que no haya citas futuras no terminadas; `Attended` y `Cancelled` son terminales y bloques libres no bloquean. Desactivación y reserva se coordinan para que una cita nueva no cruce la comprobación. Médico con especialidad inactiva no puede crear agenda ni reservar. No hay endpoints de eliminación.

## Módulos afectados

- **Pacientes:** persistir pacientes y conservar unicidad/búsqueda vigentes.
- **Agenda:** persistir médicos, especialidades y bloques; mantener actividad y regla de no solapamiento.
- **Citas:** persistir cita y bloque con reserva atómica; conservar relaciones con paciente y médico vía bloque.
- **Atenciones:** persistir Atención en tabla propia y coordinar su alta con el cambio de estado de la cita.
- **Experiencia web:** reemplazar médicos hardcodeados por consultas a la API; la gestión de catálogos queda expuesta por API conforme al alcance confirmado.
- **Infraestructura transversal:** conexión, migraciones, repositorios SQL y fallo de inicio.

## Propiedad e invariantes de datos

- `Patient` es propietario de identidad de paciente; DNI único.
- `Specialty` es catálogo con estado activo; se conserva aunque esté desactivado. `Doctor` referencia especialidad activa al crear/editar y mantiene su propio estado activo. Un médico de especialidad inactiva no crea bloques ni reserva. Listados ocultan inactivos salvo inclusión explícita.
- `ScheduleSlot` referencia un médico; unicidad de intervalo no se expresa solo con una FK, por tanto el repositorio debe evitar solapamientos en concurrencia.
- `Appointment` referencia un paciente y un bloque. La relación a médico se obtiene por el bloque.
- `Attention` referencia una cita con cardinalidad uno a cero/uno; diagnosis es requerida y prescription opcional conforme al dominio. Restricción única por cita y la transición de estado/alta de atención se confirman o revierten juntas.
- No crear tabla de clínicas (demo de una clínica), roles ni usuarios. No insertar seeds en migraciones o al iniciar.

## Errores y operación

- Errores de negocio conservan `Result` y códigos HTTP existentes; violaciones de unicidad/estado detectadas al confirmar se traducen a errores estables ya definidos.
- Errores de conexión/migración son errores de infraestructura de arranque, registrados como tales; no se devuelven como error de negocio.
- No borrar tablas ni truncar datos en migraciones. La conexión de runtime necesita permisos para ejecutar las migraciones requeridas; confirmar permisos concretos en el quickstart de implementación.

## Verificación

- Pruebas de repositorio reales contra SQL Server 2022 Standard local: relaciones/FK, restricción de una atención por cita, consultas y persistencia entre reinicios; no sustituir estas pruebas por mocks/in-memory.
- Probar carrera de reservas solapadas y de dos atenciones para la misma cita; asegurar atomicidad y ausencia de estados parciales.
- Probar primera migración, actualización desde una versión previa con datos y fallo de arranque por conexión/migración.
- Comprobar que el inicio limpio no crea datos de negocio; utilizar fixtures solo en tests.
- Pruebas de contratos HTTP de mantenimiento médico/especialidad, errores, filtro `includeInactive`, bloqueo por citas futuras y no eliminación; `dotnet build` y `dotnet test` con SQL Server local.

## Interfaces

- No cambian los contratos HTTP compartidos existentes para pacientes, agenda, citas y atención: ver [`../001-citas-medicas-core/contracts/api-contracts.md`](../001-citas-medicas-core/contracts/api-contracts.md).
- Interfaces nuevas de catálogo y el contrato detallado de repositorios se describen en [`contracts/persistence-and-catalog-contracts.md`](contracts/persistence-and-catalog-contracts.md).
