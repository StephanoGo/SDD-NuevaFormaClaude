# Tareas: Reprogramación de citas

Las tareas siguen el diseño de esta feature y mantienen las reglas de persistencia segura de [`003-persistencia-sql-server/plan.md`](../003-persistencia-sql-server/plan.md). Todas quedan pendientes; marcar solo con evidencia. No modificar esta lista como completada hasta que las pruebas correspondientes pasen.

## Contratos y dominio

- [ ] **REPRO-01** Definir el contrato de dominio para validar motivo de cambio (trim, requerido, máximo 500), elegibilidad `Pending`, mismo médico, destino distinto y comienzo futuro en `America/Lima`; especificar errores estables y límites inclusivo/exclusivo según spec. (RF-REPRO-01, 02, 04; AC: motivo inválido, destino inválido, cita pendiente con origen pasado)
- [ ] **REPRO-02** Incorporar representación del evento de reprogramación y salida de historial con origen/destino, motivo y timestamp UTC, conservando compatibilidad de `AppointmentResponse` y significado del motivo clínico. (RF-REPRO-01, 05; AC: conserva ID/paciente/motivo y detalle con historial)
- [ ] **REPRO-03** Extender `IAppointmentRepository` con reprogramación atómica e historial, documentando resultados de cita inexistente, destino inexistente/no disponible y origen inconsistente. (RF-REPRO-02–05; AC: rechazos sin cambios, AC-REPRO-09)

## Aplicación y persistencia en memoria

- [ ] **REPRO-04** Implementar caso de uso en Application: cargar cita/destino, aplicar reglas con `ClinicClock`, normalizar motivo, ejecutar una única escritura y componer detalle con historial. (RF-REPRO-01, 02, 04, 05; AC: éxito, futuro local, datos preservados)
- [ ] **REPRO-05** Implementar semántica atómica de la operación e historial en repositorio en memoria, incluyendo rechazo sin mutaciones cuando origen, destino o estado no son válidos. (RF-REPRO-02–05; AC: todos los fallos preservan estado)
- [ ] **REPRO-06** Añadir migración EF Core aditiva para `AppointmentReschedules`, FKs restrictivas, límite de motivo e índice de consulta ordenada; no alterar filas existentes ni hacer backfill. (RF-REPRO-05; AC: historial persistido, migración conserva esquema/datos)
- [ ] **REPRO-07** Implementar transacción SQL de reprogramación: CAS de cita `Pending`, origen `Reserved`, destino `Available`, mismo médico, actualizar FK y ambos slots, insertar historial; revertir ante cualquier fallo y rechazar origen inconsistente sin reparación. (RF-REPRO-02–05; AC: atomicidad y conflictos, AC-REPRO-09)
- [ ] **REPRO-08** Implementar lectura SQL del historial completo ordenado por timestamp e ID, y mapear filas a DTO de detalle. (RF-REPRO-05; AC: historial de múltiples cambios)

## API

- [ ] **REPRO-09** Exponer `POST /api/appointments/{appointmentId}/rescheduling` con request `scheduleSlotId`/`reason`, validación de formato y mapeo de códigos HTTP definidos en contrato. (RF-REPRO-01–04; AC: éxito, inválidos, cita inexistente/no pendiente, conflictos)
- [ ] **REPRO-10** Incluir historial en `GET /api/appointments/{appointmentId}` y en respuesta exitosa de reprogramación, conservando campos y convenciones compartidas; serializar `changedAt` ISO 8601 UTC. (RF-REPRO-05; AC: consulta de detalle completa)

## Interfaz web

- [ ] **REPRO-11** Añadir cliente API para reprogramar y consumir historial; mapear errores de negocio con mensajes españoles y código estable. (RF-REPRO-05, 06; AC: errores comprensibles)
- [ ] **REPRO-12** Añadir flujo en búsqueda/detalle de citas pendientes: elegir fecha y bloque disponible del mismo médico, capturar motivo, revisar origen/destino y confirmar explícitamente. (RF-REPRO-01, 02, 06; AC: revisión previa y restricciones)
- [ ] **REPRO-13** Tras éxito actualizar detalle, listado y disponibilidad; impedir doble envío; ante conflicto conservar contexto y solicitar revisar el estado vigente; ante timeout no reintentar y consultar el detalle; si también falla la consulta, informar resultado no confirmado sin afirmar éxito/fracaso. (RF-REPRO-03, 06; AC: éxito, carrera, AC-REPRO-10)
- [ ] **REPRO-14** Presentar historial completo en orden cronológico, convirtiendo timestamp UTC a `America/Lima`; incluir estado vacío, diseño accesible y responsive desde 360 px. (RF-REPRO-05, 06; AC: historial visible y UX adaptable)

## Pruebas

- [ ] **REPRO-15** Añadir pruebas de dominio/servicio con reloj fijo para estado elegible, origen pasado, destino futuro y límites de motivo (vacío, 500 y 501 caracteres), conservando paciente, ID y motivo clínico. (RF-REPRO-01, 02, 04; AC: válidos e inválidos)
- [ ] **REPRO-16** Añadir pruebas de repositorios en memoria para éxito e historial, fallos sin mutación, estado `Attended`/`Cancelled`, slot fuente inconsistente, mismo slot, slot reservado/pasado/inexistente y médico distinto. (RF-REPRO-02–05; AC: rechazos y atomicidad, AC-REPRO-09)
- [ ] **REPRO-17** Añadir pruebas SQL de integración para migración aditiva con datos existentes, persistencia/orden del historial, rollback ante fallo intermedio y rechazo íntegro de origen ausente/no `Reserved`, usando exclusivamente `BD_MedicinaGen_Tests` conforme a preflight; reportar bloqueadas si no hay conexión válida. (RF-REPRO-03, 05; AC: persistencia y todo-o-nada, AC-REPRO-09)
- [ ] **REPRO-18** Añadir pruebas SQL concurrentes: dos citas por el mismo destino, reserva frente a reprogramación, cancelación frente a reprogramación, atención frente a reprogramación y dos reprogramaciones de una cita; comprobar como máximo un ganador y estado íntegro. (RF-REPRO-03, 04; AC: concurrencia)
- [ ] **REPRO-19** Añadir pruebas HTTP de endpoint, parseo, validación, códigos ProblemDetails, respuesta ampliada y consulta del historial; asegurar que errores no producen escrituras, incluyendo el error por origen inconsistente. (RF-REPRO-01–05; AC: contratos de éxito/error, AC-REPRO-09)
- [ ] **REPRO-20** Verificar manualmente en navegador búsqueda → selección → revisión → confirmación, refresco de disponibilidad, conflictos, timeout/recarga sin reenvío y estado de resultado no confirmado si falla también la consulta; historial y uso responsive/accesible. (RF-REPRO-05, 06; AC: flujo web completo, AC-REPRO-10)

## Documentación y cierre

- [ ] **REPRO-21** Actualizar resumen vigente de Citas y referencias de Agenda/experiencia web, catálogo `specs/README.md` y contrato API compartido sin duplicar contratos; reflejar estado real de la entrega. (RF-REPRO-01–06; AC: documentación coherente)
- [ ] **REPRO-22** Ejecutar `dotnet build` y `dotnet test` desde la raíz; registrar resultados y cualquier prueba SQL bloqueada por falta de base dedicada, sin contarla como aprobada. (RF-REPRO-01–06; verificación de todos los criterios)
