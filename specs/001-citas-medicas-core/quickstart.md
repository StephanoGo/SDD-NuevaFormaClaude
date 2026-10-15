---
layout: tutorial_hands_on
title: "Quickstart: Núcleo de Citas Médicas"
feature: 001-citas-medicas-core
questions:
  - "¿Cómo preparo el entorno para compilar y ejecutar la API?"
  - "¿Cómo verifico rápidamente que el flujo paciente → agenda → cita funciona?"
objectives:
  - "Restaurar, compilar y ejecutar la Web API en .NET 10."
  - "Registrar un paciente, consultar la agenda de un médico y reservar una cita."
  - "Ejecutar la suite de pruebas con dotnet test."
time_estimation: "15M"
key_points:
  - "Una sola solución: dotnet restore, dotnet build y dotnet test se ejecutan desde la raíz."
  - "Los datos viven en memoria: cada reinicio parte desde cero con 3 médicos precargados."
  - "Los errores devuelven ProblemDetails con un campo code estable."
references:
  - "[spec.md](./spec.md) · [plan.md](./plan.md) · [tasks.md](./tasks.md) · [api-contracts.md](./contracts/api-contracts.md)"
---

> <agenda-title></agenda-title>
>
> 1. [Prerrequisitos](#1-prerrequisitos)
> 2. [Restaurar, compilar y ejecutar](#2-restaurar-compilar-y-ejecutar)
> 3. [Verificación rápida](#3-verificación-rápida)
> 4. [Pruebas](#4-pruebas)
{: .agenda}

> <warning-title>Estado de la feature</warning-title>
> Esta guía describe el comportamiento definido en [api-contracts.md](./contracts/api-contracts.md). Los comandos funcionarán una vez completadas las tareas T1 a T5 de [tasks.md](./tasks.md).
{: .warning}

---

## 1. Prerrequisitos

| Herramienta | Versión | Verificación |
| :--- | :--- | :--- |
| .NET SDK | **10.0.100 o superior** (verificado con `10.0.401`) | `dotnet --list-sdks` |
| Git | Cualquiera reciente | `git --version` |
| cURL | 7.82 o superior (incluido en Windows 10/11) | `curl --version` |

> <hands-on-title>Verificar el entorno</hands-on-title>
>
> ```bash
> dotnet --list-sdks          # Debe listar una versión 10.0.x
> git checkout 001-citas-medicas-core
> ```
{: .hands_on}

> <comment-title>Sin base de datos</comment-title>
> En esta fase la persistencia es en memoria ([research.md R-02](./research.md)). No se necesita SQL Server ni cadenas de conexión.
{: .comment}

---

## 2. Restaurar, compilar y ejecutar

> <hands-on-title>Levantar la Web API</hands-on-title>
>
> Desde la raíz del repositorio:
>
> ```bash
> dotnet restore
> dotnet build
> dotnet run --project src/CitasMedicas.Api --urls http://localhost:5080
> ```
>
> El resultado esperado de `dotnet build` es `0 Advertencias, 0 Errores`. La API queda escuchando en `http://localhost:5080`.
{: .hands_on}

> <tip-title>Puerto fijo</tip-title>
> `--urls` evita depender del puerto aleatorio de `launchSettings.json`. Los ejemplos siguientes asumen `http://localhost:5080`.
{: .tip}

**Datos precargados** ([data-model.md §7](./data-model.md)):

| `doctorId` | Médico | Especialidad | Activo |
| :-: | :--- | :--- | :-: |
| 1 | Ana Torres Vega | Medicina General | ✅ |
| 2 | Luis Paredes Soto | Pediatría | ✅ |
| 3 | María Quispe Rojas | Cardiología | ❌ |

---

## 3. Verificación rápida

Ejecuta los pasos en orden, en una segunda terminal y sobre una API recién iniciada (los `id` esperados asumen datos vacíos).

### 3.1 Crear un paciente (E1 · RF-001)

> <hands-on-title>Registrar paciente</hands-on-title>
>
> ```bash
> curl -i -X POST http://localhost:5080/api/patients \
>   -H "Content-Type: application/json" \
>   -d '{"dni":"45678912","firstName":"Rosa","lastName":"García López","phone":"+51 987 654 321","email":"rosa.garcia@correo.pe"}'
> ```
>
> **Esperado**: `201 Created`
> ```json
> { "id": 1, "dni": "45678912", "firstName": "Rosa", "lastName": "García López", "phone": "+51 987 654 321", "email": "rosa.garcia@correo.pe" }
> ```
{: .hands_on}

> <question-title></question-title>
> ¿Qué ocurre si repites exactamente la misma llamada?
>
> > <solution-title></solution-title>
> > `409 Conflict` con `"code": "PATIENT_DNI_DUPLICATE"` (RF-002).
> {: .solution}
{: .question}

### 3.2 Consultar la agenda de un médico (E5 · RF-014)

La agenda empieza vacía. Primero se crea un bloque (E4, RF-009) y luego se consulta.

> <hands-on-title>Crear un bloque y consultar la agenda</hands-on-title>
>
> ```bash
> # Preparación: crear el bloque 09:00–09:30 para la Dra. Torres
> curl -i -X POST http://localhost:5080/api/doctors/1/schedule-slots \
>   -H "Content-Type: application/json" \
>   -d '{"date":"2026-10-15","startTime":"09:00","endTime":"09:30"}'
>
> # Consulta de la agenda del día
> curl -i "http://localhost:5080/api/doctors/1/schedule-slots?date=2026-10-15"
> ```
>
> **Esperado**: `201 Created` para el bloque y `200 OK` para la consulta:
> ```json
> [ { "id": 1, "doctorId": 1, "date": "2026-10-15", "startTime": "09:00", "endTime": "09:30", "status": "Available" } ]
> ```
{: .hands_on}

> <tip-title>Fechas</tip-title>
> La fecha del bloque debe ser futura (RF-012). Si ejecutas la guía después del 2026-10-15, usa una fecha posterior en ambos comandos.
{: .tip}

### 3.3 Reservar una cita médica (E6 · RF-015)

> <hands-on-title>Reservar cita</hands-on-title>
>
> ```bash
> curl -i -X POST http://localhost:5080/api/appointments \
>   -H "Content-Type: application/json" \
>   -d '{"scheduleSlotId":1,"patientId":1,"reason":"Dolor de cabeza recurrente"}'
> ```
>
> **Esperado**: `201 Created`, con la cita en `Pending`:
> ```json
> {
>   "id": 1, "status": "Pending", "reason": "Dolor de cabeza recurrente",
>   "diagnosis": null, "prescription": null,
>   "patient": { "id": 1, "dni": "45678912", "fullName": "Rosa García López" },
>   "doctor":  { "id": 1, "fullName": "Ana Torres Vega", "specialty": "Medicina General" },
>   "slot":    { "id": 1, "date": "2026-10-15", "startTime": "09:00", "endTime": "09:30" }
> }
> ```
{: .hands_on}

> <question-title></question-title>
> 1. ¿Qué estado muestra ahora el bloque si repites la consulta de agenda del paso 3.2?
> 2. ¿Qué devuelve una segunda reserva sobre el mismo bloque?
>
> > <solution-title></solution-title>
> > 1. `"status": "Reserved"` (RF-015).
> > 2. `409 Conflict` con `"code": "SLOT_NOT_AVAILABLE"` (RF-016).
> {: .solution}
{: .question}

> <tip-title>PowerShell en Windows</tip-title>
> En PowerShell, `curl` puede ser un alias de `Invoke-WebRequest` y las comillas del JSON se interpretan distinto. Usa `curl.exe` con el JSON en un archivo (`-d "@paciente.json"`) o el equivalente nativo:
>
> ```powershell
> Invoke-RestMethod -Method Post -Uri http://localhost:5080/api/patients `
>   -ContentType "application/json" `
>   -Body '{"dni":"45678912","firstName":"Rosa","lastName":"García López","phone":"+51 987 654 321"}'
> ```
>
> El archivo `src/CitasMedicas.Api/CitasMedicas.Api.http` (T5.10) contiene estas mismas llamadas, listas para Visual Studio o VS Code.
{: .tip}

---

## 4. Pruebas

> <hands-on-title>Ejecutar la suite</hands-on-title>
>
> ```bash
> dotnet test
> ```
>
> **Esperado**: `Con error: 0`, `Superado: N`, `Omitido: 0` (Constitución §5).
{: .hands_on}

> <tip-title>Filtrar por requisito o historia</tip-title>
>
> ```bash
> dotnet test --filter "RF=RF-013"                              # Conflicto de horarios
> dotnet test --filter "HU=HU-03"                               # Reserva de citas
> dotnet test --filter "FullyQualifiedName~ConcurrentReservation" # Concurrencia (RF-021)
> ```
{: .tip}
