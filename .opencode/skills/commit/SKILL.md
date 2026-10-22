---
name: commit
description: Revisa cambios preparados en Git, crea un mensaje conforme al manual de commits de la empresa y realiza el commit local solo con autorización. No publica ni crea releases.
metadata:
  opencode/slash: "true"
---

# /commit

## Alcance y autorización

Por defecto, trabaja únicamente con los cambios que ya están en el índice de Git. Genera el mensaje y solicita confirmación antes de crear el commit, salvo que el usuario ya haya autorizado explícitamente crear el commit de esos cambios. Pedir solo un mensaje no autoriza el commit. Publicar, enmendar commits, crear releases o cambiar ramas está fuera de este flujo.

Si el usuario autoriza incluir archivos nuevos o cambios no preparados, inspecciónalos primero y prepara únicamente las rutas o fragmentos autorizados. No uses preparación general ni `git commit -am`. Conserva los cambios preparados y no preparados que no pertenezcan al alcance; si el índice mezcla cambios ajenos, pregunta cómo separarlos sin reiniciar ni alterar el índice del usuario.

## Flujo

1. Localiza el repositorio con `git rev-parse --show-toplevel`. Lee las instrucciones aplicables y el manual `MANUAL_GIT_COMMIT_v1.1` si está disponible. Revisa `git status --short`, la rama actual y si hay un merge, rebase, cherry-pick o revert en curso. Si una operación está activa o HEAD está separado, informa el estado y resuelve qué hacer antes de crear el commit.
2. Inspecciona `git diff --cached --stat`, `git diff --cached --name-status` y `git diff --cached`. Si no hay cambios preparados, informa y detente, salvo que el usuario haya pedido prepararlos. Revisa si hay cambios ajenos o credenciales preparadas por accidente; no muestres valores secretos. Detente ante un riesgo concreto e identifica solo las rutas afectadas.
3. Clasifica el cambio según el diff real; un archivo nuevo no implica automáticamente una funcionalidad nueva. Usa exactamente uno de los tipos aprobados: `feat`, `fix`, `improve`, `refactor`, `style`, `perf`, `docs`, `test`, `chore`, `build`, `ci` o `revert`.
   - Formato de la primera línea: `<tipo>(<alcance>): <descripción de acción> #<ticket>`. El alcance es obligatorio e identifica el módulo o componente afectado. Si no puedes determinarlo a partir del cambio, pregunta; no lo omitas ni lo inventes.
   - `docs` puede omitir ` #<ticket>` únicamente si el cambio preparado es exclusivamente documentación. Para cualquier otro tipo, exige un ID real de tarea de Azure DevOps: entero positivo sin ceros iniciales. Nunca inventes ni adivines el ticket. Si Azure DevOps está conectado, verifica que exista; si no, usa solo un ID proporcionado por el usuario e indica que no se pudo verificar independientemente. Si falta, pregunta y no crees el commit todavía.
   - La descripción comienza en minúscula, usa un verbo de acción y no termina en punto. La primera línea completa, incluidos tipo, alcance y ticket, no debe superar 72 caracteres. Acorta la descripción sin quitar campos obligatorios; si aún no cabe, pide un alcance más breve o una decisión al usuario.
   - No uses `wip`. No uses `!` para indicar cambios incompatibles. En su lugar, añade un footer `BREAKING CHANGE:` que explique la incompatibilidad y la adaptación necesaria. No añadas `Co-Authored-By`.
4. Muestra las rutas incluidas y el mensaje completo. Si todavía falta autorización para crear el commit, solicítala una vez. No vuelvas a pedir una aprobación ya dada para el mismo alcance. Antes de ejecutar el commit, compara HEAD y el diff preparado con lo que revisaste; si cambiaron, vuelve a revisar.
5. Guarda el mensaje en un archivo temporal exacto y ejecuta `git commit -F <archivo-mensaje>`. Ubica el temporal mediante `git rev-parse --git-path` o el directorio temporal del sistema; `.git` puede ser un archivo en un worktree. Usa herramientas de escritura de archivos o comillas seguras; nunca interpoles mensajes no confiables como código de shell. No desactives hooks, firma ni comprobaciones configuradas. Si falla, informa la causa y conserva el estado del índice; no reintentes a ciegas ni crees un commit vacío. Si un hook cambia el diff, solicita una nueva revisión.
6. Verifica el SHA, el mensaje y las rutas del commit. Informa qué cambios siguen en el árbol de trabajo. Elimina solo el archivo temporal creado por esta ejecución. No hagas push.

## Captura guiada del mensaje

Después de revisar el diff, prepara una propuesta con los datos que puedas deducir con seguridad. No hagas que el usuario vuelva a escribir información que ya aparece claramente en el cambio. Si falta algún dato obligatorio o la clasificación es ambigua, recógelo en una sola interacción guiada usando la herramienta `question` de OpenCode cuando esté disponible:

- **Tipo:** propone el tipo que mejor describa el cambio y presenta los tipos aprobados como opciones. Si no hay evidencia suficiente, deja que el usuario elija.
- **Alcance:** sugiere el módulo inferido de las rutas/diff y permite que el usuario lo corrija o escriba otro.
- **Descripción breve:** ofrece una frase de acción en minúscula, sin punto final y que quepa en el límite total de 72 caracteres.
- **Ticket:** solicita el ID real de Azure DevOps. Permite dejarlo vacío únicamente si el cambio es exclusivamente `docs`.
- **Cuerpo:** ofrece incluir una explicación breve generada a partir del diff, escribir/editar una propia o no incluir cuerpo. Recomienda cuerpo solo si ayuda a explicar el qué y el porqué; no pidas que el usuario lo redacte desde cero. Cuando ayude a entender el cambio, añade al cuerpo una sección breve `archivos:` con las rutas principales modificadas. No enumeres todos los archivos mecánicamente: si son muchos, agrúpalos por módulo o incluye solo los más relevantes. Las rutas nunca van en el footer.
- **Footer:** por defecto, ninguno. Solo solicita detalles si hay metadatos necesarios o un cambio incompatible; en ese caso prepara `BREAKING CHANGE: <impacto y adaptación requerida>` y permite que el usuario lo corrija. No uses el footer para listar archivos.

Muestra las sugerencias como valores iniciales confirmables/editables, no como decisiones ya autorizadas. Si la herramienta no está disponible o no permite ese formato, pregunta una sola vez en el chat con los campos que realmente falten. No bloquees por no tener una interfaz gráfica.

Una vez recibidas las respuestas, valida tipo, alcance, sintaxis del ticket y longitud de la primera línea; genera el mensaje completo y muéstralo para revisión. La respuesta al formulario no autoriza por sí sola crear el commit: conserva la solicitud de confirmación del paso 4, salvo autorización explícita previa para ese commit.

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
