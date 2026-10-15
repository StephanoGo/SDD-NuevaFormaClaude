# Persistencia SQL Server para la demo clínica

- **Módulo:** Persistencia (transversal; afecta Pacientes, Agenda, Citas y Atenciones)
- **Rama:** `003-persistencia-sql-server`
- **Fecha:** 2026-10-15
- **Estado:** Specified

## Solicitud original

> Define una funcionalidad para reemplazar la persistencia en memoria por SQL Server 2022 Standard en localhost, para una demo de una sola clínica.
>
> Alcance: pacientes, citas, agenda y atenciones. Elimina los datos seed de la aplicación; no cargues datos ficticios al iniciar. Conserva los datos de prueba que sean necesarios únicamente dentro de los tests.
>
> La base debe tener tablas para las entidades y catálogos que correspondan —por ejemplo, especialidades— y relaciones mediante claves foráneas donde el dominio lo requiera. No agregues una tabla de roles ni funcionalidad de usuarios/autenticación sin confirmar primero su necesidad: aún no hemos definido qué entidad tendría un rol.
>
> La aplicación debe crear o actualizar automáticamente el esquema de la base de datos al arrancar, usando un mecanismo versionado de migraciones. No borres datos existentes al actualizar el esquema.
>
> Antes de cerrar la especificación, revisa el código actual y pregúntame cualquier decisión de negocio o técnica que siga sin estar definida. No inventes reglas ni modifiques código. Deja explícitos el alcance, lo que queda fuera, las aclaraciones pendientes y los requisitos funcionales con criterios de aceptación Given/When/Then.

## Decisiones aclaradas

- La conexión local usa autenticación integrada de Windows y la base se llama `BD_MedicinaGen`.
- Se aprueba una excepción documentada a la restricción de paquetes de producción para el proveedor SQL y las migraciones versionadas.
- La excepción concreta es EF Core 10 para persistencia relacional en SQL Server y migraciones versionadas; no autoriza otras tecnologías.
- Se autorizó además fijar `System.Security.Cryptography.Pkcs` 9.0.21 únicamente para corregir la vulnerabilidad del paquete transitivo requerido por el proveedor EF SQL Server; no amplía el alcance tecnológico.
- Médicos y especialidades deben poder darse de alta y editarse; ambos se desactivan, no se eliminan.
- La desactivación se bloquea si el médico tiene citas futuras no terminadas, o si la especialidad tiene médicos con citas futuras no terminadas. `Attended` y `Cancelled` son estados terminados; bloques `Available` sin cita no bloquean.
- Un médico cuya especialidad está inactiva no puede crear nueva agenda ni reservar citas.
- Los listados ocultan inactivos por defecto y permiten incluirlos mediante un filtro explícito.
- Diagnóstico y receta se persistirán en una tabla de Atención relacionada con la cita.
- Una cita admite como máximo una Atención.
- Si SQL Server no está disponible o falla una migración, la aplicación no debe iniciar.

## Alcance

- Sustituir por persistencia SQL Server local la persistencia de pacientes, médicos, especialidades, agenda/bloques, citas y atenciones.
- Guardar relaciones del dominio con claves foráneas donde correspondan y conservar las operaciones atómicas necesarias para reservas y registro de atenciones.
- Crear o actualizar el esquema automáticamente al iniciar mediante migraciones versionadas, preservando los datos existentes.
- No cargar datos ficticios ni datos seed al iniciar. Los datos de prueba requeridos se limitan a los tests.
- Permitir alta y edición de médicos y especialidades, y desactivación de ambos desde la aplicación; no ofrecer eliminación de especialidades ni médicos.
- La gestión de catálogos se realiza mediante la API. Los campos actuales de médico (número de licencia, nombres y apellidos) y el nombre de especialidad son obligatorios y no vacíos; no se añaden formatos, longitudes ni reglas de unicidad. La especialidad referenciada por un médico debe estar activa.
- Desactivar médico o especialidad se rechaza mientras quede alguna cita futura no terminada asociada. `Attended` y `Cancelled` cuentan como terminadas; bloques `Available` sin cita no impiden la desactivación.
- Médicos vinculados a especialidades inactivas no pueden crear bloques de agenda ni reservar citas.
- Los listados de médicos y especialidades excluyen inactivos por defecto; un filtro explícito permite incluirlos.
- Usar SQL Server 2022 Standard en localhost, base `BD_MedicinaGen` y autenticación integrada de Windows.
- No iniciar la aplicación si la conexión o la actualización del esquema falla.

## Fuera de alcance

- Múltiples clínicas o configuración de varias sedes.
- Usuarios, autenticación, autorización o tabla de roles.
- Eliminar médicos o especialidades.
- Cargar información de demostración en la base al iniciar.
- Cambios a `src/` o `tests/` durante esta fase de especificación/planificación.

## Aclaraciones pendientes

- Ninguna decisión pendiente reportada. La excepción técnica queda limitada a EF Core 10 para SQL Server y migraciones versionadas.

## Requisitos funcionales

- **RF-PER-01:** La aplicación debe almacenar y recuperar pacientes, médicos, especialidades, bloques de agenda, citas y atenciones en SQL Server local `BD_MedicinaGen`, usando autenticación integrada de Windows.
- **RF-PER-02:** Los registros relacionados deben mantener integridad mediante relaciones de base de datos, y las reservas y atenciones deben conservar sus garantías de atomicidad y concurrencia vigentes.
- **RF-PER-03:** Al iniciar, la aplicación debe aplicar migraciones versionadas para crear o actualizar el esquema sin borrar datos existentes; si la conexión o una migración falla, debe impedir el inicio.
- **RF-PER-04:** La aplicación no debe insertar datos ficticios ni seed al iniciar; los datos de prueba solo pueden residir en los tests.
- **RF-PER-05:** La API debe permitir crear y editar médicos y especialidades, así como desactivar cualquiera de ellos; no debe permitir eliminarlos.
- **RF-PER-06:** La atención debe persistirse como registro relacionado con la cita, incluyendo diagnóstico y receta según las reglas vigentes del módulo Atenciones.
- **RF-PER-07:** La solución no debe incorporar usuarios, autenticación, autorización ni roles.
- **RF-PER-08:** La API debe permitir crear y editar médicos y especialidades con los campos de dominio actuales obligatorios y no vacíos; la especialidad de un médico debe existir y estar activa. No se imponen formatos, longitudes ni unicidad adicionales.
- **RF-PER-09:** La API debe permitir desactivar médicos y especialidades sin borrarlos, y debe rechazar la operación si existen citas futuras asociadas que no estén `Attended` ni `Cancelled`; bloques `Available` sin cita no bloquean.
- **RF-PER-10:** Los listados de médicos y especialidades deben excluir inactivos por defecto y permitir incluirlos con una opción explícita.
- **RF-PER-11:** Cada cita puede tener como máximo una atención; un intento de concluir una cita ya atendida no debe crear otra.
- **RF-PER-12:** Un médico vinculado a una especialidad inactiva no puede crear nuevos bloques de agenda ni reservar citas.

## Criterios de aceptación

| ID | Dado | Cuando | Entonces |
|---|---|---|---|
| AC-PER-01 | SQL Server 2022 Standard está disponible en localhost y `BD_MedicinaGen` puede abrirse con identidad integrada | Arranca la aplicación | Se conecta a esa base y las operaciones de los módulos incluidos usan persistencia SQL (RF-PER-01) |
| AC-PER-02 | Se guarda un registro que referencia otra entidad existente | Se confirma la operación | La relación queda persistida; no se puede persistir una referencia inexistente (RF-PER-02) |
| AC-PER-03 | Dos solicitudes intentan reservar el mismo bloque o concluir la misma cita concurrentemente | Se procesan las solicitudes | Como máximo una reserva y una conclusión efectiva se confirman (RF-PER-02) |
| AC-PER-04 | El esquema está ausente o hay una migración posterior, y existen datos guardados | Arranca la aplicación | Se aplican migraciones versionadas y los datos preexistentes se conservan (RF-PER-03) |
| AC-PER-05 | SQL Server no está disponible o una migración falla | Arranca la aplicación | El inicio falla y no opera con persistencia en memoria (RF-PER-03) |
| AC-PER-06 | La aplicación inicia por primera vez | Se inspeccionan los datos de negocio | No se cargaron datos seed/ficticios; fixtures solo están en tests (RF-PER-04) |
| AC-PER-07 | Un médico o especialidad debe incorporarse o corregirse | Un cliente crea o edita por la API | Los cambios se persisten y no existe endpoint de eliminación (RF-PER-05) |
| AC-PER-08 | Se inicia o consulta el esquema/capacidades de acceso | Se inspecciona la solución | No hay tabla de roles ni funcionalidad de usuarios/autenticación (RF-PER-07) |
| AC-PER-09 | Se crea/edita un médico/especialidad con campos requeridos vacíos, o un médico referencia especialidad inexistente/inactiva | Se envía solicitud API | Se rechaza sin modificar datos y con el error contractual (RF-PER-08) |
| AC-PER-10 | Médico/especialidad tiene cita futura no terminada | Se solicita desactivarlo | Se rechaza y permanece activo; `Attended`/`Cancelled` no bloquean y bloques libres `Available` tampoco (RF-PER-09) |
| AC-PER-11 | No quedan citas futuras no terminadas asociadas | Se desactiva médico/especialidad | La fila se conserva y queda inactiva (RF-PER-09) |
| AC-PER-12 | Se consulta un listado sin filtro o con inclusión explícita | Se procesan médicos/especialidades | Por defecto solo activos; `includeInactive=true` incluye inactivos (RF-PER-10) |
| AC-PER-13 | Una cita ya tiene atención o su estado ya es `Attended` | Se intenta registrar otra atención | Se rechaza y permanece una sola atención vinculada (RF-PER-06, RF-PER-11) |
| AC-PER-14 | Un médico activo está vinculado a una especialidad inactiva | Intenta crear un bloque o reservar uno existente | Se rechaza y no se crea bloque ni cita (RF-PER-12) |

## Revisión del código actual

- `CitasMedicas.Infrastructure` registra repositorios singleton en memoria; `InMemoryDoctorRepository` obtiene su catálogo de `DoctorSeed`.
- La interfaz contiene una lista de médicos predefinidos en `src/CitasMedicas.Api/wwwroot/js/app.js`.
- `Doctor.Specialty` es texto, sin entidad de catálogo; no hay operaciones de aplicación/API para crear o editar médicos o especialidades.
- `Appointment` contiene actualmente `Diagnosis` y `Prescription`; el módulo Atenciones documenta este límite del modelo.
- La configuración actual no define una conexión a base de datos; `Program.cs` llama a `AddInfrastructure()` sin configuración.
- `Doctor` contiene `LicenseNumber`, `FirstName`, `LastName`, `Specialty` (texto) e `IsActive`, sin validaciones de creación; la capa API actual no contiene endpoints de catálogo.
- Los estados de cita incluyen `Pending`, `Attended` y `Cancelled`; no existe endpoint actual para cancelación. Esta feature no añade cancelación, solo considera `Cancelled` un estado terminado para la regla de desactivación.

**Verificación:** planificar contratos y persistencia para los criterios anteriores; implementar pruebas de persistencia, migraciones, preservación de datos, errores de inicio e invariantes atómicas antes de cerrar la implementación.
