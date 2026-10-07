# Contratos: Núcleo de Citas Médicas

| Campo | Valor |
| :--- | :--- |
| **Feature** | `001-citas-medicas-core` |
| **Plan** | [plan.md](../plan.md) |
| **Especificación** | [spec.md](../spec.md) |
| **Modelo de datos** | [data-model.md](../data-model.md) |

Este documento define:
1. **Contrato HTTP** de la Web API (§2): lo que consume un cliente.
2. **Contratos de repositorio** (§3): `IPatientRepository`, `IDoctorRepository` e `IAppointmentRepository`, la frontera entre Aplicación e Infraestructura.
3. **Catálogo de errores** (§4): códigos estables exigidos por RF-029.

> Las firmas se describen en tablas, sin código C#. La implementación las traduce literalmente.

---

## 1. Convenciones Generales

| Aspecto | Convención |
| :--- | :--- |
| Ruta base | `/api` |
| Formato | JSON UTF-8; propiedades en `camelCase` e inglés |
| Fechas | `yyyy-MM-dd` (por ejemplo `"2026-10-15"`) |
| Horas | `HH:mm` en 24 h (por ejemplo `"09:30"`) |
| Enumeraciones | Cadena con el identificador en inglés (`"Available"`, `"Pending"`, …) |
| Identificadores | Entero positivo |
| Normalización | La Aplicación aplica `Trim` a todos los textos antes de validar |
| Error | `application/problem+json` (ProblemDetails) con la extensión `code` |

**Formato de error**

```json
{
  "type": "https://httpstatuses.io/409",
  "title": "Conflicto de negocio",
  "status": 409,
  "detail": "Horario no disponible",
  "code": "SLOT_NOT_AVAILABLE"
}
```

- `detail`: mensaje en español definido en la spec.
- `code`: código estable del catálogo (§4). Las pruebas verifican `code`, nunca `detail`.

---

## 2. Contrato HTTP

### Resumen de endpoints

| # | Método | Ruta | Propósito | Requisitos |
| :-: | :--- | :--- | :--- | :--- |
| E1 | `POST` | `/api/patients` | Registrar paciente | RF-001 – RF-005 |
| E2 | `GET` | `/api/patients?dni={dni}` | Buscar paciente por DNI | RF-006 |
| E3 | `GET` | `/api/patients?lastName={texto}` | Buscar pacientes por apellidos | RF-007, RF-008 |
| E4 | `POST` | `/api/doctors/{doctorId}/schedule-slots` | Crear bloque de agenda | RF-009 – RF-013 |
| E5 | `GET` | `/api/doctors/{doctorId}/schedule-slots?date={fecha}` | Consultar agenda del día | RF-014 |
| E6 | `POST` | `/api/appointments` | Reservar cita | RF-015 – RF-021 |
| E7 | `POST` | `/api/appointments/{appointmentId}/attention` | Registrar atención | RF-022 – RF-025 |
| E8 | `GET` | `/api/appointments/{appointmentId}` | Consultar cita | RF-026 |

> No existen endpoints de edición ni eliminación (spec §9). Esto refuerza RF-025: una cita *Atendida* no tiene ninguna vía de modificación.

---

### E1 — Registrar paciente

`POST /api/patients`

**Request**

| Campo | Tipo | Oblig. | Reglas | Req. |
| :--- | :--- | :-: | :--- | :--- |
| `dni` | string | Sí | 8 dígitos numéricos; único | RF-002, RF-003 |
| `firstName` | string | Sí | No vacío | RF-005 |
| `lastName` | string | Sí | No vacío | RF-005 |
| `phone` | string | Sí | No vacío | RF-005 |
| `email` | string \| null | No | Formato válido si se informa | RF-004 |

```json
{
  "dni": "45678912",
  "firstName": "Rosa",
  "lastName": "García López",
  "phone": "+51 987 654 321",
  "email": "rosa.garcia@correo.pe"
}
```

**Respuestas**

| Código | Cuándo | Cuerpo |
| :-: | :--- | :--- |
| `201 Created` | Registro exitoso (RF-001) | `PatientResponse` |
| `400` | `PATIENT_DNI_INVALID`, `PATIENT_EMAIL_INVALID`, `PATIENT_REQUIRED_FIELD` | ProblemDetails |
| `409` | `PATIENT_DNI_DUPLICATE` | ProblemDetails |

**`PatientResponse`**

```json
{
  "id": 1,
  "dni": "45678912",
  "firstName": "Rosa",
  "lastName": "García López",
  "phone": "+51 987 654 321",
  "email": "rosa.garcia@correo.pe"
}
```

---

### E2 / E3 — Buscar pacientes

`GET /api/patients?dni={dni}` · `GET /api/patients?lastName={texto}`

**Parámetros de consulta**: se debe enviar **exactamente uno** de los dos.

| Parámetro | Reglas | Req. |
| :--- | :--- | :--- |
| `dni` | Coincidencia exacta | RF-006 |
| `lastName` | Mínimo 2 caracteres; contiene; sin distinguir mayúsculas ni tildes; orden por `lastName, firstName` | RF-007, RF-008 |

**Respuestas**

| Código | Cuándo | Cuerpo |
| :-: | :--- | :--- |
| `200 OK` | Siempre que la consulta sea válida, **incluso sin resultados** (RF-006, RF-007) | `PatientResponse[]` (0 o 1 elemento para `dni`) |
| `400` | `SEARCH_CRITERIA_TOO_SHORT`; `VALIDATION_FORMAT` si se envían ambos parámetros o ninguno | ProblemDetails |

---

### E4 — Crear bloque de agenda

`POST /api/doctors/{doctorId}/schedule-slots`

**Request**

| Campo | Tipo | Oblig. | Reglas | Req. |
| :--- | :--- | :-: | :--- | :--- |
| `date` | string (`yyyy-MM-dd`) | Sí | No en el pasado | RF-012 |
| `startTime` | string (`HH:mm`) | Sí | `startTime < endTime`; no transcurrida si `date` es hoy | RF-011, RF-012 |
| `endTime` | string (`HH:mm`) | Sí | Exclusiva para el solapamiento | RF-011, RF-013 |

```json
{ "date": "2026-10-15", "startTime": "09:00", "endTime": "09:30" }
```

**Respuestas**

| Código | Cuándo | Cuerpo |
| :-: | :--- | :--- |
| `201 Created` | Bloque creado en `Available` (RF-009) | `ScheduleSlotResponse` |
| `400` | `SLOT_INVALID_TIME_RANGE`, `SLOT_IN_PAST`, `VALIDATION_FORMAT` | ProblemDetails |
| `409` | `DOCTOR_NOT_AVAILABLE` (médico inexistente o inactivo), `SLOT_OVERLAP` | ProblemDetails |

**`ScheduleSlotResponse`**

```json
{
  "id": 10,
  "doctorId": 1,
  "date": "2026-10-15",
  "startTime": "09:00",
  "endTime": "09:30",
  "status": "Available"
}
```

---

### E5 — Consultar agenda del día

`GET /api/doctors/{doctorId}/schedule-slots?date=2026-10-15`

| Código | Cuándo | Cuerpo |
| :-: | :--- | :--- |
| `200 OK` | Consulta válida; lista ordenada por `startTime`. Vacía si no hay bloques o si el médico no existe (RF-014) | `ScheduleSlotResponse[]` |
| `400` | `VALIDATION_FORMAT` (falta `date` o tiene formato inválido) | ProblemDetails |

---

### E6 — Reservar cita

`POST /api/appointments`

**Request**

| Campo | Tipo | Oblig. | Reglas | Req. |
| :--- | :--- | :-: | :--- | :--- |
| `scheduleSlotId` | int | Sí | Existe; `Available`; médico activo; bloque no iniciado | RF-016, RF-017, RF-019 |
| `patientId` | int | Sí | Existe | RF-017 |
| `reason` | string | Sí | No vacío | RF-018 |

```json
{ "scheduleSlotId": 10, "patientId": 1, "reason": "Dolor de cabeza recurrente" }
```

**Respuestas**

| Código | Cuándo | Cuerpo |
| :-: | :--- | :--- |
| `201 Created` | Cita `Pending` creada y bloque `Reserved`, de forma atómica (RF-015, RF-020) | `AppointmentResponse` |
| `400` | `APPOINTMENT_REASON_REQUIRED` | ProblemDetails |
| `404` | `PATIENT_NOT_FOUND`, `SLOT_NOT_FOUND` | ProblemDetails |
| `409` | `SLOT_NOT_AVAILABLE` (reservado, médico inactivo, bloque iniciado o perdido por concurrencia) | ProblemDetails |

---

### E7 — Registrar atención

`POST /api/appointments/{appointmentId}/attention`

**Request**

| Campo | Tipo | Oblig. | Reglas | Req. |
| :--- | :--- | :-: | :--- | :--- |
| `diagnosis` | string | Sí | No vacío | RF-023 |
| `prescription` | string \| null | No | Texto libre | RF-022 |

```json
{ "diagnosis": "Migraña tensional", "prescription": "Paracetamol 500 mg c/8h por 3 días" }
```

**Respuestas**

| Código | Cuándo | Cuerpo |
| :-: | :--- | :--- |
| `200 OK` | Cita pasa a `Attended` (RF-022) | `AppointmentResponse` |
| `400` | `ATTENTION_DIAGNOSIS_REQUIRED` | ProblemDetails |
| `404` | `APPOINTMENT_NOT_FOUND` | ProblemDetails |
| `409` | `APPOINTMENT_NOT_PENDING` (cita `Attended` o `Cancelled`) | ProblemDetails |

---

### E8 — Consultar cita

`GET /api/appointments/{appointmentId}`

| Código | Cuándo | Cuerpo |
| :-: | :--- | :--- |
| `200 OK` | La cita existe (RF-026) | `AppointmentResponse` |
| `404` | `APPOINTMENT_NOT_FOUND` | ProblemDetails |

**`AppointmentResponse`** (compartido por E6, E7 y E8)

```json
{
  "id": 100,
  "status": "Attended",
  "reason": "Dolor de cabeza recurrente",
  "diagnosis": "Migraña tensional",
  "prescription": "Paracetamol 500 mg c/8h por 3 días",
  "patient": { "id": 1, "dni": "45678912", "fullName": "Rosa García López" },
  "doctor": { "id": 1, "fullName": "Ana Torres Vega", "specialty": "Medicina General" },
  "slot": { "id": 10, "date": "2026-10-15", "startTime": "09:00", "endTime": "09:30" }
}
```

`diagnosis` y `prescription` son `null` mientras la cita esté en `Pending`.

---

## 3. Contratos de Repositorio

Ubicación en código: `CitasMedicas.Application/Abstractions/Persistence` ([research.md R-05](../research.md)). Implementación de esta fase: en memoria ([R-02](../research.md)).

**Reglas comunes**
- Todas las operaciones son asíncronas y aceptan un token de cancelación.
- Las operaciones de escritura son **atómicas**: se confirman por completo o no se confirman ([R-03](../research.md)).
- Las escrituras que pueden fallar por el estado persistido devuelven `Result` con un código del catálogo (§4); no lanzan excepciones de negocio.
- Los repositorios **no** deciden reglas de negocio. Solo garantizan que el estado sobre el que decidió el Dominio no cambió antes de confirmar.

### 3.1 `IPatientRepository`

| Operación | Entrada | Salida | Semántica | Req. |
| :--- | :--- | :--- | :--- | :--- |
| `AddAsync` | `Patient` (sin Id) | `Result<Patient>` | Asigna el `Id` e inserta. Si el DNI ya existe al confirmar, devuelve `PATIENT_DNI_DUPLICATE` (INV-01). | RF-001, RF-002 |
| `GetByIdAsync` | `id` | `Patient?` | Lectura por clave. | RF-017, RF-026 |
| `GetByDniAsync` | `dni` | `Patient?` | Coincidencia exacta. | RF-002, RF-006 |
| `SearchByLastNameAsync` | `text` | Lista de `Patient` | Contiene, sobre valores normalizados con `TextNormalizer.ForSearch` ([R-08](../research.md)); ordenada por `LastName, FirstName`. | RF-007 |

### 3.2 `IDoctorRepository`

| Operación | Entrada | Salida | Semántica | Req. |
| :--- | :--- | :--- | :--- | :--- |
| `GetByIdAsync` | `id` | `Doctor?` | Lectura por clave. Los datos provienen del seed ([data-model.md §7](../data-model.md)). | RF-010, RF-019, RF-026 |

> No existen operaciones de escritura sobre médicos (spec, supuesto 4).

### 3.3 `IAppointmentRepository`

Gestiona `ScheduleSlot` y `Appointment` porque la reserva las modifica atómicamente ([R-04](../research.md)).

| Operación | Entrada | Salida | Semántica | Req. |
| :--- | :--- | :--- | :--- | :--- |
| `AddSlotAsync` | `ScheduleSlot` (sin Id) | `Result<ScheduleSlot>` | Dentro de la sección crítica vuelve a verificar el solapamiento con `ScheduleSlot.OverlapsWith` (método de dominio) contra los bloques del mismo médico y fecha. Si hay solapamiento, devuelve `SLOT_OVERLAP`; si no, asigna el `Id` e inserta (INV-02). | RF-009, RF-013 |
| `GetSlotByIdAsync` | `id` | `ScheduleSlot?` | Lectura por clave. | RF-016, RF-017 |
| `GetSlotsByDoctorAndDateAsync` | `doctorId`, `date` | Lista de `ScheduleSlot` | Ordenada por `StartTime`. | RF-013, RF-014 |
| `ReserveAsync` | `ScheduleSlot` (ya marcado `Reserved` por el Dominio), `Appointment` (sin Id, `Pending`) | `Result<Appointment>` | *Compare-and-set*: si el bloque **persistido** sigue `Available`, lo cambia a `Reserved`, asigna el `Id` e inserta la cita en la misma unidad atómica. Si no, devuelve `SLOT_NOT_AVAILABLE` sin efectos (INV-03, INV-04). | RF-015, RF-020, RF-021 |
| `GetAppointmentByIdAsync` | `id` | `Appointment?` | Lectura por clave. | RF-022, RF-026 |
| `RegisterAttentionAsync` | `Appointment` (ya `Attended` en el Dominio) | `Result<Appointment>` | *Compare-and-set*: confirma `Status`, `Diagnosis` y `Prescription` solo si la cita **persistida** sigue `Pending`. Si no, devuelve `APPOINTMENT_NOT_PENDING` sin efectos (INV-05, INV-06). | RF-022, RF-024, RF-025 |

---

## 4. Catálogo de Errores (RF-029)

| Código | HTTP | Mensaje (`detail`) | Origen | Req. |
| :--- | :-: | :--- | :--- | :--- |
| `PATIENT_DNI_INVALID` | 400 | DNI inválido | Dominio | RF-003 |
| `PATIENT_EMAIL_INVALID` | 400 | Email inválido | Dominio | RF-004 |
| `PATIENT_REQUIRED_FIELD` | 400 | El campo {campo} es obligatorio | Dominio | RF-005 |
| `PATIENT_DNI_DUPLICATE` | 409 | DNI ya registrado | Aplicación / Repositorio | RF-002 |
| `SEARCH_CRITERIA_TOO_SHORT` | 400 | Criterio de búsqueda demasiado corto | Aplicación | RF-008 |
| `DOCTOR_NOT_AVAILABLE` | 409 | Médico no disponible para agenda | Dominio | RF-010 |
| `SLOT_INVALID_TIME_RANGE` | 400 | Rango horario inválido | Dominio | RF-011 |
| `SLOT_IN_PAST` | 400 | No se permiten bloques en el pasado | Dominio | RF-012 |
| `SLOT_OVERLAP` | 409 | Bloque solapado | Dominio / Repositorio | RF-013 |
| `SLOT_NOT_FOUND` | 404 | Bloque de agenda no encontrado | Aplicación | RF-017 |
| `PATIENT_NOT_FOUND` | 404 | Paciente no encontrado | Aplicación | RF-017 |
| `SLOT_NOT_AVAILABLE` | 409 | Horario no disponible | Dominio / Repositorio | RF-016, RF-019, RF-021 |
| `APPOINTMENT_REASON_REQUIRED` | 400 | Motivo obligatorio | Dominio | RF-018 |
| `ATTENTION_DIAGNOSIS_REQUIRED` | 400 | Diagnóstico obligatorio | Dominio | RF-023 |
| `APPOINTMENT_NOT_PENDING` | 409 | La cita no admite atención | Dominio / Repositorio | RF-024, RF-025 |
| `APPOINTMENT_NOT_FOUND` | 404 | Cita no encontrada | Aplicación | RF-026 |
| `VALIDATION_FORMAT` | 400 | Formato de solicitud inválido: {detalle} | Api (solo formato) | R-09 |

> `VALIDATION_FORMAT` es el único error que se genera en la capa Api. Cubre únicamente errores de **formato** (JSON mal formado, fecha u hora no parseables, parámetros de consulta excluyentes) y nunca reglas de negocio (Constitución §3).
