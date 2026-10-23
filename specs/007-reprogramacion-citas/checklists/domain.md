# Checklist de dominio — Reprogramación de citas

Los ítems `[ ]` son comprobaciones de implementación/prueba pendientes, no defectos actuales. Las aclaraciones funcionales están resueltas y aprobadas en `spec.md`. Vinculado a RF-REPRO-01–05.

- [ ] El ID, paciente y motivo de consulta se conservan; solo cambia el bloque vigente (RF-REPRO-01).
- [ ] Motivo del cambio se recorta, es obligatorio y tiene entre 1 y 500 caracteres (RF-REPRO-01, 05).
- [ ] Destino existente, distinto, `Available`, mismo médico y con inicio estrictamente futuro en `America/Lima` al confirmar (RF-REPRO-02).
- [ ] Cita `Pending` se puede mover aunque el horario original pasó; `Attended`/`Cancelled` no se alteran (RF-REPRO-04).
- [ ] Origen faltante o no `Reserved` se rechaza, reporta la inconsistencia y no modifica cita, bloques ni historial ni repara datos (RF-REPRO-03; AC-REPRO-09).
- [ ] Cita, estado origen/destino e inserción de evento se confirman o revierten juntos (RF-REPRO-03, 05).
- [ ] Carreras con reserva, cancelación, atención y otra reprogramación no duplican destino ni pisan al ganador (RF-REPRO-03, 04).
- [ ] Cada operación exitosa inserta un evento con horario anterior/nuevo, motivo y UTC; historial completo ordenado y mostrado en hora clínica (RF-REPRO-05).
- [ ] Timeout no provoca reenvío; consulta exitosa confirma el estado y consulta fallida informa resultado no confirmado sin afirmar éxito/fracaso (RF-REPRO-06; AC-REPRO-10).
- [ ] Migración aditiva conserva filas existentes y no inventa historial previo (RF-REPRO-05).

## Aclaraciones de negocio

Resueltas: inicio destino estrictamente futuro; timeout con consulta de detalle y sin reintento automático; origen inconsistente rechazado sin reparación; timestamp almacenado en UTC y presentado en zona clínica. No quedan preguntas de negocio abiertas.

## Verificación pendiente

Validar mediante pruebas con reloj controlado, repositorios y SQL Server real. Pruebas SQL solo contra `BD_MedicinaGen_Tests` según `specs/003-persistencia-sql-server/plan.md`; no usar la base demo. No hay pruebas declaradas como ejecutadas por este checklist.
