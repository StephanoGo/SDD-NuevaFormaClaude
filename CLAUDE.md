# Memoria Técnica - Citas Médicas App

Sistema de gestión médica para registro de pacientes, agenda de médicos, programación de citas y registro de atenciones clínicas.

## Stack Tecnológico
- Backend: .NET 10 (Minimal APIs agrupadas por recurso) con C# 13.
- Persistencia: Repositorios desacoplados en memoria para la fase inicial (`IPatientRepository`, `IDoctorRepository`, `IAppointmentRepository`). Preparado para migración a SQL Server con Stored Procedures en fases posteriores.
- Pruebas: xUnit 2.9.3 + FluentAssertions 7.2.2 en un único proyecto `tests/CitasMedicas.Tests`.
- Idioma: Código e identificadores en inglés; interfaz y documentación en español.

## Comandos Frecuentes
- Compilar: `dotnet build`
- Pruebas: `dotnet test`
- Ejecutar: `dotnet run --project src/CitasMedicas.Api`

## Decisiones Transversales
- **Patrón Result propio**: Manejo de errores clínicos y de validación mediante `Result` / `Result<T>` y `Error(Code, Message, Kind)` en `Domain/Common`, mapeado a `ProblemDetails` con extensión `code` en la Web API (400, 404, 409). Sin excepciones de negocio ni paquetes externos.
- **Transacciones Atómicas y Compare-and-Set**: La reserva de citas y registro de atenciones clínicas aplican semántica atómica de tipo *compare-and-set* con bloqueo concurrente en persistencia.
- **Semántica de Copias**: Los repositorios en memoria almacenan y devuelven copias profundas mediante métodos internos `Rehydrate`, garantizando inmutabilidad del estado persistido.
- **Lista Cerrada de Paquetes NuGet**: Prohibido agregar paquetes en proyectos de producción (`Domain`, `Application`, `Infrastructure`, `Api`). Solo `FluentAssertions 7.x`, `Microsoft.NET.Test.Sdk`, `xunit` y su runner en pruebas.
- **Zona Horaria de la Clínica**: Manejo de fechas y horas locales basado en `System.TimeProvider` y la zona horaria `America/Lima`.

## Convenciones
- Arquitectura limpia en 3 capas (`Domain`, `Application`, `Infrastructure`) + Host Web API (`Api`).
- Cada feature vive en su rama Git: `NNN-nombre-feature`.
- Ninguna línea de código de negocio se implementa sin un plan y tareas aprobadas.
- Actualizar este archivo al cerrar cada spec con decisiones transversales.

Las reglas de producto viven en .specify/memory/constitution.md y el estado del producto en specs/README.md.
