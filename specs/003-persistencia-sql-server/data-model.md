# Modelo de datos: Persistencia SQL Server

Las entidades actuales del código son la referencia para campos y reglas. Este modelo fija relaciones y propiedad, no inventa campos clínicos adicionales.

| Tabla | Datos/propiedad | Relaciones y restricciones |
| --- | --- | --- |
| `Patients` | Identidad y contacto de `Patient` | PK; DNI único. |
| `Specialties` | Nombre y activo/inactivo del catálogo | PK; no se elimina desde el producto. |
| `Doctors` | Licencia, nombres, apellidos y estado de `Doctor` | PK; FK requerida a `Specialties`; sin unicidad adicional de licencia. |
| `ScheduleSlots` | Médico, fecha, inicio, fin y estado | PK; FK a `Doctors`; fecha/inicio menor a fin; consultas e invariantes de solapamiento por médico/fecha. |
| `Appointments` | Paciente, bloque, motivo y estado | PK; FK a `Patients` y FK única a `ScheduleSlots` (un bloque puede reservarse una sola vez). |
| `Attentions` | Diagnóstico y receta | PK; FK única a `Appointments` para máximo una atención por cita; diagnóstico obligatorio. Alta coordinada con estado Attended. |
| `__EFMigrationsHistory` | Versiones aplicadas del esquema | Administrada por mecanismo de migraciones; no es dato seed del dominio. |

No hay tabla de clínica, roles o usuarios. La desactivación conserva la fila y sus referencias históricas. La restricción referencial y las reglas de borrado deben evitar borrar en cascada pacientes, médicos, especialidades, bloques, citas o atenciones con historial. Las migraciones no deben eliminar ni reiniciar los datos almacenados.

**Límites aprobados:** especialidad pasa de texto embebido a FK a `Specialties`; no hay unicidad adicional para `Doctor.LicenseNumber` ni `Specialty.Name`. Los campos de creación/edición son requeridos y no vacíos, sin formatos o longitudes adicionales. La tabla `Attentions` aplica FK única a cita. No se insertan filas de negocio como seed. Si la especialidad está inactiva, la regla de RF-PER-12 impide agenda nueva y reserva para sus médicos.
