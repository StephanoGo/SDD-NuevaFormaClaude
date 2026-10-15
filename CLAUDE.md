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

## SDD y módulos
- Las specs activas por módulo viven en `specs/modulos/`; el catálogo está en `specs/README.md`.
- Para toda feature significativa, seguir en orden `/speckit-specify` → `/speckit-plan` → `/speckit-checklist` → `/speckit-tasks` → `/speckit-analyze` → `/speckit-implement`.
- Mantener artefactos por módulo y concisos: `spec.md` (clarifications, in/out of scope, RF y Given/When/Then), `plan.md` (arquitectura, modelo y contratos), checklists de requisitos y dominio/UX cuando corresponda, y `tasks.md` con dependencias y trazabilidad RF.
- `/speckit-analyze` es una puerta de bloqueo: no implementar si detecta ambigüedades, contradicciones o RF sin cobertura en diseño, tareas y pruebas.
- Solo `/speckit-implement` modifica código ejecutable bajo `src/` y `tests/`. Debe añadir o ajustar pruebas, ejecutar `dotnet build` y `dotnet test`, y marcar tareas `[x]` únicamente cuando ambas comprobaciones pasen.
- Basarse en la spec y el código; no inventar requisitos. Aclarar con el usuario decisiones de negocio faltantes y reportar comprobaciones que no se pudieron ejecutar.
- Actualizar esta memoria solo cuando cambie una decisión técnica transversal.

Las reglas de desarrollo viven en `.specify/memory/constitution.md`. Las skills locales que exponen los seis comandos están en `.claude/skills/speckit-*/SKILL.md`. Los documentos de entregas anteriores bajo `specs/001-citas-medicas-core` y `specs/002-interfaz-web-citas` son registros compactos; las versiones extensas están en el historial de Git.
