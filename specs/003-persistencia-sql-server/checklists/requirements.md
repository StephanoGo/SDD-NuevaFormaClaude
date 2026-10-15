# Checklist de calidad de requisitos

Los 16 controles siguientes son una **propuesta de baseline**, pues el proyecto no ha confirmado el wording formal. Evalúan calidad de requisitos, no agregan comportamiento de producto. Estados: **Pasa**, **Falla**, **Necesita aclaración**.

| # | Control propuesto | Estado | Requisitos / hallazgo |
|---:|---|---|---|
| 1 | Cada requisito tiene ID estable y único. | Pasa | RF-PER-01–12. |
| 2 | Cada requisito describe un comportamiento atómico. | Falla | RF-PER-01 combina seis recursos, configuración, conexión y lectura/escritura; RF-PER-02 agrupa integridad y dos flujos atómicos; RF-PER-03 combina migración, conservación y arranque fallido. |
| 3 | Se identifica actor o componente responsable. | Pasa | Aplicación, API y almacenamiento están identificados. |
| 4 | El disparador o condición es explícito. | Pasa | Inicio, guardado, solicitudes concurrentes y acciones por API constan en escenarios. |
| 5 | El resultado esperado es explícito. | Pasa | Cada RF tiene al menos un resultado observable. |
| 6 | El wording tiene una sola interpretación. | Pasa | RF-PER-08–10 y 12 fijan campos requeridos, bloqueo por citas futuras no terminadas, filtro y efecto de especialidad inactiva. |
| 7 | Los términos vagos son medibles o eliminados. | Pasa | RF-PER-08–12 delimitan campos, estados terminales, filtros, cardinalidad y resultado de agenda/reserva. |
| 8 | Los términos del dominio son consistentes con módulos base. | Pasa | Usa paciente, médico, especialidad, bloque, cita, atención; la nueva tabla Atención se declara en decisión aclarada y RF-PER-06. |
| 9 | In Scope y Out of Scope son explícitos. | Pasa | Secciones 28–44. |
| 10 | Preconditions y estado relevante constan. | Pasa | SQL disponible, cita Pending y recursos relacionados se describen en escenarios. |
| 11 | El comportamiento exitoso tiene aceptación observable. | Pasa | Criterios Given/When/Then en líneas 60–72 de spec. |
| 12 | Validación y fallos relevantes están cubiertos. | Pasa | Contrato fija 400/404/409, filtro inválido y bloqueo de desactivación; RF-PER-03 cubre fallos de arranque. |
| 13 | Propiedad de datos y límites de módulos están claros. | Pasa | Persistencia transversal; mantenimiento de catálogos por API; módulos afectados en el plan. |
| 14 | Transiciones e invariantes concuerdan con el modelo. | Pasa | Cita terminada si Attended/Cancelled, bloque libre Available no bloquea; especialidad inactiva bloquea agenda/reserva; atención máximo una por cita. |
| 15 | Cada requisito tiene al menos un escenario Given/When/Then. | Pasa | RF-PER-01–12 están referenciados en la tabla de aceptación. |
| 16 | Requisitos funcionales evitan prescribir implementación innecesaria. | Pasa con excepción aprobada | SQL Server 2022 local, EF Core 10 y migraciones versionadas son restricciones aprobadas; EF Core se limita a SQL relacional/migraciones. |

## Hallazgos críticos

- RF-PER-01/02/03 aún agrupan subcomportamientos y podrían atomizarse sin cambiar las decisiones aprobadas.
- Confirmar el baseline de 16 controles propuesto antes de tratarlo como estándar formal del proyecto.
