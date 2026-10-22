---
name: commit
description: Revisa cambios, guía su preparación y crea un commit conforme al manual de la empresa; ofrece publicar la rama con autorización explícita.
metadata:
  opencode/slash: "true"
---

# /commit

## Alcance y autorización

El flujo puede revisar cambios sin preparar y guiar al usuario para seleccionarlos. Pedir solo un mensaje no autoriza preparar archivos, crear un commit ni hacer push. El push es opcional y requiere una autorización separada después de crear y verificar el commit. Enmendar commits, crear releases o cambiar ramas está fuera de este flujo.

Si el usuario autoriza incluir archivos nuevos o cambios no preparados, inspecciónalos primero y prepara únicamente las rutas o fragmentos autorizados. No uses preparación general ni `git commit -am`. Conserva los cambios preparados y no preparados que no pertenezcan al alcance; si el índice mezcla cambios ajenos, pregunta cómo separarlos sin reiniciar ni alterar el índice del usuario.

## Flujo

1. Localiza el repositorio con `git rev-parse --show-toplevel`. Lee las instrucciones aplicables y el manual `MANUAL_GIT_COMMIT_v1.1` si está disponible. Ejecuta `git status --short --untracked-files=all` y muestra al usuario las rutas y su estado (preparadas, modificadas sin preparar, eliminadas o nuevas). Revisa también la rama actual y si hay un merge, rebase, cherry-pick o revert en curso. Si una operación está activa o HEAD está separado, informa y detente antes de preparar o confirmar cambios.
2. Antes de cambiar el índice, muestra las rutas detectadas y aclara que todo lo que ya está preparado formará parte del commit. Pregunta qué hacer mediante `question` de OpenCode cuando esté disponible: (a) usar lo preparado y añadir todas las otras rutas mostradas, (b) usar lo preparado y añadir solo rutas concretas, (c) usar únicamente lo que ya está preparado o (d) preparar manualmente y volver a ejecutar `/commit`. Si el usuario quiere excluir rutas que ya estaban preparadas, no las retires del índice sin autorización explícita. No incluyas rutas ignoradas ni archivos que no aparezcan en el estado mostrado. Al preparar la opción elegida, usa `git add -- <rutas-explícitas>`; nunca uses `git add .` ni `git add -A`. Si el usuario elige preparación manual, detente y pídele volver a ejecutar `/commit` después de preparar los archivos.
3. Inspecciona el alcance seleccionado con `git diff --cached --stat`, `git diff --cached --name-status` y `git diff --cached`; revisa también el diff no preparado para detectar cambios ajenos que no deban entrar. Inspecciona archivos nuevos seleccionados. Revisa si hay credenciales o datos sensibles; no muestres sus valores. Si detectas algo ajeno, secreto o preocupante, detente y aclara solo las rutas afectadas.
4. Clasifica el cambio según el diff real; un archivo nuevo no implica automáticamente una funcionalidad nueva. Usa exactamente uno de los tipos aprobados: `feat`, `fix`, `improve`, `refactor`, `style`, `perf`, `docs`, `test`, `chore`, `build`, `ci` o `revert`.
   - Formato de la primera línea: `<tipo>(<alcance>): <descripción de acción> #<ticket>`. El alcance es obligatorio e identifica el módulo o componente afectado. Si no puedes determinarlo a partir del cambio, pregunta; no lo omitas ni lo inventes.
   - `docs` puede omitir ` #<ticket>` únicamente si el cambio preparado es exclusivamente documentación. Para cualquier otro tipo, exige un ID real de tarea de Azure DevOps: entero positivo sin ceros iniciales. Nunca inventes ni adivines el ticket. Si Azure DevOps está conectado, verifica que exista; si no, usa solo un ID proporcionado por el usuario e indica que no se pudo verificar independientemente. Si falta, pregunta y no crees el commit todavía.
   - La descripción comienza en minúscula, usa un verbo de acción y no termina en punto. La primera línea completa, incluidos tipo, alcance y ticket, no debe superar 72 caracteres. Acorta la descripción sin quitar campos obligatorios; si aún no cabe, pide un alcance más breve o una decisión al usuario.
   - No uses `wip`. No uses `!` para indicar cambios incompatibles. En su lugar, añade un footer `BREAKING CHANGE:` que explique la incompatibilidad y la adaptación necesaria. No añadas `Co-Authored-By`.
5. Muestra las rutas incluidas y el mensaje completo (asunto, cuerpo y footer, si existen). Si falta autorización para crear el commit, solicítala una vez. No vuelvas a pedir una aprobación ya dada para el mismo alcance. Antes de ejecutar el commit, compara HEAD y el diff preparado con lo que revisaste; si cambiaron, vuelve a revisar.
6. Guarda el mensaje en un archivo temporal exacto y ejecuta `git commit -F <archivo-mensaje>`. Ubica el temporal mediante `git rev-parse --git-path` o el directorio temporal del sistema; `.git` puede ser un archivo en un worktree. Usa herramientas de escritura de archivos o comillas seguras; nunca interpoles mensajes no confiables como código de shell. No desactives hooks, firma ni comprobaciones configuradas. Si falla, informa la causa y conserva el estado del índice; no reintentes a ciegas ni crees un commit vacío. Si un hook cambia el diff, solicita una nueva revisión.
7. Verifica el SHA, el mensaje y las rutas del commit. Informa qué cambios siguen en el árbol de trabajo. Elimina solo el archivo temporal creado por esta ejecución. Luego revisa la rama actual y confirma que existe el remoto `origin`. Actualiza referencias de `origin` sin podar ni forzar. Determina qué commits se publicarían en esa rama (incluidos los commits locales anteriores que el push incluiría). Si la rama remota no existe, indícalo y muestra los commits que se publicarían al crearla. Si no se puede consultar el remoto o la rama remota diverge de la local, no intentes publicar. Muestra la rama exacta, el comando `git push origin <rama>` y la lista/resumen de commits que subirían; pregunta si autoriza ejecutar ese push. No hagas push si responde que no, no responde, cambia de tema o la inspección remota no permite determinar un destino seguro.
8. Solo tras una respuesta afirmativa explícita, vuelve a comprobar que sigues en la misma rama y que el destino `origin/<rama>` no ha cambiado de forma que requiera integración. Ejecuta únicamente `git push origin <rama>`; nunca uses force, `--all`, `--tags` ni cambies el nombre del remoto. Si el push se rechaza, detente, conserva el estado y explica el siguiente paso; no fuerces ni reintentes a ciegas. Verifica el resultado remoto y reporta SHA publicado. Si `origin` no existe, HEAD está separado o la rama remota diverge, no intentes otro destino o estrategia sin pedir autorización.

## Captura guiada del mensaje

Después de revisar el diff, prepara una propuesta con los datos que puedas deducir con seguridad. No hagas que el usuario vuelva a escribir información que ya aparece claramente en el cambio. Si falta algún dato obligatorio o la clasificación es ambigua, recógelo en una sola interacción guiada usando la herramienta `question` de OpenCode cuando esté disponible:

- **Tipo:** propone el tipo que mejor describa el cambio y presenta los tipos aprobados como opciones. Si no hay evidencia suficiente, deja que el usuario elija.
- **Alcance:** sugiere el módulo inferido de las rutas/diff y permite que el usuario lo corrija o escriba otro.
- **Descripción breve:** ofrece una frase de acción en minúscula, sin punto final y que quepa en el límite total de 72 caracteres.
- **Ticket:** solicita el ID real de Azure DevOps. Permite dejarlo vacío únicamente si el cambio es exclusivamente `docs`.
- **Cuerpo:** ofrece incluir una explicación breve generada a partir del diff, escribir/editar una propia o no incluir cuerpo. Recomienda cuerpo solo si ayuda a explicar el qué y el porqué; no pidas que el usuario lo redacte desde cero. Cuando ayude a entender el cambio, añade al cuerpo una sección breve `archivos:` con las rutas principales modificadas. No enumeres todos los archivos mecánicamente: si son muchos, agrúpalos por módulo o incluye solo los más relevantes. Las rutas nunca van en el footer.
- **Footer:** por defecto, ninguno. Solo solicita detalles si hay metadatos necesarios o un cambio incompatible; en ese caso prepara `BREAKING CHANGE: <impacto y adaptación requerida>` y permite que el usuario lo corrija. No uses el footer para listar archivos.

Muestra las sugerencias como valores iniciales confirmables/editables, no como decisiones ya autorizadas. Si la herramienta no está disponible o no permite ese formato, pregunta una sola vez en el chat con los campos que realmente falten. No bloquees por no tener una interfaz gráfica.

Una vez recibidas las respuestas, valida tipo, alcance, sintaxis del ticket y longitud de la primera línea; genera el mensaje completo y muéstralo para revisión. La respuesta al formulario no autoriza por sí sola crear el commit: conserva la solicitud de confirmación del paso 5, salvo autorización explícita previa para ese commit. La aprobación del commit tampoco autoriza el push: pide la autorización de publicación por separado después de verificar el commit.

## Idioma y cuerpo

El manual de la empresa prevalece sobre las convenciones genéricas. No fija el idioma del mensaje: usa el idioma habitual del equipo o el que solicite el usuario, siempre respetando la estructura y las reglas anteriores. Cuando aporte claridad, añade un cuerpo que explique qué cambió y por qué, separado de la primera línea por una línea en blanco. Si las rutas ayudan a revisar el cambio, inclúyelas como una lista breve dentro del cuerpo, no en el footer. El footer es opcional y se separa del cuerpo por otra línea en blanco. El ticket principal debe permanecer al final de la primera línea, nunca trasladarse al footer.

Si hay cambios independientes, recomienda commits separados. No crees commits intermedios `wip`; antes de integrar, los commits intermedios de un mismo cambio deben agruparse mediante squash y conservar un mensaje final conforme al manual.

Ejemplo (usar siempre un ticket real):

```text
feat(ui): agregar revelado al desplazarse #123

mantiene visible la vista previa mientras se recorre la página.

archivos:
- src/scripts/site.ts: agrega initStickyReveal con soporte para movimiento reducido.
```
