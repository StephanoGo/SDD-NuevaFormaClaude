# Modelo de datos: Consulta y cancelación

Sin cambios de esquema. `Appointments.Status` mantiene Pending/Attended/Cancelled y `ScheduleSlots.Status` Available/Reserved. Cancelación compare-and-set de appointment Pending a Cancelled y liberación del slot Reserved a Available dentro de una única unidad atómica. Listado es una proyección de cita/paciente/slot/médico/especialidad y opcionalmente atención; ordenar por Date, StartTime, AppointmentId ascendente.
