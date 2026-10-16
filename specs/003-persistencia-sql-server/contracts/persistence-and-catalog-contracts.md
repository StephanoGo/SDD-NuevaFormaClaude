# Contratos de persistencia y catálogos

## Motor local

SQL Server 2025 Developer en localhost, base `BD_MedicinaGen`, autenticación integrada de Windows. No modifica los contratos HTTP existentes ni requiere reescribir la persistencia. No crear, recrear ni borrar bases.

**Pruebas SQL:** base separada, dedicada y preexistente `BD_MedicinaGen_Tests`, indicada por `CLINIC_TEST_CONNECTION_STRING` (p. ej. `Server=localhost;Database=BD_MedicinaGen_Tests;Integrated Security=True;TrustServerCertificate=True`). El harness valida `localhost`, catálogo exacto y estado antes de `MigrateAsync`; sin valor por defecto, sin `master` y nunca `BD_MedicinaGen`. Sin conexión válida o estado no confirmado: no ejecutar `SqlServerPersistenceTests`, seleccionar solo pruebas no SQL y reportar SQL bloqueadas (no omitidas). Con historial EF válido en `dbo` reconocido por la aplicación: ejecutar por filtro las pruebas SQL aplicables, excluyendo el caso inicial sin contarlo como omitido ni como T002. Datos únicos por ejecución y conservados; no limpiar, truncar ni eliminar filas/bases.

**Migración inicial:** se ejecuta por separado solo con base provista previamente y preflight que confirme vacío (sin tablas de negocio ni historial). Con historial reconocido no aplica y se excluye por filtro; ante inspección fallida, historial fuera de `dbo`, vacío/desconocido o tablas sin historial, se bloquea SQL antes de migrar. No usar `SkipException.ForSkip` para contabilizar una exclusión como omisión. La actualización con datos queda pendiente hasta una migración posterior real; reaplicar la inicial no la simula.

## Contratos existentes

Rutas y formatos HTTP de pacientes, agenda, citas y atención según [`../../001-citas-medicas-core/contracts/api-contracts.md`](../../001-citas-medicas-core/contracts/api-contracts.md), con RF-PER-12 (médico de especialidad inactiva no crea agenda ni reserva). Los repositorios SQL conservan semánticas async, `CancellationToken`, `Result`, atomicidad y compare-and-set; reservar cita e insertar atención son transacciones completas o sin efectos.

## Catálogos HTTP

Formato general: JSON UTF-8, `camelCase`, IDs enteros positivos, errores `application/problem+json` con `code` estable. Textos requeridos rechazados si son null/vacíos/solo espacios; sin otras reglas de formato, longitud o unicidad. `includeInactive` booleano, por defecto `false`.

### Especialidades

| Método y ruta | Request | Éxito | Errores |
|---|---|---|---|
| `GET /api/specialties?includeInactive={boolean}` | Sin body. | `200`; lista `{id, name, isActive}`; por defecto activos, `true` incluye inactivos. | `400 VALIDATION_FORMAT` si el filtro no es booleano. |
| `POST /api/specialties` | `{ "name": "Cardiología" }` | `201`; `{id, name, isActive}` (activo al crear). | `400 SPECIALTY_NAME_REQUIRED`. |
| `PUT /api/specialties/{specialtyId}` | `{ "name": "Cardiología" }` | `200`; `{id, name, isActive}`. | `400 SPECIALTY_NAME_REQUIRED`; `404 SPECIALTY_NOT_FOUND`. |
| `POST /api/specialties/{specialtyId}/deactivation` | Sin body. | `204`; conserva la fila y la marca inactiva. | `404 SPECIALTY_NOT_FOUND`; `409 SPECIALTY_FUTURE_APPOINTMENTS_EXIST`. |

La desactivación se rechaza si un médico de la especialidad tiene cita futura no `Attended` ni `Cancelled`. Un bloque `Available` sin cita no bloquea. Consulta y cambio de estado protegidos contra reservas concurrentes.

### Médicos

| Método y ruta | Request | Éxito | Errores |
|---|---|---|---|
| `GET /api/doctors?includeInactive={boolean}` | Sin body. | `200`; lista `{id, licenseNumber, firstName, lastName, specialtyId, specialty, isActive}`; por defecto activos, `true` incluye inactivos. | `400 VALIDATION_FORMAT` si el filtro no es booleano. |
| `POST /api/doctors` | `{ "licenseNumber": "CMP-123", "firstName": "Ana", "lastName": "Torres", "specialtyId": 1 }` | `201`; médico con `isActive: true`. | `400 DOCTOR_LICENSE_REQUIRED`, `DOCTOR_FIRST_NAME_REQUIRED`, `DOCTOR_LAST_NAME_REQUIRED`, `DOCTOR_SPECIALTY_REQUIRED`; `404 SPECIALTY_NOT_FOUND`; `409 DOCTOR_SPECIALTY_NOT_ACTIVE`. |
| `PUT /api/doctors/{doctorId}` | Mismos cuatro campos. | `200`; representación actualizada, conserva estado. | Errores anteriores; `404 DOCTOR_NOT_FOUND`/`SPECIALTY_NOT_FOUND`; `409 DOCTOR_SPECIALTY_NOT_ACTIVE`. |
| `POST /api/doctors/{doctorId}/deactivation` | Sin body. | `204`; conserva la fila y la marca inactiva. | `404 DOCTOR_NOT_FOUND`; `409 DOCTOR_FUTURE_APPOINTMENTS_EXIST`. |

La desactivación se rechaza si el médico tiene cita futura no `Attended` ni `Cancelled`; bloques `Available` sin cita no bloquean; sin endpoints `DELETE`; se usa la hora de la clínica (`America/Lima`). Un médico de especialidad inactiva no crea bloques ni reserva (`DOCTOR_NOT_AVAILABLE` / `SLOT_NOT_AVAILABLE`).

### Catálogo de errores

| Código | HTTP | Condición |
|---|---:|---|
| `SPECIALTY_NAME_REQUIRED` | 400 | Nombre de especialidad vacío. |
| `SPECIALTY_NOT_FOUND` | 404 | ID de especialidad inexistente. |
| `SPECIALTY_FUTURE_APPOINTMENTS_EXIST` | 409 | Cita futura no terminada vinculada a un médico de la especialidad. |
| `DOCTOR_LICENSE_REQUIRED` | 400 | Número de licencia vacío. |
| `DOCTOR_FIRST_NAME_REQUIRED` | 400 | Nombre vacío. |
| `DOCTOR_LAST_NAME_REQUIRED` | 400 | Apellido vacío. |
| `DOCTOR_SPECIALTY_REQUIRED` | 400 | Falta `specialtyId`. |
| `DOCTOR_NOT_FOUND` | 404 | ID de médico inexistente. |
| `DOCTOR_SPECIALTY_NOT_ACTIVE` | 409 | Especialidad referenciada inactiva. |
| `DOCTOR_FUTURE_APPOINTMENTS_EXIST` | 409 | Cita futura no terminada para el médico. |
| `VALIDATION_FORMAT` | 400 | `includeInactive` no es booleano aceptable. |

## Repositorios

- `IPatientRepository`: firmas y reglas del contrato compartido; unicidad de DNI.
- `IDoctorRepository`: `GetByIdAsync`; alta, edición, listado con `includeInactive` y desactivación condicionada a ausencia de citas futuras no terminadas.
- `ISpecialtyRepository`: alta, edición, listado con `includeInactive` y desactivación con el mismo criterio sobre los médicos asociados; sin borrado.
- `IAppointmentRepository`: reserva y consultas. `ReserveAsync` coordinado transaccionalmente con la desactivación; `RegisterAttentionAsync` inserta la atención y cambia `Pending`→`Attended` en una transacción, con unicidad por cita.

Las operaciones no sustituyen reglas de dominio; las escrituras condicionadas al estado devuelven códigos estables y no dejan cambios parciales.
