# Guía SDD: reprogramación de citas con Spec Kit

Esta guía explica cómo desarrollar la próxima feature del proyecto mediante sus seis comandos de Spec Kit. Es una guía de trabajo: no constituye una spec aprobada ni indica que la función esté implementada.

## 1. Preparar el contexto

Abre el asistente desde la raíz de `CitasMedicasApp`. Los comandos se escriben en el chat del asistente que tenga cargada la configuración del repositorio, no en PowerShell.

El proyecto define los comandos en `.opencode/commands/` y sus instrucciones en `.claude/skills/speckit-*/SKILL.md`. Usa los nombres con guion que aparecen aquí. Si tu asistente no los muestra, verifica que haya cargado esa configuración; no supongas que ejecutar el texto en una terminal invoca Spec Kit.

La siguiente entrega disponible al redactar esta guía es `007-reprogramacion-citas`. Comprueba `specs/README.md` antes de comenzar por si ya se asignó ese número.

El módulo propietario será Citas; Agenda y experiencia web serán módulos afectados. La afectación de Historia Clínica debe evaluarse según las reglas elegidas. Una rama propuesta es `codex/007-reprogramacion-citas`; registrar su nombre en la spec no demuestra que exista una rama Git.

## 2. Definir el comportamiento: `/speckit-specify`

Este paso establece **qué debe hacer la función** y cómo se comprobará. Copia este mensaje:

```text
/speckit-specify Crear la feature 007-reprogramacion-citas para permitir que recepción cambie el horario de una cita pendiente desde la web. Conservar el paciente y el motivo de consulta. El horario destino debe estar disponible y ser futuro según la zona horaria de la clínica. Liberar el bloque anterior y reservar el nuevo como una sola operación: si el cambio falla, conservar intacta la cita original. Rechazar la reprogramación de citas atendidas o canceladas y evitar reservas duplicadas ante solicitudes concurrentes. Quiero trazabilidad del cambio de horario y de su motivo. Revisa las specs y el código existentes, pregunta por las decisiones de negocio pendientes y crea únicamente el spec.md de esta etapa. Usa IDs de requisitos que no colisionen con los RF-CIT existentes. Propón la rama codex/007-reprogramacion-citas.
```

El texto anterior propone el alcance inicial. Revisa las reglas y apruébalas en la spec antes de tratarlas como definitivas.

### Decisiones que debes resolver

El asistente debe aclarar contigo, como mínimo:

- ¿Se permite cambiar de médico? Si se permite, ¿debe tener la misma especialidad?
- ¿Se puede reprogramar una cita cuyo horario original ya pasó?
- ¿El motivo del cambio es obligatorio? ¿Qué límites tendrá?
- ¿La cita conserva su identificador? ¿Dónde se consulta el registro de cambios y qué información muestra?
- ¿Se permiten varias reprogramaciones de una misma cita?
- ¿Qué respuesta recibe el operador si elige el mismo bloque o si otra persona modifica la cita mientras confirma?

No copies respuestas por defecto sin revisarlas. Las decisiones aprobadas deben quedar en `spec.md`; mientras existan preguntas que cambien el comportamiento, la spec permanece en borrador.

### Respuestas seleccionadas para tener en cuenta

Según las opciones compartidas por el usuario, usa estas respuestas al resolver las aclaraciones de esta feature y regístralas en `spec.md`:

| Tema | Respuesta seleccionada |
| --- | --- |
| Médico | Conservar el mismo médico. |
| Cita pasada | Sí, permitir reprogramar una cita cuyo horario original ya pasó, siempre que siga pendiente y el destino sea futuro. |
| Motivo del cambio | Obligatorio, con un máximo de 500 caracteres. |
| Trazabilidad | Conservar el ID de la cita y mantener un historial de reprogramaciones. |
| Conflictos | Rechazar la operación sin cambios; conservar la cita y su reserva original. |

Estas respuestas no resuelven por sí solas dónde se consultará el historial, qué información mostrará ni si habrá un límite de reprogramaciones. Aclara esos puntos durante `specify` antes de avanzar al plan.

### Resultado esperado

`specs/007-reprogramacion-citas/spec.md`, con solicitud original, módulo propietario, módulos afectados, metadatos, alcance, fuera de alcance, aclaraciones, RF con IDs estables y criterios Dado/Cuando/Entonces vinculados a esos RF.

Ejemplo de criterio para revisar:

> Dada una cita pendiente y un horario destino que otro operador acaba de reservar, cuando recepción confirma el cambio, entonces el sistema rechaza la reprogramación y conserva el horario y la reserva originales.

Antes de avanzar, comprueba que las respuestas a las aclaraciones estén registradas y que los criterios cubran éxito, errores y concurrencia.

## 3. Diseñar la solución: `/speckit-plan`

Con las decisiones funcionales resueltas, copia:

```text
/speckit-plan specs/007-reprogramacion-citas. Diseña la solución conforme al spec.md y la constitución del proyecto. Revisa el modelo de citas y bloques, los repositorios, la persistencia SQL Server y la web existentes. Define cómo proteger el cambio completo frente a fallos y concurrencia, incluyendo reprogramación contra reserva, cancelación, atención y otra reprogramación. Documenta modelo de datos, impacto de migraciones, contratos HTTP/JSON y de repositorio, errores y estrategia de pruebas. Reutiliza los contratos compartidos por referencia. No modifiques src/ ni tests/.
```

Resultados esperados:

- `plan.md`: arquitectura, capas afectadas, decisiones técnicas y verificación.
- `data-model.md`: datos, relaciones, invariantes e impacto en persistencia.
- `contracts/`: interfaces nuevas o modificadas, con solicitudes, respuestas y errores precisos.

Aquí se decide la implementación técnica. No agregues un endpoint o un campo definitivo en la guía como sustituto del diseño. Si el plan descubre una decisión funcional faltante, resuélvela primero en la spec.

## 4. Revisar calidad: `/speckit-checklist`

```text
/speckit-checklist specs/007-reprogramacion-citas. Audita los requisitos y crea checklists de dominio y UX centrados en reprogramación, integridad de la reserva, concurrencia, confirmación del cambio y mensajes de error. Vincula los hallazgos con los RF y distingue preguntas pendientes de defectos. Mantén identificadas como propuestas las 16 comprobaciones formales mientras no hayan sido confirmadas por el propietario del proyecto.
```

Resultado esperado: `checklists/requirements.md` y checklists de dominio/UX según corresponda. Las instrucciones locales indican que la lista formal de 16 comprobaciones es una línea base propuesta, no una lista original ya confirmada.

Revisa especialmente que un fallo conserve la cita original, que no queden dos reservas activas por el cambio y que la interfaz permita comprender el horario anterior y el destino antes de confirmar.

Corrige los artefactos de la etapa correspondiente cuando aparezcan ambigüedades. Un checklist revisa la calidad de los requisitos; no demuestra por sí solo que el código funcione.

## 5. Desglosar el trabajo: `/speckit-tasks`

```text
/speckit-tasks specs/007-reprogramacion-citas. Genera tareas atómicas y ordenadas por dependencia para dominio, aplicación, persistencia, API, interfaz web y pruebas según el diseño aprobado. Vincula cada tarea con sus RF y criterios de aceptación. Incluye migraciones si el modelo las requiere, actualización de documentación vigente y verificación final con dotnet build y dotnet test. Deja todas las tareas sin marcar y no modifiques src/ ni tests/.
```

Resultado esperado: `tasks.md`, con casillas pendientes, dependencias y trazabilidad. Cada RF debe tener trabajo que lo implemente y pruebas planificadas que verifiquen su comportamiento.

### Aclaraciones surgidas durante `/speckit-tasks`

Durante esta etapa, el asistente presentó cuatro preguntas adicionales. El usuario compartió estas respuestas seleccionadas para documentarlas:

| Tema | Respuesta seleccionada |
| --- | --- |
| Futuro | Inicio futuro: el inicio del horario destino debe ser posterior al momento actual. |
| Timeout | Recargar detalle. |
| Origen inconsistente | Rechazar y reportar. |
| Auditoría | Hora clínica. |

Para interpretar estas respuestas junto con el contexto de la feature:

- **Futuro:** evaluar el inicio del bloque destino con la hora de la clínica, no únicamente su fecha o su hora de fin.
- **Timeout:** recargar el detalle de la cita para consultar su estado antes de decidir cómo continuar; una respuesta agotada por tiempo no confirma por sí sola si el cambio se guardó.
- **Origen inconsistente:** rechazar la reprogramación y reportar la inconsistencia. Precisar en los contratos qué condiciones constituyen esa inconsistencia y cómo se informa.
- **Auditoría:** usar la hora de la clínica (`America/Lima`) para la fecha y hora del registro del cambio; documentar su representación técnica en el modelo y los contratos.

Si estas decisiones aún no están en los artefactos, actualizar `spec.md` y después el plan, el modelo, los contratos y los checklists afectados antes de completar las tareas. Las tareas deben derivarse de esas decisiones documentadas. Ejecutar `/speckit-analyze` sobre el conjunto actualizado.

## 6. Validar coherencia: `/speckit-analyze`

```text
/speckit-analyze specs/007-reprogramacion-citas. Contrasta spec, plan, modelo de datos, contratos, checklists y tareas con la constitución y el código existente. Verifica cobertura de todos los RF, decisiones de negocio resueltas y consistencia de estados, errores, persistencia y concurrencia. Devuelve BLOCKED con hallazgos concretos o READY FOR IMPLEMENTATION. No implementes código.
```

Resultados posibles:

| Resultado | Siguiente acción |
| --- | --- |
| `BLOCKED` | Corregir los artefactos señalados, actualizar los dependientes y volver a ejecutar el análisis. |
| `READY FOR IMPLEMENTATION` | Continuar con la implementación de las tareas analizadas. |

Un ejemplo de bloqueo sería exigir un historial de reprogramaciones en la spec sin incluir su persistencia en el diseño ni tareas que lo implementen y prueben.

Conserva el resultado del análisis; las instrucciones locales no fijan un nombre obligatorio de archivo para él. Las entregas anteriores usan `analyze.md`. Si cambias el comportamiento o el diseño después de un análisis favorable, actualiza los documentos afectados y vuelve a analizar antes de implementar.

### Bloqueo encontrado en esta entrega y cómo resolverlo

Al ejecutar el análisis de la entrega 007 se reportó que los RF-REPRO-01–06 tenían criterios, diseño, contratos, tareas y pruebas planificadas, pero había inconsistencias documentales y la spec seguía pendiente de aprobación. La revisión de los archivos confirmó:

| Archivo | Situación observada | Corrección |
| --- | --- | --- |
| `spec.md` | Aclaraciones resueltas, pero estado de borrador pendiente de aprobación. | Registrar la aprobación explícita del propietario cuando la otorgue. |
| `checklists/requirements.md` | Conserva preguntas sobre futuro, timeout y origen inconsistente; también observaciones antiguas sobre auditoría y refresco de interfaz. | Reevaluar cada observación contra la spec y el plan actuales; cerrar las resueltas con referencias. |
| `checklists/domain.md` | Sigue preguntando por cuatro decisiones ya registradas. | Sustituir preguntas resueltas por las decisiones vigentes y conservar pendientes las verificaciones de implementación. |
| `checklists/ux.md` | Declara pendientes la política de timeout y la zona horaria visual. | Reflejar consulta del detalle sin reintento automático y presentación en `America/Lima`. |

La fila 16 de requisitos marca como fallo usar `America/Lima`. Esa zona ya es una política de Agenda y una decisión funcional confirmada: mencionarla no es, por sí sola, un detalle técnico innecesario. Tampoco es obligatorio copiar los códigos HTTP del contrato dentro de los RF para demostrar que un rechazo es coherente; se debe comprobar que el resultado funcional y su traducción HTTP sean compatibles.

Las 16 comprobaciones deben permanecer identificadas como **línea base propuesta** mientras el propietario no las confirme. El propio checklist indica que esa confirmación no bloquea por sí sola la feature. No confundir este estado con preguntas funcionales abiertas ni con pruebas de implementación pendientes.

Hay una precisión que conviene revisar: «cualquier fallo conserva la cita original» se refiere a una operación rechazada o una transacción fallida. Perder la respuesta tras un commit exitoso no deshace el cambio; por eso la política confirmada exige consultar el detalle tras timeout. Expresar esta distinción de forma coherente en los criterios y documentos relacionados.

#### Mensaje para corregir el bloqueo

Si estás conforme con el comportamiento descrito en la spec, puedes enviar este mensaje al asistente. La primera frase constituye tu aprobación explícita de esa especificación; no implica confirmar la línea base propuesta de 16 comprobaciones.

```text
Apruebo funcionalmente la spec specs/007-reprogramacion-citas/spec.md con las aclaraciones ya registradas: mismo médico, origen pasado permitido si la cita sigue pendiente, destino con inicio estrictamente futuro en America/Lima, motivo obligatorio de hasta 500 caracteres, mismo ID e historial completo con cambios múltiples, rechazo sin cambios ante conflictos u origen inconsistente, consulta del detalle tras timeout sin reintento automático y auditoría UTC presentada en hora de clínica.

Corrige únicamente los artefactos SDD de esta feature antes de implementar:
1. Registra mi aprobación explícita y actualiza el estado de spec.md.
2. Sincroniza checklists/requirements.md, domain.md y ux.md con spec.md, plan.md, data-model.md y contracts/rescheduling-contracts.md. Cierra preguntas resueltas con referencias y conserva los hallazgos que sigan siendo reales. No marques verificaciones de implementación o pruebas como realizadas.
3. Reevalúa las filas 2, 6, 7, 14 y 16 de requirements.md. America/Lima es una política de negocio vigente; no la clasifiques como defecto por sí sola. Comprueba la atomicidad y verificabilidad de los RF sin renumerarlos innecesariamente.
4. Distingue rechazo/rollback de pérdida de respuesta después de un commit: un timeout no demuestra que la operación haya fallado; la web consulta el detalle sin repetir la escritura. Haz coherente esa distinción en los artefactos y añade criterios explícitos para las aclaraciones si falta cobertura.
5. Revisa los defectos antiguos sobre códigos HTTP, fecha/hora y refresco web contra los documentos actuales. Cierra los ya resueltos sin duplicar contratos dentro de los RF; corrige cualquier discrepancia real que permanezca.
6. Mantén las 16 comprobaciones identificadas como línea base propuesta, sin afirmar que las he confirmado. Su falta de confirmación no debe bloquear por sí sola la feature.
7. Actualiza las tareas solo si estas correcciones afectan su cobertura; déjalas sin marcar. No modifiques src/ ni tests/ y no implementes todavía.
Informa los cambios documentales y los hallazgos pendientes.
```

Después de revisar la corrección, ejecuta de nuevo:

```text
/speckit-analyze specs/007-reprogramacion-citas
```

Solo si el nuevo resultado es `READY FOR IMPLEMENTATION`, continúa con el paso 7. Corregir los checklists no garantiza ese resultado: el análisis debe comprobar de nuevo el conjunto de documentos.

### Segundo bloqueo: faltan escenarios explícitos de aceptación

Después de registrar la aprobación y sincronizar los checklists, el análisis volvió a devolver `BLOCKED`. El hallazgo restante fue la falta de dos escenarios Dado/Cuando/Entonces: las reglas estaban aprobadas y contempladas en el diseño, pero no figuraban explícitamente en la tabla de aceptación.

| RF | Cobertura faltante |
| --- | --- |
| RF-REPRO-03 | Rechazo sin reparación ni cambios cuando el bloque de origen falta o no está `Reserved`. |
| RF-REPRO-06 | Consulta del detalle tras timeout, sin reintentar automáticamente la escritura. |

La aprobación de la spec no sustituye esos criterios verificables. Para una exposición, puede resolverse este hallazgo en una sola intervención: añadir ambos escenarios, enlazar su cobertura y repetir el análisis, sin recorrer de nuevo todas las etapas desde cero.

#### Prompt para resolver el segundo bloqueo

```text
Resuelve en una sola intervención los dos hallazgos pendientes de specs/007-reprogramacion-citas, manteniendo las reglas aprobadas.

Añade a la tabla de aceptación de spec.md estos escenarios:

1. RF-REPRO-03: Dada una cita pendiente cuyo bloque de origen no existe o no está Reserved, cuando recepción solicita reprogramarla, entonces el sistema rechaza la operación, reporta la inconsistencia y no modifica la cita, los bloques ni el historial, sin reparar datos automáticamente.

2. RF-REPRO-06: Dada una solicitud de reprogramación cuya respuesta se pierde o agota el tiempo de espera, cuando la interfaz detecta esa situación, entonces consulta nuevamente el detalle de la cita sin reenviar automáticamente la reprogramación y presenta el estado confirmado. Si la consulta también falla, informa que no pudo confirmar el resultado sin afirmar éxito ni fracaso de la operación.

Enlaza ambos escenarios con las tareas y verificaciones existentes de origen inconsistente y timeout. Amplía esas tareas solo si falta cobertura y sincroniza los checklists o contratos afectados. No marques pruebas como ejecutadas ni modifiques src/ o tests/.

Después ejecuta /speckit-analyze specs/007-reprogramacion-citas. Si los hallazgos quedaron resueltos y no hay otros bloqueos reales, devuelve READY FOR IMPLEMENTATION. No bloquees por preferencias de redacción ni por las 16 comprobaciones que siguen como línea base propuesta. Si existe otro bloqueo real, identifica el requisito y la contradicción exacta.
```

Durante la exposición, muestra el hallazgo, la corrección de los dos escenarios y el nuevo resultado del análisis como evidencia del control SDD. El tiempo limitado no convierte un resultado bloqueado en aprobado; el objetivo de este prompt es cerrar la cobertura concreta sin iteraciones documentales innecesarias.

## 7. Implementar y verificar: `/speckit-implement`

Ejecuta este comando únicamente cuando el último análisis sea `READY FOR IMPLEMENTATION`:

```text
/speckit-implement specs/007-reprogramacion-citas. Implementa las tareas pendientes respaldadas por los requisitos aprobados. Incluye pruebas de aceptación y concurrencia previstas en los artefactos. Ejecuta dotnet build y dotnet test desde la raíz. Marca tareas completas únicamente cuando ambas comprobaciones pasen e informa tareas pendientes, errores y comprobaciones que no se pudieron ejecutar. Actualiza la documentación vigente afectada. No hagas commit ni publiques cambios.
```

Este es el único comando del flujo autorizado a cambiar código en `src/` y `tests/`.

Si las pruebas SQL requieren una base dedicada, documenta el requisito y verifica su disponibilidad. No presentes pruebas omitidas o una dependencia no disponible como validación satisfactoria de SQL.

Además de las comprobaciones automatizadas, verifica el flujo web definido: localizar cita, elegir destino, revisar y confirmar, comprobar el nuevo horario y comprobar que el anterior vuelve a estar disponible. Ante un conflicto, debe verse un error comprensible y conservarse la cita original conforme a la spec.

## 8. Cerrar la entrega

Revisa que los RF estén cubiertos, las tareas completadas tengan evidencia y los resultados de compilación y pruebas estén informados. Actualiza `specs/README.md`, el resumen de Citas, Agenda y cualquier otra documentación que haya quedado incorrecta con el cambio.

La actualización del catálogo debe reflejar el estado real: borrador, diseñada o implementada según corresponda. Crear documentación no equivale a terminar la feature.

## Secuencia rápida

Ejecuta un comando por vez y revisa su resultado antes del siguiente:

```text
/speckit-specify [necesidad y alcance de la reprogramación]
/speckit-plan specs/007-reprogramacion-citas
/speckit-checklist specs/007-reprogramacion-citas
/speckit-tasks specs/007-reprogramacion-citas
/speckit-analyze specs/007-reprogramacion-citas
/speckit-implement specs/007-reprogramacion-citas
```

Las aclaraciones se resuelven durante `specify` en este repositorio. No se necesita volver a crear la constitución para esta feature; se aplica la existente en `.specify/memory/constitution.md`.
