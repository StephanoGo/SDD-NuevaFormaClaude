# Checklist de requisitos — Reprogramación de citas

Las 16 comprobaciones son una **línea base propuesta** derivada de las instrucciones del flujo. El propietario no confirmó que sean el checklist formal; esta falta de confirmación no bloquea la feature ni agrega requisitos de producto.

| # | Comprobación propuesta | Estado | RF / observación |
|---:|---|---|---|
| 1 | Todo requisito tiene un ID único y estable. | Pasa | RF-REPRO-01–06, sin colisión con RF-CIT. |
| 2 | Cada requisito describe un comportamiento atómico. | Pasa | Aunque algunos RF reúnen validación y resultado, se pueden probar con escenarios trazables. |
| 3 | El actor o componente responsable es identificable. | Pasa | Recepción opera; sistema valida y conserva integridad. |
| 4 | El disparador o condición es explícito. | Pasa | Cita Pending, solicitud y confirmación; destino descrito en RF-REPRO-01–04. |
| 5 | El resultado esperado es explícito. | Pasa | Cita, bloques, historial y resultado web observables. |
| 6 | El enunciado tiene una sola interpretación inequívoca. | Pasa | Timeout, destino futuro y origen inconsistente aclarados en spec. |
| 7 | Términos vagos tienen definición medible o se eliminan. | Pasa | Inicio destino estrictamente posterior al momento actual local. |
| 8 | Términos de dominio son consistentes con la línea base. | Pasa | Pending/Attended/Cancelled, Available/Reserved; zona clínica America/Lima. |
| 9 | Alcance y fuera de alcance son explícitos. | Pasa | Secciones dedicadas en spec. |
| 10 | Se indican precondiciones y estado relevante. | Pasa | Elegibilidad, bloque origen y destino establecidos. |
| 11 | Éxito tiene criterios observables. | Pasa | Escenarios RF-REPRO-01–03 y 05. |
| 12 | Validación y fallo están cubiertos. | Pasa | Motivo, destino, estado, atomicidad y carreras. |
| 13 | Propiedad de datos y límites modulares claros. | Pasa | Citas posee cita/historial; Agenda, bloques; Atenciones compite por estado. |
| 14 | Transiciones e invariantes concuerdan con el modelo. | Pasa | Origen inconsistente se rechaza; escrituras atómicas. |
| 15 | Cada RF está ligado a escenarios Dado/Cuando/Entonces. | Pasa | Todos tienen criterios en spec. |
| 16 | RF no prescribe detalles de implementación innecesarios. | Pasa | La zona clínica es una regla funcional compartida con Agenda; diseño técnico queda fuera de RF. |

## Preguntas pendientes

Ninguna decisión de comportamiento pendiente. La aprobación formal consta en `spec.md`. La confirmación de esta línea base de calidad sigue pendiente, pero no bloquea.

## Defectos de especificación

No quedan defectos conocidos. Los antiguos hallazgos se reevaluaron: códigos del contrato distinguen destino inexistente (`SLOT_NOT_FOUND`), no disponible/pasado/otro médico (`SLOT_NOT_AVAILABLE`) y mismo bloque o carrera (`APPOINTMENT_RESCHEDULE_CONFLICT`); timestamp UTC se presenta en America/Lima; la interfaz actualiza detalle/listado/disponibilidad al éxito y consulta detalle tras timeout sin reintentar.

## Dominio

- [ ] Éxito conserva ID, paciente y motivo clínico; cambia el bloque vigente y registra exactamente un evento (RF-REPRO-01, 03, 05).
- [ ] Motivo de cambio trim, obligatorio, máximo 500; no sustituye el motivo clínico (RF-REPRO-01, 05).
- [ ] Destino distinto, existente, Available, del mismo médico y con inicio futuro estricto en zona clínica; estado revalidado al confirmar (RF-REPRO-02).
- [ ] Cita Pending con origen pasado puede cambiar; cita Attended/Cancelled se rechaza sin mutación (RF-REPRO-04).
- [ ] Origen ausente/no Reserved se rechaza sin reparación automática (RF-REPRO-03).
- [ ] Cambio de cita, estados de ambos bloques e historial son atómicos; ante fallo no hay mutaciones parciales (RF-REPRO-03, 05).
- [ ] Carreras contra reserva, cancelación, atención y reprogramación preservan un único estado ganador (RF-REPRO-03, 04).
- [ ] Historial completo estable, timestamp UTC y presentación en hora clínica (RF-REPRO-05).

## UX

- [ ] Acción disponible solo para Pending; origen y paciente visibles (RF-REPRO-01, 06).
- [ ] Destinos se limitan al mismo médico; disponibilidad se revalida al confirmar (RF-REPRO-02, 06).
- [ ] Revisión previa distingue horario anterior/nuevo y motivo del cambio (RF-REPRO-06).
- [ ] Validación accesible del campo requerido y máximo de 500 caracteres (RF-REPRO-01, 06).
- [ ] Carga y bloqueo de doble envío; éxito refresca detalle/listado/disponibilidad; conflicto informa y orienta a revisar el horario (RF-REPRO-06).
- [ ] Timeout no reintenta automáticamente; si detalle consulta funciona presenta estado confirmado, y si consulta falla informa resultado no confirmado sin afirmar éxito/fracaso (RF-REPRO-06; AC-REPRO-10).
- [ ] Origen ausente o no `Reserved` reporta inconsistencia y no cambia cita, bloques ni historial ni repara datos automáticamente (RF-REPRO-03; AC-REPRO-09).
- [ ] Historial muestra horarios, motivo y hora local, con estado vacío claro (RF-REPRO-05, 06).
- [ ] Errores en español con códigos estables; uso teclado/lector, contraste mínimo 4.5:1 y responsive desde 360 px (RF-REPRO-06).
