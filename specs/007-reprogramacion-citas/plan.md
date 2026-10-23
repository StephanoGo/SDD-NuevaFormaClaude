# Plan: Reprogramación de citas

## Diseño y límites

Mantener las capas existentes: el Dominio valida elegibilidad y campos; Application coordina la solicitud y arma la respuesta; API traduce formato HTTP; Infrastructure garantiza la operación atómica en la persistencia; la SPA añade el flujo a Consulta de Citas/detalle. Citas es propietario de Appointment y del historial; Agenda conserva la propiedad de ScheduleSlot. Atenciones no cambia su contrato clínico, pero su transición a `Attended` debe competir correctamente con reprogramación.

Persistencia de producción es SQL Server 2025 con EF Core 10 y migraciones versionadas, conforme a la excepción ya aprobada en la feature 003; no agregar tecnologías ni dependencias. El repositorio en memoria sigue implementando el mismo contrato para pruebas unitarias.

## Operación atómica y concurrencia

El repositorio expone una sola operación `RescheduleAsync`; nunca se persisten por separado la cita y los estados de los dos bloques. En SQL, una transacción verifica y adquiere condicionalmente el estado `Pending` de la cita y `Available` del destino, comprueba que el origen actual sigue `Reserved`, y que origen/destino pertenecen al mismo médico y son distintos. Actualiza el `ScheduleSlotId` de la misma cita, libera el origen, reserva el destino e inserta el evento histórico; confirma solo si todas las condiciones y escrituras tienen éxito. Un resultado fallido o excepción revierte todas las escrituras. Orden de adquisición estable (cita y bloques ordenados por ID) para reducir interbloqueos; los deadlocks transitorios no se traducen en éxito y la transacción abortada deja el estado íntegro.

Las transiciones de cita usan compare-and-set sobre `Pending`, compartido por reprogramación, atención y cancelación: solo una gana. La reserva ordinaria y la reprogramación compiten por `Available` del destino mediante compare-and-set; dos citas no lo pueden ocupar. Dos reprogramaciones de una cita compiten por el estado/slot origen esperado y la ganadora no se sobrescribe. Ante perdedora: error de conflicto sin cambios propios. Las consultas previas de Application son informativas; la persistencia vuelve a verificar todas las condiciones.

La implementación SQL debe ejecutar integración de concurrencia real para demostrar estas garantías; no confiar en locks de proceso ni en el proveedor in-memory. Usar transacción SQL Server más actualizaciones condicionadas (y locks de fila equivalentes a patrones existentes) para serializar contendientes. No efectuar liberación del origen antes de asegurar destino; aunque el orden de escrituras difiera, la transacción no permite visibilidad parcial. Si el origen falta o no está `Reserved`, rechazar sin reparación automática. Ante timeout, la web consulta el detalle y no reintenta automáticamente.

## Zona horaria y validación

Application usa `ClinicClock` existente (`America/Lima`) para determinar futuro y capturar la marca temporal del cambio. El destino es futuro al momento de confirmar (fecha posterior a hoy local o, hoy, inicio estrictamente posterior a ahora local). No se exige que el bloque anterior sea futuro. Validar en servicio/dominio: motivo recortado, no vacío, máximo 500 caracteres; ID positivo/formato en API. Persistencia repite las comprobaciones de estado relacional, no sustituye las reglas de negocio.

## Interfaz

En los resultados de búsqueda y detalle, ofrecer “Reprogramar” solo para `Pending`. Seleccionar destino de los bloques disponibles del mismo médico (agenda y fecha), introducir motivo de cambio, y mostrar revisión explícita con horario actual y nuevo antes de confirmar. La respuesta exitosa actualiza cita, búsqueda y agenda; errores de estado/disponibilidad aparecen en español conservando código. Deshabilitar envío durante la petición. El detalle presenta la secuencia histórica cronológica, convirtiendo la marca UTC a `America/Lima`.

## Persistencia y migración

Añadir entidad/tabla de historial append-only vinculada a `Appointments`, con FK al bloque de origen y destino, motivo del cambio y fecha/hora de registro; conservar eventos al margen de cuál sea el bloque actual. La cita conserva su ID y relación paciente/motivo; el bloque actual sigue siendo la FK única existente `Appointments.ScheduleSlotId`. Migración aditiva versionada: crear tabla e índices/FK, sin transformar ni eliminar citas/bloques existentes, sin seed. Detalle de campos/precisión/índices en `data-model.md`.

## Errores

Los errores de negocio mantienen `Result` y ProblemDetails con `code`; la API reserva `VALIDATION_FORMAT` para parseo/estructura. Usar errores estables: `APPOINTMENT_NOT_FOUND` (404), `APPOINTMENT_NOT_PENDING` (409), `SLOT_NOT_FOUND` (404), `SLOT_NOT_AVAILABLE` (409, incluye destino pasado/no disponible/otro médico y carrera), y error específico `APPOINTMENT_RESCHEDULE_REASON_REQUIRED` / `APPOINTMENT_RESCHEDULE_REASON_TOO_LONG` (400). Mismo bloque y carrera sobre cita devuelven conflicto `APPOINTMENT_RESCHEDULE_CONFLICT` (409). No se persisten cambios en todos los errores. Si durante implementación algún error existente cubre inequívocamente el caso, mantenerlo en vez de multiplicar códigos y actualizar contrato antes de tareas.

## Módulos afectados

- Citas: regla, aplicación, contrato y propietario del historial.
- Agenda: disponibilidad/estado de bloques coordinado por la operación de Citas.
- Atenciones: transición a atendida compite sobre el mismo estado pendiente, sin modificar su payload clínico.
- Experiencia web: selección, confirmación, presentación de historia y errores.
- Infraestructura: repositorios in-memory y SQL Server, migración y pruebas de transacción.

## Verificación

Pruebas de dominio/servicio con reloj fijo: elegibilidad, futuro local, longitud/vacío, conservar datos e historial. Pruebas de repositorio in-memory: éxito/fallo atómico y transiciones inválidas. Integración SQL: migración aditiva, atomicidad ante fallo, historial persistido/ordenado, mismo médico, origen y destino consistentes; carreras de dos reprogramaciones al destino, reprogramación/reserva, reprogramación/cancelación, reprogramación/atención y reprogramación/reprogramación sobre la misma cita. Pruebas HTTP para payload, estados, errores y lectura del historial. Verificación manual/browser del flujo web responsive, confirmación y refresco. Ejecutar `dotnet build` y `dotnet test`; pruebas SQL solo contra `BD_MedicinaGen_Tests` según política de feature 003, nunca contra la base de demo, y reportar bloqueos por base no disponible.

## Contratos

Ver [`contracts/rescheduling-contracts.md`](contracts/rescheduling-contracts.md). Los contratos comunes de citas, ProblemDetails y persistencia se reutilizan por referencia desde [`../001-citas-medicas-core/contracts/api-contracts.md`](../001-citas-medicas-core/contracts/api-contracts.md); no se copian aquí.
