# Guía de demostración SDD: reprogramación de citas

## Propósito y alcance

Esta guía permite demostrar el flujo SDD del proyecto con la feature `007-reprogramacion-citas`, desde la necesidad hasta la implementación. Haz la prueba directamente en la copia didáctica y en la rama que ya está activa. **No crees ni cambies de rama.** No hagas commit, push, release ni despliegue. Antes de ejecutar cada comando, comprueba si el artefacto ya existe y usa el modo indicado en cada paso.

La demostración tiene seis etapas SDD: `specify`, `plan`, `checklist`, `tasks`, `analyze` e `implement`. La preparación y el cierre son pasos operativos adicionales, no etapas SDD.

> No ejecutes comandos slash en PowerShell. Escríbelos en el chat del asistente que tenga abierto este repositorio y haya cargado sus comandos. Los comandos están definidos en `.opencode/commands/` y sus instrucciones detalladas en `.claude/skills/speckit-*/SKILL.md`.

## Preparación: elegir correctamente el estado inicial

1. Abre la **copia didáctica sin implementación 007**, no el checkout donde la feature ya está implementada.
2. Comprueba que el asistente está en la raíz del repositorio y revisa `git status --short --branch`.
3. Confirma que no hay cambios de otra persona que puedan mezclarse con la demostración. No limpies ni descartes cambios automáticamente.
4. Revisa `specs/README.md` y `specs/007-reprogramacion-citas/`.
5. Comprueba en `src/` y `tests/` que esa copia no contenga la feature 007. La presencia de la spec no significa que la implementación exista.
6. Conserva la rama actual durante toda la prueba. No crees ni cambies ramas. No hace falta iniciar un flujo Git para demostrar Spec Kit.

### Dos formas de presentar la demo

- **Recorrido de resultados ya preparados (recomendado para una exposición corta):** inspecciona los artefactos que ya existen, explica qué produce cada etapa y ejecuta `analyze` para mostrar la puerta de calidad. No repitas comandos generadores sobre archivos ya preparados.
- **Ejecución de extremo a extremo:** usa una copia didáctica preparada para iniciar la 007 sin sus artefactos SDD. Ejecuta los seis comandos, en orden, revisando cada resultado antes de continuar. Si un artefacto existe, no lo sobrescribas a ciegas: usa el recorrido de resultados o crea una copia limpia de demostración.

En el estado documentado actualmente, `spec.md`, `plan.md`, `data-model.md`, contratos, checklists y `tasks.md` ya existen. `tasks.md` tiene las tareas sin marcar y el catálogo describe la feature como pendiente. Por lo tanto, en ese estado corresponde el **recorrido de resultados preparados**, no afirmar que se crea todo desde cero.

## Feature de la demostración

- **Nombre:** `007-reprogramacion-citas`.
- **Módulo propietario:** Citas.
- **Módulos afectados:** Agenda, Atenciones y experiencia web.
- **Rama:** la prueba se ejecuta sobre la rama que ya está activa. No crear ni cambiar de rama. Si la spec contiene un nombre de rama propuesto, trátalo como metadato descriptivo y no como una instrucción Git.
- **Necesidad:** permitir que recepción reprograme una cita pendiente desde la web, conservando paciente, motivo de consulta e ID; reservar el destino y liberar el origen de forma atómica; evitar conflictos y registrar el historial.

Las reglas de negocio aprobadas están en `specs/007-reprogramacion-citas/spec.md`. No uses respuestas históricas como si fueran preguntas abiertas ni modifiques reglas aprobadas para hacer la demo más sencilla.

## Etapa 1 — Definir el comportamiento: `/speckit-specify`

**Objetivo:** establecer qué debe hacer la feature y cómo se comprobará. Solo crea o modifica `spec.md`.

Si la spec no existe en tu copia limpia, pega este prompt:

```text
/speckit-specify Crear la feature 007-reprogramacion-citas para permitir que recepción cambie el horario de una cita pendiente desde la web. Conservar el paciente y el motivo de consulta. El horario destino debe estar disponible y ser futuro según la zona horaria de la clínica. Liberar el bloque anterior y reservar el nuevo como una sola operación: si el cambio falla, conservar intacta la cita original. Rechazar la reprogramación de citas atendidas o canceladas y evitar reservas duplicadas ante solicitudes concurrentes. Quiero trazabilidad del cambio de horario y de su motivo. Revisa las specs y el código existentes, pregunta por las decisiones de negocio pendientes y crea únicamente el spec.md de esta etapa. Usa IDs de requisitos que no colisionen con los RF-CIT existentes. No propongas ni crees una rama Git; registra que la demostración se ejecuta en la rama actual.

Usa estas decisiones para responder las aclaraciones de negocio y regístralas en `spec.md`: conserva el mismo médico; permite reprogramar una cita pendiente aunque el horario original haya pasado si el destino es futuro; exige motivo de cambio de texto libre de hasta 500 caracteres; conserva el ID y muestra el historial completo; registra el nombre de recepción enviado en la solicitud; ante cita no pendiente responde con `APPOINTMENT_NOT_PENDING`; si el horario original ya pasó, permite reprogramar mientras la cita siga pendiente y el destino sea futuro; muestra el historial en el detalle de la cita, ordenado del cambio más reciente al más antiguo; rechaza conflictos sin cambiar cita ni reserva; ante timeout consulta el detalle sin reenviar la operación; guarda el timestamp en UTC y lo muestra en `America/Lima`. Si el asistente pregunta algo distinto que cambie el comportamiento, no inventes una respuesta: consúltala.
```

**Antes de avanzar:** revisa que la spec mantenga la solicitud original, identifique módulo propietario y afectados, tenga alcance/fuera de alcance, RF con IDs estables y criterios Dado/Cuando/Entonces. Resuelve con el usuario cualquier decisión que cambie el comportamiento; no infieras respuestas. La spec debe seguir en borrador mientras queden aclaraciones funcionales abiertas.

Las aclaraciones funcionales ya registradas en `spec.md` incluyen: mismo médico; permitir origen pasado si la cita sigue pendiente; destino con inicio estrictamente futuro en `America/Lima`; motivo obligatorio de texto libre, máximo 500 caracteres; mismo ID e historial completo visible, con cambios múltiples; rechazo sin mutación ante conflictos; tras timeout consultar el detalle sin reenviar la escritura; timestamp guardado UTC y presentado en hora clínica. Las respuestas adicionales de esta sesión de `specify` —operador registrado en la solicitud, historial en el detalle y orden más reciente primero— deben incorporarse a la spec y a sus artefactos dependientes antes del análisis final.

En la sesión mostrada durante esta demo, `specify` también preguntó por el operador, el error de cita no pendiente, el horario original pasado, la ubicación del historial y su orden. Las respuestas elegidas fueron: registrar el nombre de recepción enviado en la solicitud; usar `APPOINTMENT_NOT_PENDING`; permitir el cambio si la cita sigue pendiente aunque el horario original haya pasado, siempre que el destino sea futuro; mostrar el historial en el detalle de la cita; y ordenar el historial con el cambio más reciente primero.

Estas dos últimas decisiones de historial y la captura del operador amplían o precisan la spec aprobada. Regístralas como aclaraciones explícitas en `spec.md` y sincroniza los escenarios de aceptación, `data-model.md`, `plan.md`, contratos, checklists y tareas antes de volver a ejecutar `analyze`. En particular, el modelo y el contrato actuales dicen orden ascendente (más antiguo primero), lo que contradice la respuesta elegida: cámbialos a orden descendente por `ChangedAt` y define un desempate estable por ID. Actualiza también consultas, interfaz y pruebas planificadas para el orden nuevo. No presentes la decisión nueva como implementada hasta que el código y las pruebas lo demuestren. Mantén el límite de 500 caracteres ya aprobado para el motivo.

**Salida esperada:** `specs/007-reprogramacion-citas/spec.md`.

## Etapa 2 — Diseñar la solución: `/speckit-plan`

**Precondición:** spec revisada y sin decisiones funcionales abiertas.

```text
/speckit-plan specs/007-reprogramacion-citas. Diseña la solución conforme al spec.md aprobado y la constitución del proyecto. Revisa el modelo de citas y bloques, repositorios, persistencia SQL Server y la web existentes. Define cómo proteger la operación frente a fallos y concurrencia, incluyendo carreras con reserva, cancelación, atención y otra reprogramación. Documenta arquitectura, modelo de datos, impacto de migraciones, contratos HTTP/JSON y de repositorio, errores y estrategia de pruebas. Reutiliza los contratos compartidos por referencia. No modifiques src/ ni tests/.
```

**Revisa las salidas:** `plan.md`, `data-model.md` y `contracts/` cuando cambien interfaces. Comprueba que el diseño no introduzca comportamiento que no esté aprobado por la spec y que siga la constitución. Si el diseño revela una decisión funcional pendiente, vuelve a `specify` para resolverla antes de continuar.

## Etapa 3 — Revisar calidad: `/speckit-checklist`

**Precondición:** existen spec y artefactos de plan.

```text
/speckit-checklist specs/007-reprogramacion-citas. Audita la calidad de los requisitos y crea checklists de dominio y UX centrados en reprogramación, integridad de la reserva, concurrencia, confirmación del cambio y mensajes de error. Vincula cada observación con RF y escenarios. Distingue decisiones de negocio pendientes, defectos documentales y verificaciones que solo podrán hacerse durante implementación. Mantén identificadas como propuestas las 16 comprobaciones formales mientras no hayan sido confirmadas por el propietario del proyecto. No modifiques src/ ni tests/.
```

**Revisa las salidas:** `checklists/requirements.md` y, si corresponde, `domain.md` y `ux.md`. La lista formal de 16 puntos es una línea base propuesta en las instrucciones locales; no la presentes como una norma externa ya aprobada. Un checklist no demuestra que la implementación funcione.

## Etapa 4 — Desglosar el trabajo: `/speckit-tasks`

**Precondición:** spec, plan y checklists coherentes; no hay decisiones de negocio abiertas.

```text
/speckit-tasks specs/007-reprogramacion-citas. Genera tareas atómicas y ordenadas por dependencia para dominio, aplicación, persistencia, API, interfaz web y pruebas según el diseño aprobado. Vincula cada tarea con sus RF y criterios de aceptación. Incluye migraciones si corresponden, actualización de documentación vigente y verificación final con dotnet build y dotnet test. Incluye pruebas de atomicidad y concurrencia reales de SQL Server según el plan. Deja todas las tareas sin marcar y no modifiques src/ ni tests/.
```

**Revisa la salida:** `tasks.md`. Cada RF debe tener tareas de implementación y pruebas; las tareas deben estar sin marcar porque todavía no se han realizado.

## Etapa 5 — Validar coherencia: `/speckit-analyze`

**Precondición:** están presentes todos los artefactos requeridos. Este paso se puede repetir después de corregir hallazgos.

```text
/speckit-analyze specs/007-reprogramacion-citas. Contrasta spec, plan, modelo de datos, contratos, checklists y tareas con la constitución, los módulos afectados y el código existente. Comprueba que toda decisión funcional esté resuelta; cada RF tenga aceptación, diseño/contrato compatible, tarea y pruebas planificadas; y que estados, errores, términos, persistencia y concurrencia sean coherentes. Devuelve BLOCKED con hallazgos concretos y artefacto/RF que debe corregirse, o READY FOR IMPLEMENTATION con notas no bloqueantes. No modifiques src/ ni tests/.
```

**Si devuelve `BLOCKED`:** no implementes. Corrige solo los artefactos indicados, actualiza sus dependientes y vuelve a ejecutar este análisis. No conviertas preferencias de estilo en bloqueos ni marques como hecho algo que aún no se verificó.

**Si devuelve `READY FOR IMPLEMENTATION`:** guarda el resultado en un artefacto de análisis dentro de `specs/007-reprogramacion-citas/` (por ejemplo `analyze.md`) o conserva la salida de forma visible para la demo. Registra la fecha/revisión analizada. Continúa solo si el análisis corresponde a los artefactos actuales y no se cambió después el comportamiento o diseño.

## Etapa 6 — Implementar y verificar: `/speckit-implement`

**Puerta obligatoria:** el último análisis debe ser `READY FOR IMPLEMENTATION`. Si no, detente en la etapa anterior.

```text
/speckit-implement specs/007-reprogramacion-citas. Implementa las tareas pendientes respaldadas por los requisitos aprobados y el último análisis READY FOR IMPLEMENTATION. Incluye las pruebas de aceptación y concurrencia previstas. Ejecuta dotnet build y dotnet test desde la raíz. Marca una tarea completa únicamente cuando la implementación y sus pruebas relevantes pasen; informa tareas pendientes, resultados y comprobaciones que no pudiste ejecutar. Actualiza la documentación vigente que el cambio vuelva incorrecta. No hagas commit ni publiques cambios.
```

Durante la implementación, no presentes una prueba SQL omitida por falta de base dedicada como exitosa. Verifica además el recorrido web de búsqueda, selección de destino, revisión, confirmación, actualización de disponibilidad e historial; comprueba también el caso de conflicto.

## Cierre de la demostración

1. Resume qué artefactos se crearon o corrigieron en cada etapa.
2. Indica el resultado de análisis y qué revisión/estado se analizó.
3. Si se implementó, reporta el resultado real de `dotnet build` y `dotnet test`, las tareas completadas y pendientes, y cualquier prueba SQL bloqueada.
4. Actualiza `specs/README.md`, README y resúmenes de módulo solo donde su descripción haya quedado desactualizada. Describe el estado real del checkout actual.
5. No crees ni cambies de rama. No hagas commit, push, release ni despliegue durante esta prueba.

## Anexo — Incidencias históricas útiles para explicar `analyze`

En la preparación de esta feature hubo dos ciclos de análisis bloqueado. Úsalos como ejemplos didácticos, no como estado necesariamente vigente:

1. **Primer bloqueo documental:** checklists desactualizados respecto de decisiones aprobadas, errores de clasificación de `America/Lima`, y necesidad de distinguir rollback de una respuesta perdida después de un commit. La corrección fue sincronizar los artefactos y añadir cobertura verificable sin dar por confirmada la línea base de 16 puntos.
2. **Segundo bloqueo de aceptación:** faltaban escenarios explícitos para rechazar un bloque de origen inconsistente y para consultar el detalle tras timeout sin reenviar la escritura. Se añadieron escenarios vinculados a RF-REPRO-03 y RF-REPRO-06 y se enlazaron con tareas/checklists.

Los prompts exactos de esos arreglos se conservan en el historial Git de esta guía. Para una exposición normal basta mostrar el hallazgo, el artefacto corregido, el nuevo análisis y la razón por la que no se avanzó a implementar antes de tener una puerta favorable.

## Secuencia rápida

Para una ejecución completa sobre una copia de demo preparada desde cero, ejecutar **un comando por vez en la rama actual**, revisar la salida y respetar sus precondiciones. No crees una rama para la prueba:

```text
/speckit-specify [necesidad y alcance de la reprogramación]
/speckit-plan specs/007-reprogramacion-citas
/speckit-checklist specs/007-reprogramacion-citas
/speckit-tasks specs/007-reprogramacion-citas
/speckit-analyze specs/007-reprogramacion-citas
/speckit-implement specs/007-reprogramacion-citas
```

Si la copia ya contiene los artefactos documentales, no pegues la secuencia rápida completa: empieza revisando el estado preparado y ejecuta `analyze`. La constitución vigente está en `.specify/memory/constitution.md`; no hay que regenerarla para esta feature.
