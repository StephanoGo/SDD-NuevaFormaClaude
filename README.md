# Sistema de Gestión de Citas Médicas

Aplicación web para gestionar el ciclo esencial de atención de una clínica: registrar y localizar pacientes, organizar la agenda de médicos, reservar citas y documentar la atención mediante diagnóstico y receta.

El proyecto está orientado a dos perfiles operativos: la **recepcionista**, que administra pacientes, horarios y reservas; y el **médico**, que consulta la cita y registra el resultado de la atención. La solución prioriza una operación sencilla, reglas clínicas consistentes y trazabilidad desde cada requisito hasta su implementación y sus pruebas.

## Qué permite hacer

- Registrar pacientes y buscarlos por DNI o apellidos.
- Crear y consultar bloques horarios para médicos activos.
- Reservar una cita sobre un horario disponible de forma atómica, evitando reservas duplicadas incluso ante solicitudes concurrentes.
- Consultar el detalle completo de una cita.
- Registrar diagnóstico y receta, cerrando la cita como atendida e inmutable.
- Ejecutar todos los flujos desde una SPA responsiva y accesible, con validaciones y notificaciones claras en español.

## Flujo principal

1. La recepcionista registra o localiza al paciente.
2. Consulta la agenda de un médico para una fecha determinada.
3. Selecciona un bloque disponible y confirma la reserva indicando el motivo de consulta.
4. El médico recupera la cita pendiente y registra la atención.
5. La cita pasa a estado **Atendida** y sus datos clínicos quedan protegidos contra modificaciones posteriores.

## Arquitectura y tecnología

La solución utiliza **.NET 10**, **C# 13** y **Minimal APIs**, organizada en capas con responsabilidades separadas:

- **Domain**: entidades, reglas de negocio y errores clínicos.
- **Application**: casos de uso y coordinación de operaciones.
- **Infrastructure**: repositorios y persistencia en memoria desacoplada mediante interfaces.
- **Api**: endpoints REST, publicación de archivos estáticos y traducción de errores a `ProblemDetails`.
- **Web**: SPA ligera en HTML5, CSS y JavaScript estándar, sin dependencias NPM.
- **Tests**: pruebas automatizadas con xUnit y FluentAssertions.

La interfaz es servida por la propia API desde `wwwroot`, funciona desde 360 px hasta escritorio y está diseñada con contraste compatible con WCAG 2.1 AA.

## Principios del proyecto

El desarrollo sigue un ciclo SDD conciso para evitar suposiciones y mantener calidad:

1. Ejecutar `/speckit-specify` para definir alcance, clarificaciones, RF y criterios verificables.
2. Ejecutar `/speckit-plan`, `/speckit-checklist` y `/speckit-tasks` para diseño, control de calidad y desglose trazable.
3. Exigir aprobación de `/speckit-analyze` antes de `/speckit-implement`.
4. Ejecutar `dotnet build` y `dotnet test`; completar tareas solo con ambos en verde.

## Specs por módulo

| Módulo | Estado | Spec |
| --- | --- | --- |
| Pacientes | Implementado | [Spec](specs/modulos/pacientes.md) |
| Agenda | Implementado | [Spec](specs/modulos/agenda.md) |
| Citas | Implementado | [Spec](specs/modulos/citas.md) |
| Atenciones | Registro básico implementado | [Spec](specs/modulos/atenciones.md) |
| Historia Clínica | Propuesta pendiente de definición | [Spec](specs/modulos/historia-clinica.md) |
| Experiencia web | Criterios comunes | [Spec](specs/modulos/experiencia-web.md) |

El [catálogo de specs](specs/README.md) reúne el ciclo y los módulos. Las entregas 001 y 002 se conservan como registros históricos compactos; los detalles anteriores pueden recuperarse del historial de Git.

## Puesta en marcha

Requisitos: SDK de .NET 10.

```bash
dotnet build
dotnet test
dotnet run --project src/CitasMedicas.Api
```

Una vez iniciado el servidor, la aplicación web queda disponible en la URL local indicada por Kestrel.

## Alcance actual

Esta versión se concentra en el flujo clínico esencial de una única sede y usa médicos precargados y persistencia en memoria. No incluye autenticación, gestión del catálogo de médicos, cancelación o reprogramación de citas, notificaciones ni facturación. La consulta longitudinal de historia clínica está propuesta, pero aún no implementada.
