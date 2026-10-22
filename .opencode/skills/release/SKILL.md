---
name: release
description: Automatiza un release Git Flow configurado con changelog, merges locales, etiqueta anotada y publicación remota limitada, siguiendo el manual de commits de la empresa. Puede crear un GitHub Release si se solicita. Se detiene ante conflictos o estados ambiguos.
metadata:
  opencode/slash: "true"
---

# /release

## Alcance y autorización

Automatiza la preparación local, validación y publicación del release. Una invocación sin argumentos prepara el release y pide confirmación una vez antes del primer push. Si el usuario solicita explícitamente publicar el release identificado, esa petición ya autoriza la publicación: muestra el resumen final y continúa sin pedir la misma aprobación otra vez. Indicar solo una versión no autoriza publicar. `prepare-only` termina con commits y etiqueta locales. Trata los argumentos como instrucciones para el agente, no como opciones de Git.

Las ramas/etiquetas Git y los GitHub Releases son resultados distintos. Por defecto, publica únicamente las ramas previstas y la etiqueta anotada. Crea un GitHub Release solo si se pide mediante `github-release` o una instrucción equivalente. No crees PR, envíes mensajes al equipo, despliegues, borres ramas remotas ni eludas protecciones como parte implícita del release. Si la protección requiere un PR que no se solicitó, informa ese siguiente paso.

## 1. Revisar repositorio y política

- Localiza el repositorio y lee las instrucciones aplicables y `git-guide.txt`, si existe. Que falte esa guía no bloquea por sí solo si la configuración restante es suficiente.
- Exige índice y árbol de trabajo limpios, incluidos archivos sin seguimiento, y que no haya merge/rebase/cherry-pick/revert activo. No guardes cambios automáticamente en stash, no confirmes cambios ajenos ni reinicies el checkout.
- Lee la configuración de Git Flow: rama de producción (`gitflow.branch.master`), integración (`gitflow.branch.develop`), prefijos de feature/release y prefijo de etiquetas de versión. Resuelve los nombres y úsalos en todo el flujo; no fijes `main`, `develop` o `release/` si la configuración define otros. Si las ramas necesarias son ambiguas, pregunta una vez. Los comandos nativos de Git bastan; el ejecutable git-flow es opcional.
- Resuelve el remoto según la petición, guía o upstream de la rama. Usa `origin` solo si es la opción configurada inequívoca. Inspecciona destinos de fetch y push sin mostrar credenciales incrustadas. No deduzcas que es GitHub por el nombre del remoto. Para publicar un GitHub Release, verifica host y repositorio GitHub/GitHub Enterprise, autenticación y disponibilidad de la herramienta de publicación antes de empezar.
- Actualiza referencias remotas pertinentes y etiquetas sin forzar su reemplazo. Registra los SHA de las ramas remotas y distingue rama inexistente de error de red/autenticación. Si falla fetch, no uses referencias locales obsoletas. Un release deliberadamente local solo puede continuar en modo `prepare-only`.
- Revisa archivos de versión, lockfiles, changelog, convenciones y comandos de verificación requeridos. Sigue la herramienta de actualización existente del proyecto; no asumas que `package.json` es la única fuente de versión.
- Lee el manual `MANUAL_GIT_COMMIT_v1.1` cuando esté disponible y aplícalo a cada commit creado por este flujo, incluidos commits de integración y merge: tipo aprobado, alcance obligatorio, ticket real de Azure DevOps en la primera línea salvo documentación exclusiva y máximo de 72 caracteres para la línea completa. El manual indica que la numeración decimal de la aplicación se gestiona por separado durante el despliegue; por tanto, no modifiques archivos de versión de la aplicación como paso predeterminado. Hazlo solo si la política de release/despliegue del repositorio lo exige explícitamente y el usuario autoriza ese alcance. Una etiqueta o versión de release no autoriza por sí sola a editar esos archivos.
- Resuelve el ID real de Azure DevOps correspondiente al release/integración antes de crear cualquier commit. Nunca fabriques un ticket. Si Azure DevOps está conectado, verifica que exista; si no, usa solo un ID proporcionado por el usuario e informa que no se verificó independientemente. Si el flujo requiere varios commits independientes, usa para cada uno el ticket que le corresponda según el equipo. Si el ticket falta o no está claro, detente antes del primer commit y pregunta.
- Registra un checkpoint compacto en una ruta compatible con worktrees, obtenida con `git rev-parse --git-path`. Incluye rama/HEAD originales, referencias y SHA resueltos, archivos previstos, versión, pasos completados y estado de publicación; nunca guardes credenciales. Si ya existe un checkpoint, inspecciónalo y compara las referencias reales antes de decidir si reanudas o preparas otro release.

## 2. Sincronizar e integrar localmente

Actualiza las ramas locales configuradas de producción e integración con avance rápido únicamente cuando estén detrás de sus remotos. No descartes commits locales ni fusiones automáticamente historiales divergentes; inspecciona la divergencia y detente si cambia el alcance previsto. Detecta si una rama está activa en otro worktree; no fuerces el cambio.

Desde integración, continúa allí. Desde una feature, intégrala solo si se pidió o si la guía del equipo establece que forma parte del release; en caso contrario, pregunta si debe incluirse. Desde otra rama, aclara cuál es la rama de origen antes de cambiar.

Si una feature no se publicó, haz rebase sobre la punta sincronizada de integración cuando esa sea la estrategia del equipo. Por defecto, conserva el historial de una feature publicada; no reescribas commits compartidos automáticamente. Si se solicita explícitamente reescribirlo, registra el SHA remoto anterior. Cualquier force push posterior requiere autorización específica y `--force-with-lease=<ref>:<sha-anterior>` exacto. Nunca fuerces push a producción o integración.

Integra localmente la feature aprobada con la estrategia de merge del equipo y un mensaje deliberado que cumpla el manual. Conserva la rama feature. No publiques cambios de integración en esta fase.

## 3. Fijar el rango y elegir versión/etiqueta

Después de integrar, fija la punta candidata de integración como `SOURCE_SHA` y registra la punta de producción. Resuelve las etiquetas de versión usando el prefijo configurado exacto y SemVer válido. No uses orden lexicográfico de etiquetas para determinar la base del release.

Elige la etiqueta de release aplicable más reciente en el historial first-parent de producción y exige que su commit derivado sea ancestro de `SOURCE_SHA`. Registra etiqueta y commit derivado como `BASE_SHA`. Si varias etiquetas apuntan al mismo commit, las historias no coinciden o producción contiene cambios ausentes en la candidata, resuélvelo antes de continuar. Si no existe una etiqueta base aplicable, pregunta cuál es la base en vez de adivinarla. Ignora etiquetas ajenas al release al inferir la versión.

Interpreta SemVer numéricamente, respetando prerelease y prefijo configurado. La versión explícita debe ser válida y no colisionar con una etiqueta ni rama de release local o remota. Comprueba que avance respecto de las versiones existentes; no permitas una degradación silenciosa. Para promociones o incrementos prerelease, exige una política o versión explícita si el repositorio no la define.

Sugiere el incremento usando sujetos y cuerpos completos de `BASE_SHA..SOURCE_SHA`: un footer `BREAKING CHANGE:` o un `!` en commits históricos tiene prioridad sobre `feat`, seguido de fixes/mantenimiento. Esto solo interpreta el historial: los mensajes nuevos creados por esta skill nunca deben usar `!`, porque el manual indica que el validador actual no lo acepta. Sin etiquetas, usa la versión declarada del proyecto y la guía; si ninguna define una versión inicial, pregunta en vez de inventarla. Si no hay una política inequívoca, presenta una recomendación y resuelve la decisión una vez.

Genera el changelog desde ese rango SHA fijado, no desde un HEAD que pueda moverse. Sin etiqueta previa, usa los commits alcanzables desde `SOURCE_SHA`. Excluye merges y commits de mantenimiento del release identificados previamente; cualquier commit nuevo queda fuera del rango congelado. Inspecciona diffs relevantes si los mensajes son vagos y no inventes cambios. Agrupa entradas significativas en funcionalidades, correcciones, rendimiento, refactorización, documentación y mantenimiento, omitiendo grupos vacíos. Si la guía usa squash/rebase, deriva entradas del historial real y no prometas una entrada por cada commit original.

## 4. Preparar el release

- Crea la rama de release configurada exactamente desde `SOURCE_SHA`. Si ya existe, inspecciona checkpoint y rama; no la sobrescribas ni recrees.
- No edites archivos de versión de la aplicación por defecto. Si la política explícita del repositorio y la autorización del usuario requieren el cambio, modifica solo archivos fuente de versión y lockfiles/workspace relacionados con el método existente. Conserva cambios ajenos y no actualices dependencias. Revisa el diff, prepara explícitamente `git add -- <rutas-de-version>` y crea un commit conforme al manual solo si hubo cambios. Un archivo de versión nuevo requiere una razón del proyecto; no lo inventes para producir un commit de incremento. Nunca uses `git commit -am`.
- Ejecuta las comprobaciones exigidas por el repositorio para este release y registra los resultados reales. Una comprobación requerida fallida detiene la publicación; una excepción de demo aprobada por el usuario debe informarse como excepción, no como comprobación aprobada.
- Fusiona release en producción con la estrategia del equipo, normalmente `--no-ff` y el mensaje preparado. Registra el `RELEASE_SHA` resultante y crea la etiqueta anotada con prefijo y versión exactamente en ese SHA. Fusiona ese commit exacto de release de vuelta a integración. Comprueba código de salida y estado Git después de cada operación.
- Verifica que la etiqueta derive a `RELEASE_SHA`, que producción apunte al release previsto y que integración lo contenga. Conserva la rama release hasta verificar la publicación.

## Mensajes

Aplica el manual `MANUAL_GIT_COMMIT_v1.1` a cada commit creado por este flujo; no dependas de que `/commit` esté instalado. Usa uno de los tipos aprobados (`feat`, `fix`, `improve`, `refactor`, `style`, `perf`, `docs`, `test`, `chore`, `build`, `ci`, `revert`) y un alcance significativo obligatorio. La descripción empieza en minúscula, usa un verbo de acción y no termina en punto. La primera línea completa, incluido el ticket, no debe superar 72 caracteres. Todo commit distinto de `docs` necesita un ID real positivo de Azure DevOps, sin ceros iniciales. Nunca inventes un ticket. Si Azure DevOps está conectado, verifica el ID; si no, usa solo el proporcionado por el usuario e informa que no se verificó independientemente. No uses `wip`, `!` ni `Co-Authored-By`; explica incompatibilidades con un footer `BREAKING CHANGE:`. El manual no fija idioma, así que usa el idioma habitual del equipo.

Patrones de ejemplo (reemplaza el marcador por un ticket real autorizado; no inventes ni reutilices uno):

- Merge a producción: `chore(release): integrar versión X.Y.Z #<ticket>`
- Merge a integración: `chore(release): integrar X.Y.Z a <rama> #<ticket>`
- Integración de feature: `chore(merge): integrar <feature> a <rama> #<ticket>`
- El mensaje de etiqueta anotada no es un asunto de commit: usa versión y changelog congelado, pero no lo trates como sustituto del ticket requerido en los commits.

No crees por defecto un commit para incrementar la versión: según el manual, la numeración decimal de la aplicación se gestiona por separado durante el despliegue. Si una política explícita del repositorio y la autorización del usuario lo requieren, usa un asunto conforme al manual y un ticket real, e incluye únicamente los archivos de versión autorizados.

Escribe mensajes multilínea en archivos temporales con rutas exactas obtenidas mediante `git rev-parse --git-path` o el directorio temporal del sistema y luego usa `-F <archivo>`. No interpoles mensajes en código de shell. `.git` no siempre es un directorio. Conserva archivos útiles si falla el proceso; si tiene éxito, elimina solo los archivos exactos creados por esta ejecución, nunca limpiezas con comodines.

## 5. Publicar con autorización

Muestra identidad del remoto, ramas de producción/integración, SHA de origen/base/release, cambios incluidos, versión/etiqueta, archivos de versión modificados, comprobaciones y si se solicitó un GitHub Release. Pide autorización de publicación solo si aún no se dio explícitamente para este alcance. `prepare-only` se detiene aquí.

Inmediatamente antes de push, vuelve a actualizar referencias remotas y compara sus puntas con los SHA registrados. Confirma también que referencias locales y árbol de trabajo siguen correspondiendo al release preparado. Si algo cambió, detente y concílialo; no sobrescribas ni integres trabajo nuevo a ciegas en el release autorizado.

Prefiere un único `git push --atomic <remoto>` con tres refspecs explícitos: rama de producción, rama de integración y esta etiqueta. Usa referencias completas en ambos lados. No publiques todas las ramas ni `--tags`; no fuerces. Si el servidor indica explícitamente que no admite push atómico, explica la limitación y pide autorización para publicación secuencial, salvo que esa alternativa ya estuviera autorizada. Un error genérico de push no demuestra que atomic no esté soportado.

Después del push, compara los SHA de ramas remotas y el objeto/commit derivado de la etiqueta anotada con los valores locales previstos, consultando las referencias remotas. Si hay timeout o se pierde la confirmación, inspecciona el remoto antes de reintentar. Solo entonces informa que las referencias Git están publicadas.

Si se solicitó GitHub Release, usa el repositorio verificado y la etiqueta publicada exacta. Prefiere el conector GitHub disponible o `gh release create` con archivo de notas y verificación de etiqueta. Crea un borrador solo si se pidió. Si ya existe un release para esa etiqueta, inspecciónalo e informa/reanuda; no lo sobrescribas automáticamente. Si falla crear el GitHub Release, no reviertas referencias Git ya publicadas: informa el resultado parcial y reintenta solo ese paso pendiente cuando corresponda.

Conserva ramas feature y release por defecto. La limpieza local opcional requiere política solicitada/definida por el equipo, publicación verificada y confirmación de que la punta exacta de la rama está integrada. Usa borrado normal y seguro, nunca borrado forzado. Borrar una rama remota es una operación separada que debe pedir el usuario.

## Conflictos, fallos y reanudación

Ante un conflicto, detente y registra operación activa, referencias pertinentes y rutas sin resolver. Informa qué archivos requieren resolución y pide al usuario que los resuelva, salvo que se haya delegado explícitamente esa tarea. No abortes ni reinicies automáticamente sus cambios.

Al reanudar, inspecciona el estado real del merge/rebase y las rutas que tuvieron conflictos. Un archivo editado puede seguir en el índice como no fusionado hasta que se prepare: no exijas que la lista de archivos sin resolver esté vacía antes de prepararlos. Revisa diffs y marcadores, prepara solo rutas resueltas y comprueba después que `git ls-files -u` esté vacío y que los cambios preparados no incluyan trabajo ajeno. La presencia o ausencia de texto marcador no demuestra por sí sola que el conflicto esté resuelto. Continúa la operación activa exacta con su comando y mensaje preparados; repite el ciclo si aparece otro conflicto.

Ante cualquier otro fallo, detén pasos dependientes e informa rama/HEAD actuales, commits/merges/etiqueta locales completados, referencias publicadas, comando fallido y siguiente paso mínimo. Conserva ramas, etiquetas y checkpoint. Nunca borres automáticamente referencias publicadas ni reinicies commits para simular una reversión. Al reanudar, concilia checkpoint con el estado local y remoto y omite pasos ya verificados. No recrees una etiqueta, repitas un incremento ni publiques dos veces a ciegas.

El informe final distingue preparación local, publicación Git remota y publicación de GitHub Release. Incluye resultados de comprobación, ramas conservadas y el paso exacto pendiente si el flujo quedó incompleto. No afirmes que hubo despliegue ni notifiques al equipo salvo que se haya solicitado por separado.
