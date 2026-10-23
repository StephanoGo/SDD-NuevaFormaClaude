# Checklist UX — Reprogramación de citas

Los ítems `[ ]` son criterios para validar durante implementación, no defectos confirmados. Comportamiento aprobado en `spec.md`. Vinculado a RF-REPRO-01, 02, 05 y 06.

- [ ] Búsqueda/detalle ofrece reprogramar solo para citas `Pending` y muestra contexto actual (RF-REPRO-01, 04, 06).
- [ ] Selector limita las opciones al mismo médico y destinos que comienzan en el futuro; disponibilidad se revalida al confirmar (RF-REPRO-02, 06).
- [ ] Antes de confirmar se muestran horario actual, destino y motivo de cambio (RF-REPRO-06).
- [ ] Campo de motivo indica obligatoriedad y máximo 500 caracteres; validación accesible y texto conservado ante error (RF-REPRO-01, 06).
- [ ] Envío muestra carga y evita duplicados; éxito actualiza detalle, listado y disponibilidad (RF-REPRO-06).
- [ ] Conflicto no muestra éxito falso, conserva contexto y orienta a revisar horario vigente (RF-REPRO-03, 06).
- [ ] Tras timeout/red no se reintenta automáticamente; se vuelve a consultar el detalle y se presenta el estado confirmado (RF-REPRO-06; AC-REPRO-10).
- [ ] Si también falla la consulta, se informa que no se pudo confirmar el resultado, sin afirmar éxito ni fracaso (RF-REPRO-06; AC-REPRO-10).
- [ ] Origen inconsistente se reporta como conflicto comprensible, sin sugerir reparación automática ni mostrar éxito (RF-REPRO-03; AC-REPRO-09).
- [ ] Historial vacío/poblado presenta cada horario anterior/nuevo, motivo y fecha/hora convertida de UTC a `America/Lima` (RF-REPRO-05, 06).
- [ ] Errores distinguen cita no pendiente, bloque inexistente/no disponible, mismo destino, motivo inválido y concurrencia; mensajes en español y código estable (RF-REPRO-02–04, 06).
- [ ] Flujo operable con teclado/lector, contraste al menos 4.5:1 y adaptable desde 360 px (RF-REPRO-06).

## Aclaraciones

Timeout, consulta posterior y zona horaria de presentación están resueltos en la spec aprobada. La verificación visual/interactiva queda pendiente para implementación; no se afirma que se haya realizado.
