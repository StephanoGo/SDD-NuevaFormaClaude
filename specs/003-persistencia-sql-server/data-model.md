# Modelo de datos: Persistencia SQL Server 2025 Developer

Las entidades actuales del código son la referencia para campos y reglas; este modelo fija relaciones y propiedad, sin inventar campos clínicos adicionales.

| Tabla | Datos/propiedad | Relaciones y restricciones |
| --- | --- | --- |
| `Patients` | Identidad y contacto de `Patient` | PK; DNI único. |
| `Specialties` | Nombre y activo/inactivo | PK; no se elimina desde el producto. |
| `Doctors` | Licencia, nombres, apellidos y estado | PK; FK a `Specialties`; sin unicidad adicional de licencia. |
| `ScheduleSlots` | Médico, fecha, inicio, fin y estado | PK; FK a `Doctors`; inicio menor a fin; no solapamiento por médico/fecha. |
| `Appointments` | Paciente, bloque, motivo y estado | PK; FK a `Patients` y FK única a `ScheduleSlots` (un bloque se reserva una vez). |
| `Attentions` | Diagnóstico y receta | PK; FK única a `Appointments` (máximo una atención por cita); diagnóstico obligatorio; alta coordinada con estado `Attended`. |
| `__EFMigrationsHistory` | Versiones aplicadas del esquema | Administrada por migraciones; no es dato seed. |

No hay tabla de clínica, roles ni usuarios. La desactivación conserva la fila y sus referencias históricas. No hay borrado en cascada de pacientes, médicos, especialidades, bloques, citas o atenciones con historial; las migraciones no eliminan ni reinician datos.

**Motor y pruebas:** SQL Server 2025 Developer en localhost, base `BD_MedicinaGen`, autenticación integrada de Windows. Para pruebas SQL, `BD_MedicinaGen_Tests` debe ser una base separada, dedicada, preexistente y provista por `CLINIC_TEST_CONNECTION_STRING`; nunca se usa `BD_MedicinaGen`, `master` ni fallback. No se crean, recrean ni borran bases.

**Migración inicial:** se verifica sobre `BD_MedicinaGen_Tests` preexistente y confirmada vacía (sin esquema de negocio ni historial); debe crear las tablas sin seed. Si no está disponible o no se confirma vacía, la prueba queda bloqueada sin limpiar ni reiniciar la base y sin usar la demo como alternativa. La conservación de datos queda pendiente hasta una migración posterior real; reaplicar la inicial no es actualización.

**Límites aprobados:** especialidad pasa a FK a `Specialties`; sin unicidad adicional para `Doctor.LicenseNumber` ni `Specialty.Name`; campos de alta/edición requeridos y no vacíos, sin formatos o longitudes extra; `Attentions` con FK única a cita; sin filas de negocio como seed; especialidad inactiva impide agenda nueva y reserva (RF-PER-12).
