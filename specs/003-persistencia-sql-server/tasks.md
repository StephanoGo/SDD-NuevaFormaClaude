# Tareas: Persistencia SQL Server

Todas las tareas están pendientes. Las referencias `AC-PER-xx` corresponden a los criterios de aceptación del mismo número de RF en `spec.md`.

## Modelo y almacenamiento

- [x] **T001** Añadir dependencia EF Core 10 y proveedor SQL Server únicamente en Infrastructure y configurar DbContext; documentar el alcance de excepción. (RF-PER-01, AC-PER-01)
- [x] **T002** Mapear Patient, Doctor, Specialty, ScheduleSlot, Appointment y Attention con PK/FK y restricciones aprobadas, incluida una sola Attention por cita y sin unicidad extra no aprobada. (RF-PER-02, RF-PER-06, RF-PER-08, RF-PER-11; AC-PER-02, AC-PER-09, AC-PER-13)
- [x] **T003** Crear migración inicial versionada para el esquema relacional sin filas de negocio seed. (RF-PER-03, RF-PER-04; AC-PER-04, AC-PER-06)
- [ ] **T004** Configurar conexión local `BD_MedicinaGen` con Windows Integrated Authentication y ejecutar migraciones al arrancar antes de servir API; propagar fallos sin fallback en memoria. (RF-PER-01, RF-PER-03; AC-PER-01, AC-PER-04, AC-PER-05)
- [ ] **T005** Reemplazar repositorios en memoria por repositorios SQL y conservar resultados, búsquedas, orden y errores funcionales existentes. (RF-PER-01, RF-PER-02; AC-PER-01, AC-PER-02)
- [x] **T006** Implementar creación de bloques con protección concurrente contra solapamientos, preservar intervalos contiguos y rechazar médico cuya especialidad está inactiva. (RF-PER-02, RF-PER-12; AC-PER-02, AC-PER-03, AC-PER-14; Agenda RF-AGE-01–03)
- [x] **T007** Implementar reserva compare-and-set transaccional del bloque e inserción de cita, incluidas FK e integridad referencial; rechazar especialidad inactiva. (RF-PER-02, RF-PER-12; AC-PER-02, AC-PER-03, AC-PER-14; Citas RF-CIT-01–04)
- [x] **T008** Implementar escritura transaccional de atención y cambio Pending→Attended, con una restricción que impida una segunda atención por cita. (RF-PER-02, RF-PER-06, RF-PER-11; AC-PER-03, AC-PER-13; Atenciones RF-ATT-01–04)

## Catálogos y API

- [x] **T009** Añadir persistencia/repositorio de Specialty y asociar Doctor por FK; mapear el valor de especialidad devuelto por la API sin perder compatibilidad de respuestas vigentes. (RF-PER-01, RF-PER-02, RF-PER-08; AC-PER-01, AC-PER-02, AC-PER-09)
- [ ] **T010** Implementar alta/edición de especialidad y endpoints `GET/POST/PUT /api/specialties`, validación requerida y respuestas/códigos del contrato. (RF-PER-05, RF-PER-08; AC-PER-07, AC-PER-09)
- [ ] **T011** Implementar listado de especialidades con `includeInactive=false` por defecto, inclusión explícita y validación de formato. (RF-PER-10; AC-PER-12)
- [ ] **T012** Implementar alta/edición de médico y endpoints `GET/POST/PUT /api/doctors`; validar campos requeridos y FK a especialidad existente y activa. (RF-PER-05, RF-PER-08; AC-PER-07, AC-PER-09)
- [ ] **T013** Implementar listado de médicos con `includeInactive=false` por defecto, inclusión explícita y validación de formato. (RF-PER-10; AC-PER-12)
- [x] **T014** Implementar desactivación lógica de médico: rechazar atómicamente con el error contractual si tiene cita futura no terminada; permitir si futuras citas son Attended/Cancelled y conservar registro. (RF-PER-05, RF-PER-09; AC-PER-07, AC-PER-10, AC-PER-11)
- [x] **T015** Implementar desactivación lógica de especialidad: comprobar citas futuras no terminadas de sus médicos, sin bloquear por bloques libres, y coordinar con reserva concurrente. (RF-PER-05, RF-PER-09; AC-PER-07, AC-PER-10, AC-PER-11)
- [ ] **T016** Verificar por pruebas API/esquema que no hay endpoints de borrado ni tablas/funciones de usuarios, autenticación, autorización o roles. (RF-PER-05, RF-PER-07; AC-PER-07, AC-PER-08)
- [ ] **T017** Retirar las listas de médicos hardcodeadas/seed de la aplicación y conectar consumidores web con endpoints persistidos. (RF-PER-01, RF-PER-04; AC-PER-01, AC-PER-06)

## Pruebas con SQL Server real y verificación

- [ ] **T018** Añadir suite de integración de repositorios que ejecute contra SQL Server 2022 Standard local: persistencia tras reinicio, FK, DNI único, búsquedas, filtros de catálogos y ausencia de tablas de usuarios/roles. (RF-PER-01, RF-PER-02, RF-PER-07, RF-PER-10; AC-PER-01, AC-PER-02, AC-PER-08, AC-PER-12)
- [ ] **T019** Añadir pruebas concurrentes reales de SQL Server para reservas del mismo bloque, solapamiento de agenda, carrera entre desactivación y reserva, y rechazo de agenda/reserva para especialidad inactiva. (RF-PER-02, RF-PER-09, RF-PER-12; AC-PER-03, AC-PER-10, AC-PER-14)
- [ ] **T020** Añadir pruebas SQL/API de alta/edición, campos requeridos, especialidad faltante/inactiva, errores estables, listados default/includeInactive y bloqueo/permitido de desactivación. (RF-PER-05, RF-PER-08, RF-PER-09, RF-PER-10; AC-PER-07, AC-PER-09, AC-PER-10, AC-PER-11, AC-PER-12)
- [x] **T021** Añadir pruebas SQL de una atención por cita, estados inválidos, concurrencia, atomicidad y preservación de diagnóstico/receta. (RF-PER-02, RF-PER-06, RF-PER-11; AC-PER-03, AC-PER-13)
- [ ] **T022** Añadir pruebas de migración inicial y actualización desde versión anterior con datos no vacíos, comprobando conservación; probar fallo de conexión/migración y ausencia de seed. (RF-PER-03, RF-PER-04; AC-PER-04, AC-PER-05, AC-PER-06)
- [ ] **T023** Ejecutar `dotnet build` y `dotnet test` con SQL Server 2022 Standard local configurado; registrar cualquier requisito ambiental que impida completar pruebas reales. (RF-PER-01–12; AC-PER-01–14)
