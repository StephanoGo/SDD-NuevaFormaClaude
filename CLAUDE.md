# Memoria Técnica - Citas Médicas App

Sistema de gestión médica para registro de pacientes, agenda de médicos, programación de citas y registro de atenciones clínicas.

## Stack Tecnológico
- Backend: .NET 10 (Web API / Minimal APIs o Controllers) con C# 13.
- Persistencia: SQL Server (100% Stored Procedures) o Repositorio en memoria según la fase.
- Pruebas: xUnit + FluentAssertions (`dotnet test`).
- Idioma: Código e identificadores en inglés; interfaz y documentación en español.

## Comandos Frecuentes
- Compilar: `dotnet build`
- Pruebas: `dotnet test`
- Ejecutar: `dotnet run --project src/CitasMedicas.Api`

## Convenciones
- Arquitectura limpia en 3 capas (Domain, Application, Infrastructure).
- Cada feature vive en su rama Git: `NNN-nombre-feature`.
- Ninguna línea de código de negocio se implementa sin un plan y tareas aprobadas.
- Actualizar este archivo al cerrar cada spec con decisiones transversales.

Las reglas de producto viven en .specify/memory/constitution.md y el estado del producto en specs/README.md.

