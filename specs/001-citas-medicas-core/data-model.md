# Mapa de datos vigente

Resumen de la entrega Core; comprobar siempre las clases actuales antes de diseñar cambios.

- `Patient`: identidad, DNI y datos de contacto.
- `Doctor`: médico y estado de actividad; actualmente se carga desde datos iniciales.
- `ScheduleSlot`: médico, fecha, intervalo horario y disponibilidad.
- `Appointment`: paciente y bloque reservados, motivo, estado, diagnóstico y receta.
- Estados actuales: bloque `Available`/`Reserved`; cita `Pending`/`Attended`/`Cancelled`.

El modelo confirma que diagnóstico y receta se guardan en la cita atendida. No existe aún una entidad independiente de Historia Clínica ni una consulta longitudinal por paciente. El código de Domain es la fuente de verdad; el diagrama y detalle de la entrega original se conservan en Git.
