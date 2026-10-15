# Checklist de Negocio: Reglas de Atención

| Campo | Valor |
| :--- | :--- |
| **Feature** | `001-citas-medicas-core` |
| **Tipo** | Negocio: calidad de requisitos |
| **Fecha** | 2026-10-07 |
| **Evaluado contra** | [spec.md](../spec.md) · [data-model.md](../data-model.md) · [plan.md](../plan.md) · [api-contracts.md](../contracts/api-contracts.md) |

> **Propósito**: estos ítems son *pruebas unitarias de los requisitos*. Verifican que las reglas de negocio estén **escritas** de forma completa, clara, consistente y medible. **No** verifican el comportamiento del código.
>
> **Leyenda**: `[x]` el requisito cumple · `[ ]` hay un hallazgo (ver §6). Etiquetas: `[Completitud]`, `[Claridad]`, `[Consistencia]`, `[Medibilidad]`, `[Cobertura]`, `[Gap]`, `[Ambigüedad]`, `[Conflicto]`.

---

## 1. Citas duplicadas en el mismo horario para un médico

- [x] CHK001 ¿Está especificado que un médico no puede tener dos bloques que se solapen en la misma fecha? [Completitud, Spec RF-013, INV-02]
- [x] CHK002 ¿Está definido con precisión qué significa «solapar», incluido el caso de bloques contiguos? [Claridad, Spec §5 «Bloques contiguos», data-model §5]
- [x] CHK003 ¿Está especificado que un bloque ya *Reservado* no admite una segunda cita? [Completitud, Spec RF-016, HU-03 esc. 2]
- [x] CHK004 ¿Está definida la regla «como máximo una cita no cancelada por bloque» como invariante verificable? [Medibilidad, data-model INV-03, Spec CE-002]
- [x] CHK005 ¿Está cubierto el escenario de dos reservas simultáneas sobre el mismo bloque, con un resultado esperado medible? [Cobertura, Spec RF-021, HU-03 esc. 4]
- [x] CHK006 ¿Se exige que la creación de la cita y el cambio de estado del bloque sean atómicos? [Completitud, Spec RF-020]
- [x] CHK007 ¿Es consistente el mensaje de error de duplicidad entre spec y contratos («Horario no disponible» / `SLOT_NOT_AVAILABLE`)? [Consistencia, Spec RF-016, Contratos §4]
- [ ] CHK008 ¿Está especificado si la verificación de solapamiento al **crear bloques** debe resistir solicitudes concurrentes? [Gap, Spec RF-013. Solo lo cubre el plan R-03; no hay requisito ni criterio de éxito] → **H-01**
- [ ] CHK009 ¿Está definido si un **paciente** puede tener dos citas *Pendientes* en el mismo horario con médicos distintos? [Gap, Spec §4.3] → **H-02**
- [ ] CHK010 ¿Está definido si un paciente puede tener varias citas *Pendientes* con el mismo médico en la misma fecha? [Gap, Spec §4.3] → **H-02**

## 2. Restricción de cancelar citas *Atendidas*

- [x] CHK011 ¿Está especificado que una cita *Atendida* no puede cambiar de estado? [Completitud, Spec RF-025: «impedir cualquier modificación de su … estado»]
- [x] CHK012 ¿Está modelado *Atendida* como estado final en la máquina de estados? [Consistencia, data-model §4.2]
- [x] CHK013 ¿Está declarado explícitamente que la cancelación queda fuera de alcance de esta feature? [Claridad, Spec §9]
- [ ] CHK014 ¿Existe un requisito explícito con su código de error para el intento de **cancelar** una cita *Atendida*? [Gap, Spec RF-025 lo cubre solo de forma implícita; no hay operación ni código `APPOINTMENT_*` para cancelar] → **H-03**
- [ ] CHK015 ¿Es alcanzable el estado *Cancelada* en esta feature, de modo que los escenarios que lo usan sean verificables? [Conflicto, Spec HU-04 esc. 5 y RF-024 prueban una cita *Cancelada*, pero ninguna transición la produce (data-model §4.2)] → **H-04**
- [ ] CHK016 ¿Está definido qué ocurre con el bloque de agenda si en el futuro se cancela una cita *Pendiente* (vuelve a *Disponible*, sí o no)? [Ambigüedad, data-model §4.1 lo dibuja, pero la spec no lo afirma] → **H-03**
- [x] CHK017 ¿Está garantizado que no existe ninguna vía (endpoint u operación) para modificar una cita *Atendida*? [Cobertura, Contratos §2 «No existen endpoints de edición», `RegisterAttentionAsync` *compare-and-set*]

## 3. Formato y validación del DNI

- [x] CHK018 ¿Está especificado el formato exacto del DNI (8 dígitos numéricos)? [Claridad, Spec RF-003, Supuesto 1]
- [x] CHK019 ¿Está especificada la unicidad del DNI con su error asociado? [Completitud, Spec RF-002, INV-01]
- [x] CHK020 ¿Está definido el tratamiento de los espacios al inicio y al final antes de validar? [Claridad, Spec §5 «Espacios en blanco»]
- [x] CHK021 ¿Se conservan los ceros a la izquierda (el tipo es texto, no número)? [Claridad, data-model §2.1 `Texto(8)`]
- [x] CHK022 ¿Existen escenarios de aceptación medibles para DNI válido y duplicado? [Medibilidad, Spec HU-01 esc. 1 y 2]
- [ ] CHK023 ¿Existe un escenario de aceptación para un DNI con formato inválido (7 dígitos, 9 dígitos, letras)? [Cobertura, Spec HU-01: RF-003 no tiene escenario] → **H-05**
- [ ] CHK024 ¿Está acotado «dígitos numéricos» a los caracteres ASCII `0-9`, excluyendo dígitos Unicode de otros alfabetos? [Ambigüedad, Spec RF-003] → **H-05**
- [ ] CHK025 ¿Está definido el comportamiento al **buscar** por un DNI con formato inválido (error o resultado vacío)? [Gap, Spec RF-006 vs RF-003] → **H-05**
- [ ] CHK026 ¿Está definido el orden de validación cuando un DNI es inválido **y** hay otros campos vacíos (un error o todos)? [Ambigüedad, Spec RF-003 a RF-005] → **H-06**

## 4. Diagnóstico y receta para cerrar la cita

- [x] CHK027 ¿Está especificado que el diagnóstico es obligatorio para pasar a *Atendida*? [Completitud, Spec RF-023, INV-05]
- [x] CHK028 ¿Está definido que un diagnóstico compuesto solo por espacios se considera vacío? [Claridad, Spec §5 «Espacios en blanco»]
- [x] CHK029 ¿Está especificado que solo una cita *Pendiente* puede cerrarse? [Completitud, Spec RF-024]
- [ ] CHK030 ¿Es consistente la obligatoriedad de la **receta** con la regla de negocio «diagnóstico **y** receta obligatorios para cerrar la cita»? [Conflicto, Spec Supuesto 3, RF-022 y HU-04 esc. 2 declaran la receta **opcional**] → **H-07**
- [ ] CHK031 ¿Está definido si una cita puede cerrarse **antes** de la fecha y hora de su bloque? [Gap, Spec §5 permite registrar la atención «en fecha distinta», sin acotar las fechas futuras] → **H-08**
- [ ] CHK032 ¿Están especificadas las longitudes mínima y máxima de diagnóstico y receta? [Gap, data-model §2.4 `Texto(2000)` sin RF; misma situación que la observación H-01 del plan] → **H-09**
- [ ] CHK033 ¿Está definido quién está autorizado a cerrar una cita (solo el médico del bloque)? [Gap, Spec §2 actor «Médico» vs §9 sin autenticación] → **H-10**
- [x] CHK034 ¿Está especificado que los datos clínicos son inmutables una vez cerrada la cita? [Completitud, Spec RF-025, HU-04 esc. 4]

---

## 5. Resumen

| Punto de negocio | Ítems | ✅ Cumple | ❌ Hallazgos | Estado |
| :--- | :-: | :-: | :-: | :--- |
| 1. Citas duplicadas por médico | 10 | 7 | 3 | ⚠️ Núcleo cubierto; faltan reglas de doble reserva del paciente |
| 2. No cancelar citas *Atendidas* | 7 | 4 | 3 | ⚠️ Cubierto de forma implícita; *Cancelada* no es alcanzable |
| 3. Formato del DNI | 9 | 5 | 4 | ⚠️ Regla clara; faltan escenarios y precisión |
| 4. Diagnóstico y receta obligatorios | 8 | 4 | 4 | ❌ **Conflicto** con la spec sobre la receta |
| **Total** | **34** | **20** | **14** | |

---

## 6. Hallazgos y Acciones Propuestas

| ID | Severidad | Ítems | Hallazgo | Acción propuesta |
| :--- | :-: | :--- | :--- | :--- |
| **H-07** | 🔴 Alta | CHK030 | La regla de negocio exige **receta obligatoria**, pero la spec la declara opcional (Supuesto 3, RF-022, HU-04 esc. 2). | Decidir. **(a)** Receta obligatoria: modificar RF-022, eliminar HU-04 esc. 2, añadir el error `ATTENTION_PRESCRIPTION_REQUIRED`, actualizar INV-05 y los contratos E7. **(b)** Mantener opcional: retirar la regla de negocio. |
| **H-04** | 🔴 Alta | CHK015 | HU-04 esc. 5 y RF-024 dependen de una cita *Cancelada* que no puede existir en esta feature, así que el escenario no es verificable vía casos de uso. | Reformular HU-04 esc. 5 como prueba **unitaria de dominio** (construyendo el estado) o retirarlo hasta la feature de cancelación. |
| **H-02** | 🟠 Media | CHK009, CHK010 | No hay regla sobre la doble reserva del **paciente** (mismo horario con otro médico, o varias citas el mismo día). | Añadir RF: *«Si el paciente ya tiene una cita Pendiente cuyo bloque se solapa con el bloque solicitado, entonces el sistema deberá rechazar la reserva»* (`PATIENT_SCHEDULE_CONFLICT`). |
| **H-03** | 🟠 Media | CHK014, CHK016 | La prohibición de cancelar una cita *Atendida* solo se deduce de RF-025, y la liberación del bloque al cancelar no está afirmada. | Registrarlo como requisito heredado obligatorio para la futura feature de cancelación: «Si la cita está *Atendida*, rechazar la cancelación» y «al cancelar una cita *Pendiente*, el bloque vuelve a *Disponible*». |
| **H-08** | 🟠 Media | CHK031 | Se puede cerrar una cita cuyo bloque aún no ha ocurrido (por ejemplo, una cita de la próxima semana). | Añadir RF: *«Si la hora de inicio del bloque aún no ha llegado, rechazar el registro de atención»* (`ATTENTION_BEFORE_SLOT`). |
| **H-05** | 🟡 Baja | CHK023–CHK025 | Falta un escenario para DNI inválido; «dígitos numéricos» es ambiguo; búsqueda con un DNI mal formado sin definir. | Añadir HU-01 esc. 6 (DNI `1234567` → `PATIENT_DNI_INVALID`). Precisar en RF-003 «8 caracteres `0-9`». Definir en RF-006 «DNI con formato inválido → resultado vacío». |
| **H-01** | 🟡 Baja | CHK008 | La concurrencia en la creación de bloques está resuelta en el plan, pero sin requisito ni criterio de éxito. | Añadir a CE-002: «… ni dos bloques solapados del mismo médico, incluso bajo concurrencia». |
| **H-06** | 🟡 Baja | CHK026 | No se define si se reporta el primer error o todos. | Precisar en RF-029: «se reporta el primer error encontrado en el orden DNI → nombres → apellidos → teléfono → email». |
| **H-09** | 🟡 Baja | CHK032 | Las longitudes de diagnóstico y receta no tienen RF. | Resolver junto con la observación H-01 del plan (RF-030 propuesto). |
| **H-10** | ⚪ Informativa | CHK033 | No hay control de qué médico cierra la cita. | Aceptado por spec §9 (sin autenticación). Registrar como requisito para la feature de seguridad. |

> **Bloqueantes para `/speckit-tasks`**: **H-07** y **H-04**. El resto puede incorporarse a la spec antes de las tareas o diferirse explícitamente.
