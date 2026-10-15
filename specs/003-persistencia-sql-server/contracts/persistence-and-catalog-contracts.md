# Contratos de persistencia y catálogos

## Contratos existentes

Las rutas y formatos HTTP vigentes para pacientes, agenda, citas y atención se conservan según [`../../001-citas-medicas-core/contracts/api-contracts.md`](../../001-citas-medicas-core/contracts/api-contracts.md), con la regla añadida de RF-PER-12: un médico cuya especialidad está inactiva no crea agenda ni reserva citas. Los repositorios SQL conservan semánticas async, `CancellationToken`, `Result`, atomicidad y compare-and-set. Reservar cita e insertar atención son transacciones completas o sin efectos.

## Catálogos HTTP

Formato general: JSON UTF-8, `camelCase`, IDs enteros positivos, errores `application/problem+json` con `code` estable. Los textos requeridos se rechazan si son null/vacíos/solo espacios; no se aplican otras reglas de formato, longitud o unicidad. `includeInactive` es booleano y su valor predeterminado es `false`.

### Especialidades

| Método y ruta | Request | Éxito | Errores |
|---|---|---|---|
| `GET /api/specialties?includeInactive={boolean}` | Sin body. | `200`; lista de `{id, name, isActive}`; por defecto activos solamente, `true` incluye inactivos. | `400 VALIDATION_FORMAT` si el filtro no es booleano. |
| `POST /api/specialties` | `{ "name": "Cardiología" }` | `201`; `{id, name, isActive}` (activo al crear). | `400 SPECIALTY_NAME_REQUIRED`. |
| `PUT /api/specialties/{specialtyId}` | `{ "name": "Cardiología" }` reemplaza el nombre. | `200`; `{id, name, isActive}`. | `400 SPECIALTY_NAME_REQUIRED`; `404 SPECIALTY_NOT_FOUND`. |
| `POST /api/specialties/{specialtyId}/deactivation` | Sin body. | `204`; conserva fila y la marca inactiva. | `404 SPECIALTY_NOT_FOUND`; `409 SPECIALTY_FUTURE_APPOINTMENTS_EXIST`. |

La desactivación se rechaza si un médico de la especialidad tiene una cita futura cuyo estado no sea `Attended` ni `Cancelled`. Un bloque `Available` sin cita no bloquea. La consulta y cambio de estado deben protegerse contra reservas concurrentes.

### Médicos

| Método y ruta | Request | Éxito | Errores |
|---|---|---|---|
| `GET /api/doctors?includeInactive={boolean}` | Sin body. | `200`; lista de `{id, licenseNumber, firstName, lastName, specialtyId, specialty, isActive}`; por defecto activos solamente, `true` incluye inactivos. | `400 VALIDATION_FORMAT` si el filtro no es booleano. |
| `POST /api/doctors` | `{ "licenseNumber": "CMP-123", "firstName": "Ana", "lastName": "Torres", "specialtyId": 1 }` | `201`; representación de médico con `isActive: true`. | `400 DOCTOR_LICENSE_REQUIRED`, `DOCTOR_FIRST_NAME_REQUIRED`, `DOCTOR_LAST_NAME_REQUIRED`, `DOCTOR_SPECIALTY_REQUIRED`; `404 SPECIALTY_NOT_FOUND`; `409 DOCTOR_SPECIALTY_NOT_ACTIVE`. |
| `PUT /api/doctors/{doctorId}` | Mismos cuatro campos; reemplaza dichos datos. | `200`; representación actualizada, conserva estado activo/inactivo. | Errores de validación anteriores; `404 DOCTOR_NOT_FOUND` o `SPECIALTY_NOT_FOUND`; `409 DOCTOR_SPECIALTY_NOT_ACTIVE`. |
| `POST /api/doctors/{doctorId}/deactivation` | Sin body. | `204`; conserva fila y la marca inactiva. | `404 DOCTOR_NOT_FOUND`; `409 DOCTOR_FUTURE_APPOINTMENTS_EXIST`. |

La desactivación se rechaza si el médico tiene cita futura cuyo estado no sea `Attended` ni `Cancelled`. Bloques `Available` sin cita no bloquean. No hay endpoints `DELETE`. Se usa la fecha/hora de la clínica (`America/Lima`) para determinar si la cita es futura.

Un médico vinculado a una especialidad inactiva no puede crear bloques ni reservar citas. La creación de agenda se rechaza con `DOCTOR_NOT_AVAILABLE`; la reserva de bloque existente se rechaza con `SLOT_NOT_AVAILABLE`, conforme a los errores vigentes de Agenda/Citas.

### Catálogo de errores

| Código | HTTP | Condición |
|---|---:|---|
| `SPECIALTY_NAME_REQUIRED` | 400 | Nombre requerido vacío. |
| `SPECIALTY_NOT_FOUND` | 404 | ID de especialidad inexistente. |
| `SPECIALTY_FUTURE_APPOINTMENTS_EXIST` | 409 | Hay cita futura no terminada vinculada a médico de la especialidad. |
| `DOCTOR_LICENSE_REQUIRED` | 400 | Número de licencia vacío. |
| `DOCTOR_FIRST_NAME_REQUIRED` | 400 | Nombre vacío. |
| `DOCTOR_LAST_NAME_REQUIRED` | 400 | Apellido vacío. |
| `DOCTOR_SPECIALTY_REQUIRED` | 400 | Falta `specialtyId`. |
| `DOCTOR_NOT_FOUND` | 404 | ID de médico inexistente. |
| `DOCTOR_SPECIALTY_NOT_ACTIVE` | 409 | Especialidad referenciada inactiva. |
| `DOCTOR_FUTURE_APPOINTMENTS_EXIST` | 409 | Hay cita futura no terminada para el médico. |
| `VALIDATION_FORMAT` | 400 | `includeInactive` no es un booleano aceptable. |

## Repositorios

- `IPatientRepository`: conservar firmas y reglas del contrato compartido; persistir con unicidad de DNI.
- `IDoctorRepository`: conservar `GetByIdAsync`; añadir alta, edición, listado con `includeInactive` y desactivación condicionada a ausencia de citas futuras no terminadas.
- `ISpecialtyRepository`: añadir alta, edición, listado con `includeInactive` y desactivación condicionada al mismo criterio sobre los médicos asociados. No añadir borrado.
- `IAppointmentRepository`: conservar reserva y consultas. `ReserveAsync` debe coordinarse transaccionalmente con la desactivación para impedir una reserva posterior a una comprobación de desactivación exitosa. `RegisterAttentionAsync` inserta una fila `Attention` y cambia Pending a Attended en una sola transacción, con unicidad por cita.

Las operaciones no sustituyen reglas de dominio. Las escrituras condicionadas al estado persistido devuelven códigos estables y no dejan cambios parciales.
