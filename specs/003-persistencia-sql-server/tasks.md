# Tareas: Persistencia SQL Server

Reglas: marcar solo con evidencia. La persistencia SQL existente es la línea base: no reescribir ni cambiar de motor. No crear, recrear ni borrar bases; no limpiar ni truncar filas. Pruebas SQL solo contra `BD_MedicinaGen_Tests` (preexistente, vía `CLINIC_TEST_CONNECTION_STRING`), nunca `BD_MedicinaGen`/`master`; sin conexión válida se ejecutan solo pruebas no SQL y las SQL se reportan bloqueadas (no omitidas). Datos de prueba únicos y conservados.

## Prerrequisito y migración inicial

- [x] **T001** Exigir `CLINIC_TEST_CONNECTION_STRING` y aceptar solo `localhost` / `BD_MedicinaGen_Tests`, sin fallback; inspeccionar el catálogo antes de `MigrateAsync` (permitir vacío sin historial o historial `dbo` reconocido; bloquear el resto). Pruebas locales de política. Evidencia: política 11/11, build correcto, 131/131 no SQL. (RF-PER-03, RF-PER-04; AC-PER-04, AC-PER-06)
- [ ] **T002** Sobre `BD_MedicinaGen_Tests` preexistente y vacía, aplicar la migración inicial y verificar las seis tablas de negocio, `__EFMigrationsHistory` y ausencia de seed/`Users`/`Roles`. **Pendiente:** no hay base vacía disponible; con historial, el caso no aplica y se excluye por filtro. (RF-PER-03, RF-PER-04; AC-PER-04, AC-PER-06)

## Auditoría

- [x] **T003** Auditar configuración, DI y arranque (SQL Server 2025 Developer local, `BD_MedicinaGen`, autenticación integrada, migraciones sin fallback) sin iniciar la app contra la base principal. Evidencia estática + build. (RF-PER-01, RF-PER-03; AC-PER-01, AC-PER-05)
- [x] **T004** Auditar PK/FK, DNI único, reserva/atención únicas, borrado no cascada y ausencia de seed; sin defectos que corregir. Evidencia: modelo/migración + metadatos de solo lectura (6 PK, 5 FK `NO_ACTION`, 3 índices únicos). (RF-PER-02, RF-PER-04, RF-PER-06, RF-PER-07, RF-PER-11; AC-PER-02, AC-PER-06, AC-PER-08, AC-PER-13)
- [x] **T005** Verificar operaciones, búsquedas, orden, atomicidad y errores en SQL seguro. Evidencia: pruebas SQL aplicables aprobadas. (RF-PER-01, RF-PER-02; AC-PER-01, AC-PER-02, AC-PER-03)

## Catálogos y reglas de negocio

- [x] **T006** Pruebas HTTP de alta, edición y desactivación de médicos/especialidades: campos requeridos, especialidad existente/activa, errores estables y ausencia de DELETE. Evidencia: `CatalogHttpTests` + `CatalogServiceTests`. (RF-PER-05, RF-PER-08; AC-PER-07, AC-PER-09)
- [x] **T007** Pruebas de listados: activos por defecto, `includeInactive=true` y rechazo de filtro inválido. Evidencia: `CatalogHttpTests` + `CatalogServiceTests`. (RF-PER-10; AC-PER-12)
- [x] **T008** Desactivación con citas `Pending`/`Attended`/`Cancelled`, bloques libres y carrera con reserva; rechazo sin cambios y éxito conservando la fila. Evidencia: pruebas SQL de los cinco escenarios. (RF-PER-09; AC-PER-10, AC-PER-11)
- [x] **T009** Médico de especialidad inactiva no crea agenda ni reserva, sin escrituras parciales. Evidencia: prueba SQL `InactiveSpecialty_...`. (RF-PER-12; AC-PER-14)
- [x] **T010** Cardinalidad máxima de una atención, transición atómica `Pending`→`Attended`, concurrencia y conservación de diagnóstico/receta. Evidencia: pruebas SQL de ida/vuelta y concurrencia. (RF-PER-02, RF-PER-06, RF-PER-11; AC-PER-03, AC-PER-13)

## Migración posterior y cierre

- [ ] **T011** **Pendiente** hasta que exista una migración posterior real distinta de `202610150001_InitialClinicSchema`: probar en base preexistente con datos que la migración nueva los conserva. Reaplicar la inicial no es actualización. No crear/borrar/limpiar. (RF-PER-03; AC-PER-15)
- [x] **T012** Verificar fallo de inicio por conexión/migración y ausencia de seed, tablas o funciones de usuarios, autenticación, autorización y roles. Evidencia: `SqlConfigurationTests` (DI sin fallback, arranque aislado fallido, migración con ensamblado inexistente, modelo sin seed/Users/Roles, rutas sin autorización). Sin usar la base principal. (RF-PER-03, RF-PER-04, RF-PER-07; AC-PER-05, AC-PER-06, AC-PER-08)
- [x] **T013** Ejecutar build y seleccionar pruebas según el preflight (no SQL sin conexión válida; SQL aplicable con historial `dbo` reconocido, excluyendo por filtro el caso inicial). Evidencia: build correcto, 131/131 no SQL/HTTP y 10/10 SQL aplicable; el caso inicial no cuenta como omitido. (RF-PER-01–12; AC-PER-01–14; AC-PER-15 solo con migración posterior real)

## Criterios de aceptación

| ID | Dado | Cuando | Entonces |
|---|---|---|---|
| AC-PER-01 | SQL Server 2025 Developer disponible en localhost y `BD_MedicinaGen` abrible con identidad integrada | Arranca la aplicación | Se conecta y las operaciones usan persistencia SQL (RF-PER-01) |
| AC-PER-02 | Se guarda un registro que referencia otra entidad existente | Se confirma la operación | La relación queda persistida; no se persiste una referencia inexistente (RF-PER-02) |
| AC-PER-03 | Dos solicitudes intentan reservar el mismo bloque o concluir la misma cita concurrentemente | Se procesan | Como máximo una reserva y una conclusión efectiva (RF-PER-02) |
| AC-PER-04 | Una persona proveyó `BD_MedicinaGen_Tests`, dedicada a pruebas, confirmada vacía y sin esquema ni historial | Se aplica por primera vez la migración inicial | Se crea el esquema y no se insertan datos seed; no se crea, limpia ni elimina ninguna base. Si no está disponible o no se confirma vacía, la verificación queda bloqueada, sin usar `BD_MedicinaGen` (RF-PER-03, RF-PER-04) |
| AC-PER-05 | SQL Server no está disponible o una migración falla | Arranca la aplicación | El inicio falla y no opera con persistencia en memoria (RF-PER-03) |
| AC-PER-06 | La aplicación inicia por primera vez | Se inspeccionan los datos de negocio | No se cargaron datos seed/ficticios; fixtures solo en tests (RF-PER-04) |
| AC-PER-07 | Un médico o especialidad debe incorporarse o corregirse | Un cliente crea o edita por la API | Los cambios se persisten y no existe endpoint de eliminación (RF-PER-05) |
| AC-PER-08 | Se inicia o consulta el esquema/capacidades de acceso | Se inspecciona la solución | No hay tabla de roles ni funcionalidad de usuarios/autenticación (RF-PER-07) |
| AC-PER-09 | Se crea/edita un médico/especialidad con campos requeridos vacíos, o un médico referencia especialidad inexistente/inactiva | Se envía solicitud API | Se rechaza sin modificar datos y con el error contractual (RF-PER-08) |
| AC-PER-10 | Médico/especialidad tiene cita futura no terminada | Se solicita desactivarlo | Se rechaza y permanece activo; `Attended`/`Cancelled` no bloquean ni bloques `Available` libres (RF-PER-09) |
| AC-PER-11 | No quedan citas futuras no terminadas asociadas | Se desactiva médico/especialidad | La fila se conserva y queda inactiva (RF-PER-09) |
| AC-PER-12 | Se consulta un listado sin filtro o con inclusión explícita | Se procesan médicos/especialidades | Por defecto solo activos; `includeInactive=true` incluye inactivos (RF-PER-10) |
| AC-PER-13 | Una cita ya tiene atención o su estado ya es `Attended` | Se intenta registrar otra atención | Se rechaza y permanece una sola atención vinculada (RF-PER-06, RF-PER-11) |
| AC-PER-14 | Un médico activo está vinculado a una especialidad inactiva | Intenta crear un bloque o reservar uno existente | Se rechaza y no se crea bloque ni cita (RF-PER-12) |
| AC-PER-15 | Existe una migración anterior real aplicada a la base de pruebas y hay datos guardados | Se aplica una migración posterior real | La migración se registra y conserva los datos preexistentes; no se simula reaplicando la inicial (RF-PER-03) |
