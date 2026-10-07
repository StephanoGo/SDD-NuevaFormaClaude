# Research: Núcleo de Citas Médicas

| Campo | Valor |
| :--- | :--- |
| **Feature** | `001-citas-medicas-core` |
| **Plan** | [plan.md](./plan.md) |
| **Fecha** | 2026-10-07 |

Cada decisión sigue el formato **Decisión / Justificación / Alternativas descartadas**. Las decisiones resuelven las incógnitas técnicas del [plan.md](./plan.md) y no introducen requisitos nuevos.

---

## Entorno verificado

| Elemento | Resultado |
| :--- | :--- |
| SDK instalado | `10.0.401` (`dotnet --list-sdks`) |
| Plantilla `webapi` | `net10.0`, sin paquetes NuGet con `--no-openapi` |
| Plantilla `xunit` | `xunit 2.9.3`, `xunit.runner.visualstudio 3.1.4`, `Microsoft.NET.Test.Sdk 17.14.1`, `coverlet.collector 6.0.4` |

---

## R-01 — Estilo de API: Minimal APIs

- **Decisión**: Minimal APIs agrupadas por recurso (`MapGroup`), una clase estática de endpoints por recurso.
- **Justificación**: no requiere paquetes adicionales y deja los endpoints en el mínimo: recibir el DTO, invocar el servicio de Aplicación y traducir el `Result` a HTTP. Así se cumple la Constitución §3 (ninguna regla en la capa de presentación).
- **Alternativas**: Controllers MVC. Se descartan porque añaden más ceremonia (filtros, atributos, model binding) sin aportar valor para 7 endpoints.

## R-02 — Persistencia en esta fase: repositorios en memoria

- **Decisión**: implementar los tres repositorios en memoria dentro de `CitasMedicas.Infrastructure`, registrados como *Singleton*. SQL Server con Stored Procedures queda para una feature posterior, sin cambiar los contratos.
- **Justificación**: CLAUDE.md permite «repositorio en memoria según la fase». Así no hace falta agregar `Microsoft.Data.SqlClient` (Constitución §1), las pruebas son deterministas y los contratos de [api-contracts.md §3](./contracts/api-contracts.md) ya están diseñados para operaciones atómicas que luego se trasladan a SPs.
- **Alternativas**: SQL Server con SPs desde ya. Se descarta para esta fase porque requiere un paquete NuGet adicional, scripts de base de datos e infraestructura de pruebas de integración.
- **Consecuencia**: los datos se pierden al reiniciar la API. Es aceptable porque la spec no exige durabilidad en esta feature.

## R-03 — Atomicidad y concurrencia (RF-020, RF-021, INV-02, INV-03)

- **Decisión**: los repositorios exponen **operaciones atómicas de negocio** en lugar de un `SaveChanges` genérico:
  - `IAppointmentRepository.ReserveAsync(slot, appointment)`: *compare-and-set*. Confirma solo si el bloque persistido sigue en `Available`; en la misma sección crítica cambia el bloque a `Reserved` e inserta la cita. Si no, devuelve `SLOT_NOT_AVAILABLE`.
  - `IAppointmentRepository.AddSlotAsync(slot)`: dentro de la sección crítica vuelve a verificar el solapamiento invocando el método de dominio `ScheduleSlot.OverlapsWith`.
  - `IAppointmentRepository.RegisterAttentionAsync(appointment)`: confirma solo si la cita persistida sigue en `Pending`.
  - `IPatientRepository.AddAsync(patient)`: verifica la unicidad del DNI dentro de la sección crítica.
- **Implementación en memoria**: un `lock` por repositorio, que sirve de sección crítica equivalente a una transacción serializable.
- **Futuro con SQL Server**: cada operación pasa a ser un SP con `BEGIN TRAN`, `UPDATE … WHERE Status = 'Available'` + `@@ROWCOUNT`, `UPDLOCK, HOLDLOCK` para el solapamiento e índice único sobre `Dni`.
- **Justificación**: la regla de negocio se evalúa en el Dominio (Constitución §3). El repositorio solo garantiza que el estado sobre el que se decidió no cambió antes de confirmar (Constitución §4).
- **Alternativas**: interfaz `IUnitOfWork` genérica. Se descarta porque añade un cuarto contrato no solicitado y, en memoria, no ofrece aislamiento real. También se descarta la concurrencia optimista con columna `Version`, porque requiere un atributo que no está en [data-model.md](./data-model.md).

## R-04 — Ubicación de `ScheduleSlot` en los repositorios

- **Decisión**: `IAppointmentRepository` gestiona tanto `ScheduleSlot` como `Appointment`.
- **Justificación**: la reserva modifica ambas entidades en una sola operación atómica (RF-020). Si estuvieran en repositorios distintos, haría falta coordinar transacciones entre ellos. Además, el usuario fijó tres contratos (`IPatientRepository`, `IDoctorRepository`, `IAppointmentRepository`).
- **Alternativas**: un `IScheduleRepository` propio. Se descarta porque obliga a coordinar transacciones y crea un cuarto contrato. Ubicar los bloques en `IDoctorRepository` también se descarta, porque separa el bloque de su cita.

## R-05 — Ubicación de los contratos de repositorio en el código

- **Decisión**: las interfaces viven en `CitasMedicas.Application/Abstractions/Persistence`. `Infrastructure` las implementa.
- **Justificación**: el Dominio queda puro (sin `Task`, sin conceptos de persistencia) y la Aplicación define los puertos que necesita. Dependencias: `Domain ← Application ← Infrastructure`, y `Api` actúa como raíz de composición.
- **Alternativas**: interfaces en el Dominio. Es válido, pero acopla el Dominio a la asincronía y a la persistencia.

## R-06 — Manejo de errores: patrón `Result` propio (RF-029)

- **Decisión**: tipos `Result` / `Result<T>` y `Error(Code, Message, Kind)` propios en `CitasMedicas.Domain/Common`. `Kind ∈ {Validation, NotFound, Conflict}`. Los errores de negocio **no** usan excepciones.
- **Mapeo HTTP**: `Validation → 400`, `NotFound → 404`, `Conflict → 409`. La respuesta usa `ProblemDetails` (incluido en ASP.NET Core) con la extensión `code`.
- **Justificación**: códigos estables y verificables en pruebas (RF-029), sin paquetes externos (Constitución §1).
- **Alternativas**: los paquetes `FluentResults` y `ErrorOr` se descartan por la Constitución §1. Las excepciones de dominio con middleware se descartan porque usan excepciones para controlar el flujo y hacen las pruebas menos expresivas.

## R-07 — Fecha y hora actual y zona horaria (RF-012, RF-019, supuesto 6)

- **Decisión**: inyectar `System.TimeProvider` (incluido en .NET) y convertir a hora local con `TimeZoneInfo.FindSystemTimeZoneById("America/Lima")`. El Dominio recibe `now` como parámetro (`DateTime` local de la clínica) y nunca consulta el reloj directamente.
- **Pruebas**: una clase `FixedTimeProvider : TimeProvider` propia en los proyectos de prueba.
- **Justificación**: pruebas deterministas sin `Microsoft.Extensions.TimeProvider.Testing` (Constitución §1). Los identificadores IANA funcionan en Windows con .NET 6+ (ICU). Lima es UTC−5 sin horario de verano.
- **Alternativas**: `DateTime.Now`, descartado porque no se puede probar. Una interfaz `IClock` propia también se descarta, porque duplica a `TimeProvider`.

## R-08 — Búsqueda sin distinguir mayúsculas ni tildes (RF-007)

- **Decisión**: un helper de dominio `TextNormalizer.ForSearch(string)` aplica `Trim`, `Normalize(FormD)`, elimina los `NonSpacingMark` y convierte con `ToUpperInvariant`. El repositorio en memoria compara con `Contains` sobre los valores normalizados.
- **Justificación**: el resultado es determinista e independiente de la cultura y de `InvariantGlobalization`. Ejemplo: «Núñez» → «NUNEZ».
- **Futuro con SQL Server**: collation `Modern_Spanish_CI_AI` en la columna `LastName`.
- **Alternativas**: `CompareInfo.IndexOf` con `IgnoreNonSpace` se descarta porque depende de la cultura y falla con globalización invariante.

## R-09 — Tipos de fecha y hora

- **Decisión**: `DateOnly` para `Date` y `TimeOnly` para `StartTime`/`EndTime` en Dominio y Aplicación. En la API, los DTOs usan cadenas con formato `yyyy-MM-dd` y `HH:mm`, y la API las convierte. Un formato inválido devuelve `400 VALIDATION_FORMAT`.
- **Justificación**: los tipos coinciden con el modelo lógico y el contrato JSON es estable (por ejemplo `"09:00"` en lugar de `"09:00:00"`).
- **Alternativas**: `DateTime` para todo, descartado porque es ambiguo para horas sin fecha.

## R-10 — Paquetes NuGet declarados (Constitución §1)

Lista **cerrada**. Ningún otro paquete puede agregarse sin modificar este plan.

| Paquete | Versión | Proyecto | Motivo |
| :--- | :--- | :--- | :--- |
| `Microsoft.NET.Test.Sdk` | 17.14.1 | Proyectos de prueba | Host de pruebas (`dotnet test`) |
| `xunit` | 2.9.3 | Proyectos de prueba | Framework de pruebas (CLAUDE.md) |
| `xunit.runner.visualstudio` | 3.1.4 | Proyectos de prueba | Runner para `dotnet test` |
| `FluentAssertions` | 7.x (fijar la última 7.x al crear el proyecto) | Proyectos de prueba | Aserciones (CLAUDE.md) |

- **Sin paquetes** en `Domain`, `Application`, `Infrastructure` ni `Api`.
- **`FluentAssertions` 7.x y no 8.x**: desde la v8 la licencia es comercial (Xceed). La rama 7.x conserva la licencia Apache 2.0.
- **Se elimina `coverlet.collector`** de la plantilla `xunit`: la spec no exige medir cobertura.
- **Se crea `webapi` con `--no-openapi`**: el contrato está documentado en Markdown. Swagger u OpenAPI requeriría declarar `Microsoft.AspNetCore.OpenApi`.

## R-11 — Calidad de compilación (Constitución §5)

- **Decisión**: `Directory.Build.props` en la raíz con `Nullable=enable`, `ImplicitUsings=enable`, `TreatWarningsAsErrors=true` y `LangVersion=13`.
- **Justificación**: refuerza el criterio «0 errores» de `dotnet build`, porque las advertencias de nulabilidad se detectan como errores.
- **Alternativas**: dejar las advertencias como tales. Se descarta porque permite deuda silenciosa.

## R-12 — Estrategia de pruebas

- **Decisión**: dos proyectos de prueba.
  - `CitasMedicas.Domain.Tests` (unitarias, sin dependencias): validación de pacientes, rango y solapamiento de bloques (incluidos los contiguos), bloques en el pasado, transiciones de estado de la cita e inmutabilidad de *Atendida*.
  - `CitasMedicas.Application.Tests`: servicios de aplicación con repositorios en memoria reales y `FixedTimeProvider`. Cubre los escenarios de aceptación de HU-01 a HU-04, la unicidad del DNI y la **reserva concurrente** (N tareas en paralelo sobre el mismo bloque, con exactamente 1 éxito).
- **Justificación**: el usuario pide pruebas unitarias de dominio, y CE-001 a CE-003 exigen además cubrir la concurrencia y la atomicidad, que viven en Aplicación e Infraestructura.
- **Alternativas**: pruebas HTTP con `WebApplicationFactory`. Se descartan porque requieren `Microsoft.AspNetCore.Mvc.Testing`, que no está declarado. Pueden añadirse en una feature futura.
