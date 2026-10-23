# Specs del Proyecto

Las funcionalidades se organizan por módulos: **Pacientes**, **Agenda**, **Citas**, **Atenciones** e **Historia Clínica**. Historia Clínica es el flujo principal; médicos es un catálogo de apoyo en la implementación actual.

## Ciclo SDD por feature

1. `/speckit-specify`: registrar necesidad original, metadatos, rama, alcance, clarificaciones abiertas, RF identificados y criterios Given/When/Then.
2. `/speckit-plan`: definir arquitectura, validar la Constitución y producir plan técnico, modelo de datos e interfaces HTTP/JSON necesarias.
3. `/speckit-checklist`: comprobar las 16 reglas formales de requisitos y agregar checklist de dominio o UX según corresponda.
4. `/speckit-tasks`: desglosar trabajo en tareas atómicas con dependencias y referencias a RF.
5. `/speckit-analyze`: auditar coherencia y bloquear el avance si quedan contradicciones, requisitos sin contrato/tarea o decisiones ambiguas.
6. `/speckit-implement`: tras aprobación de `analyze`, implementar en `src/` y `tests/`, ejecutar `dotnet build` y `dotnet test`, y marcar tareas solo cuando pasen.

Los seis pasos se mantienen para cada feature; se reduce el volumen quitando duplicación, no suprimiendo controles. Las entregas/features se organizan en `specs/NNN-nombre/` (004 en adelante); `specs/modulos/` conserva los resúmenes vigentes por capacidad. Mantener los contratos compartidos en un único lugar y actualizar documentación vigente que el cambio vuelva incorrecta. El contenido detallado anterior de las entregas 001 y 002 permanece en Git; sus archivos actuales son registros compactos.

La constitución está en [`.specify/memory/constitution.md`](../.specify/memory/constitution.md). Las specs activas por módulo son:

- [Pacientes](./modulos/pacientes.md)
- [Agenda](./modulos/agenda.md)
- [Citas](./modulos/citas.md)
- [Atenciones](./modulos/atenciones.md)
- [Historia Clínica](./modulos/historia-clinica.md)
- [Experiencia web](./modulos/experiencia-web.md)

Las carpetas `001-citas-medicas-core` y `002-interfaz-web-citas` son registros compactos de entregas anteriores. El contrato HTTP se mantiene en [api-contracts.md](./001-citas-medicas-core/contracts/api-contracts.md). Las definiciones locales invocables están en `.claude/skills/speckit-*/SKILL.md`.

## Especificaciones existentes

| Número | Módulo / entrega | Spec | Estado conocido |
| :---: | :--- | :--- | :--- |
| 001 | Core: pacientes, agenda, citas y registro de atención | [Registro](./001-citas-medicas-core/spec.md) | Entrega completada; detalle original en Git |
| 002 | Interfaz web | [Registro](./002-interfaz-web-citas/spec.md) | Entrega completada; detalle original en Git |
| 003 | Persistencia SQL Server para demo clínica | [Spec](./003-persistencia-sql-server/spec.md) | Persistencia implementada; T002 pendiente de base de pruebas vacía preexistente y T011 pendiente de migración posterior real |
| 004 | Historia Clínica: consulta del historial | [Spec](./004-historia-clinica/spec.md) | Implementada; pruebas SQL de integración dependen de la base dedicada |
| 005 | Citas: consulta y cancelación | [Spec](./005-citas-consulta-cancelacion/spec.md) | Implementada; pruebas SQL de integración dependen de la base dedicada |
| 006 | Web: programación de médicos y especialidades | [Spec](./006-programacion-web/spec.md) | Implementada en frontend estático separado |
| 007 | Citas: reprogramación | [Spec](./007-reprogramacion-citas/spec.md) | Especificación aprobada; plan, modelo, contratos, checklists y tareas creados; implementación pendiente |

Los detalles de diseño, contratos, checklists y tareas están en las carpetas 004–007. Los documentos por módulo enlazan a esas entregas.
