# Checklist de calidad de requisitos

Los 16 controles son una **propuesta de baseline** (el proyecto no confirmó wording formal). Evalúan calidad de requisitos, no agregan comportamiento. Estados: **Pasa**, **Falla**, **Necesita aclaración**. Se revisan contra la spec vigente (SQL Server 2025 Developer local).

| # | Control | Estado | Hallazgo |
|---:|---|---|---|
| 1 | ID estable y único. | Pasa | RF-PER-01–12. |
| 2 | Comportamiento atómico. | Falla | RF-PER-01 agrupa recursos y persistencia; RF-PER-02 integridad/reserva/atención; RF-PER-03 migración/preservación/fallo de arranque. |
| 3 | Actor/componente responsable. | Pasa | Aplicación, API y almacenamiento identificados. |
| 4 | Disparador/condición explícitos. | Pasa | Inicio, guardado, concurrencia y acciones por API constan. |
| 5 | Resultado esperado explícito. | Pasa | Cada RF tiene al menos un resultado observable. |
| 6 | Interpretación única. | Pasa | RF-PER-08–10 y 12 fijan campos, bloqueo por citas futuras, filtro y efecto de especialidad inactiva; motor y base nombrados. |
| 7 | Términos vagos medibles/eliminados. | Pasa | RF-PER-08–12 delimitan campos, estados terminales, filtros, cardinalidad y resultado de agenda/reserva. |
| 8 | Términos consistentes con módulos base. | Pasa | Usa paciente, médico, especialidad, bloque, cita, atención; la tabla Atención se declara en decisión aclarada y RF-PER-06. |
| 9 | In/Out of Scope explícitos. | Pasa | Secciones «Alcance» y «Fuera de alcance». |
| 10 | Precondiciones y estado relevante. | Pasa | SQL disponible, estados de dominio y base dedicada preexistente vacía para AC-PER-04; AC-PER-15 requiere migración posterior real. |
| 11 | Aceptación observable. | Pasa (un escenario futuro pendiente) | AC-PER-01–14 verificables; AC-PER-04 se bloquea sin base vacía; AC-PER-15 requiere migración posterior real. |
| 12 | Validación y fallos cubiertos. | Pasa | Contrato fija 400/404/409 y fallos de arranque; pruebas SQL sin conexión segura se reportan bloqueadas, nunca contra `BD_MedicinaGen`/`master`. |
| 13 | Propiedad de datos y límites de módulos. | Pasa | Persistencia transversal; catálogos por API; módulos afectados en el plan. |
| 14 | Transiciones e invariantes concuerdan. | Pasa | Cita termina si `Attended`/`Cancelled`; bloque `Available` libre no bloquea; especialidad inactiva bloquea agenda/reserva; máximo una atención por cita. |
| 15 | Escenario Given/When/Then por requisito. | Pasa | RF-PER-01–12 referenciados en la tabla de aceptación. |
| 16 | Evita prescribir implementación innecesaria. | Pasa (excepción aprobada) | Motor aprobado SQL Server 2025 Developer local; EF Core 10 limitado a persistencia SQL relacional y migraciones. |

## Hallazgos críticos

- RF-PER-01/02/03 agrupan subcomportamientos; podrían atomizarse sin cambiar decisiones.
- Confirmar el baseline de 16 controles antes de tratarlo como estándar formal.
- AC-PER-04 exige `BD_MedicinaGen_Tests` preexistente y confirmada vacía; sin eso la verificación se bloquea sin limpieza ni fallback. AC-PER-15 pendiente hasta una migración posterior real distinta de `202610150001_InitialClinicSchema`; reaplicar `MigrateAsync()` no verifica actualización.
