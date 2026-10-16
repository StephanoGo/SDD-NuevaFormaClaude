# Checklist de dominio: persistencia clínica

| Comprobación | Estado | Referencia |
|---|---|---|
| DNI de paciente conserva unicidad y validaciones. | Cubierto | RF-PER-01; módulo Pacientes. |
| Bloque no solapa otro del mismo médico/fecha; contiguos permitidos. | Cubierto | RF-PER-02; módulo Agenda. |
| Reserva relaciona paciente y bloque y sigue atómica ante concurrencia. | Cubierto | RF-PER-02; módulo Citas. |
| Solo cita pendiente admite atención; diagnóstico requerido, receta opcional, sin doble registro. | Cubierto | RF-PER-06; módulo Atenciones. |
| Atención persistida vinculada a la cita y sin duplicar el registro clínico. | Cubierto | RF-PER-11; índice único `Attentions.AppointmentId`. |
| Desactivar médico/especialidad conserva historial y bloquea con cita futura no terminada. | Cubierto | RF-PER-09; `Attended`/`Cancelled` terminan; bloques `Available` libres no bloquean. |
| Médico de especialidad inactiva no crea agenda ni reserva. | Cubierto | RF-PER-12; `DOCTOR_NOT_AVAILABLE` / `SLOT_NOT_AVAILABLE`. |
| No cargar datos de dominio al arrancar; fixtures solo en tests. | Cubierto | RF-PER-04. |
| No crear clínica, roles, usuarios ni autenticación. | Cubierto | RF-PER-07 y Fuera de alcance. |
| Pruebas SQL solo contra `BD_MedicinaGen_Tests`; nunca `BD_MedicinaGen` ni `master`. | Cubierto | T001/T013: harness valida destino y estado; pruebas de política sin conexión. |
| Migración inicial sin seed; actualización conserva datos con migración posterior real. | Parcial: T002 pendiente (base vacía); AC-PER-15 pendiente (migración posterior). | RF-PER-03, RF-PER-04. |
| Datos de integración únicos por ejecución, sin limpiar datos ajenos a los fixtures. | Cubierto | T001; contrato de motor local. |

**T001:** completada; salvaguardas de configuración explícita, destino exacto e inspección pre-migración con casos bloqueados antes de `MigrateAsync()`, sin dobles inyectables.

**T002:** pendiente hasta ejecutar la aserción del inventario completo contra `BD_MedicinaGen_Tests` precreada y vacía tras preflight seguro; con historial válido el caso inicial se excluye por filtro (no cuenta como omitido por xUnit).

No hay reglas clínicas nuevas ni preguntas de dominio abiertas.
