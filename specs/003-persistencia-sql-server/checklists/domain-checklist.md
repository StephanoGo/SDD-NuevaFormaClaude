# Checklist de dominio: persistencia clínica

| Comprobación | Estado | Referencia |
|---|---|---|
| DNI de paciente conserva unicidad y validaciones actuales. | Cubierto | RF-PER-01; módulo Pacientes. |
| Bloque agenda pertenece a médico y no solapa otro bloque del mismo médico/fecha; contiguos siguen permitidos. | Cubierto en módulos; confirmar en persistencia | RF-PER-02 y módulo Agenda. |
| Reserva relaciona paciente y bloque y sigue atómica ante concurrencia. | Cubierto | RF-PER-02; módulo Citas. |
| Solo cita pendiente admite atención; diagnóstico requerido, receta opcional y no se concluye dos veces. | Cubierto | RF-PER-06; módulo Atenciones. |
| Atención persistida tiene vínculo con la cita y no duplica el registro clínico. | Cubierto | RF-PER-11 y restricción única de `Attentions.AppointmentId`. |
| Desactivar médico/especialidad conserva historial y bloquea mientras haya cita futura no terminada. | Cubierto | RF-PER-09; `Attended` y `Cancelled` terminan; bloques `Available` libres no bloquean. |
| Médico de especialidad inactiva no crea agenda ni reserva citas. | Cubierto | RF-PER-12; errores reutilizan `DOCTOR_NOT_AVAILABLE` y `SLOT_NOT_AVAILABLE` del contrato existente. |
| No cargar datos de dominio al arrancar; conservar fixtures únicamente en tests. | Cubierto | RF-PER-04. |
| No crear clínica, roles, usuarios o autenticación de usuario. | Cubierto | RF-PER-07 y Fuera de alcance. |

No se proponen reglas clínicas nuevas ni quedan preguntas de dominio abiertas en este checklist.
