# Modelo de datos: Consulta del historial clínico

Sin cambios de esquema. Proyección de lectura por cada `Attentions` unido a su `Appointment`, `ScheduleSlot`, `Doctor`, `Specialty` y `Patient`. Filtrar `Appointment.Status = Attended`; ordenar `ScheduleSlot.Date DESC, StartTime DESC, Appointment.Id DESC`. No se persiste copia de la historia ni fecha real de atención.
