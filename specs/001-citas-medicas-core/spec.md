# Especificación Funcional: Núcleo de Citas Médicas

| Campo | Valor |
| :--- | :--- |
| **Feature** | `001-citas-medicas-core` |
| **Rama** | `001-citas-medicas-core` |
| **Fecha** | 2026-10-07 |
| **Estado** | Borrador (pendiente de aprobación) |
| **Modelo de datos** | [data-model.md](./data-model.md) |

---

## 1. Resumen

Esta feature establece el núcleo funcional del Sistema de Gestión de Citas Médicas: registro y búsqueda de pacientes, configuración de bloques de agenda para médicos activos, reserva de citas sobre horarios disponibles y registro de la atención médica (diagnóstico y receta).

El documento describe **qué** debe hacer el sistema y **por qué**. No define tecnología, endpoints ni estructuras de código; eso corresponde a `plan.md` y `tasks.md`.

---

## 2. Actores

| Actor | Descripción |
| :--- | :--- |
| **Recepcionista** | Registra y busca pacientes, configura agendas y reserva citas. |
| **Médico** | Consulta sus citas y registra la atención (diagnóstico y receta). |

> No se incluye autenticación ni autorización por rol en esta feature (ver §8 Fuera de alcance).

---

## 3. Historias de Usuario y Escenarios de Aceptación

Las historias están priorizadas; cada una es una porción verificable de forma independiente.

### HU-01 — Registro y búsqueda de pacientes (Prioridad: P1)

**Como** recepcionista, **quiero** registrar pacientes y encontrarlos por DNI o apellidos, **para** asociarlos a sus citas sin duplicar información.

**Por qué P1**: sin pacientes no es posible reservar citas; es la base de todo el flujo.

**Prueba independiente**: registrar un paciente y recuperarlo por DNI y por apellidos.

| # | Dado | Cuando | Entonces |
| :-: | :--- | :--- | :--- |
| 1 | No existe paciente con DNI `45678912` | Registro un paciente con datos válidos y DNI `45678912` | El paciente queda registrado y recibo su identificador |
| 2 | Ya existe un paciente con DNI `45678912` | Intento registrar otro paciente con el mismo DNI | El sistema rechaza el registro indicando DNI duplicado |
| 3 | Existe el paciente con DNI `45678912` | Busco por DNI `45678912` | Obtengo exactamente ese paciente |
| 4 | Existen pacientes "García López" y "Garcés Ruiz" | Busco por apellidos "gar" | Obtengo ambos pacientes |
| 5 | No existe ningún paciente con apellidos "Zapata" | Busco por apellidos "Zapata" | Obtengo un resultado vacío, sin error |

---

### HU-02 — Configuración de agenda de médicos (Prioridad: P1)

**Como** recepcionista, **quiero** crear bloques horarios en la agenda de un médico activo, **para** ofrecer horarios disponibles a los pacientes.

**Por qué P1**: sin bloques disponibles no existe oferta de citas.

**Prueba independiente**: crear un bloque para un médico activo y verificar que queda en estado *Disponible*.

| # | Dado | Cuando | Entonces |
| :-: | :--- | :--- | :--- |
| 1 | Un médico activo sin bloques el 2026-10-15 | Creo un bloque 09:00–09:30 para ese día | El bloque queda registrado en estado *Disponible* |
| 2 | Un médico inactivo | Intento crear un bloque | El sistema rechaza la operación indicando médico inactivo |
| 3 | Un médico activo | Intento crear un bloque 10:00–09:30 | El sistema rechaza la operación por rango horario inválido |
| 4 | Un médico activo con bloque 09:00–09:30 el 2026-10-15 | Intento crear 09:15–09:45 el mismo día | El sistema rechaza la operación por solapamiento |
| 5 | Un médico activo | Intento crear un bloque en una fecha pasada | El sistema rechaza la operación |

---

### HU-03 — Reserva de cita (Prioridad: P1)

**Como** recepcionista, **quiero** reservar una cita para un paciente en un bloque disponible, **para** asegurar su atención con el médico.

**Por qué P1**: es el valor central del sistema.

**Prueba independiente**: con un paciente y un bloque *Disponible*, reservar y comprobar que la cita queda *Pendiente* y el bloque *Reservado*.

| # | Dado | Cuando | Entonces |
| :-: | :--- | :--- | :--- |
| 1 | Un paciente registrado y un bloque *Disponible* | Reservo una cita con un motivo | Se crea la cita en estado *Pendiente* y el bloque pasa a *Reservado* |
| 2 | Un bloque en estado *Reservado* | Intento reservarlo | El sistema rechaza la reserva indicando horario no disponible |
| 3 | Un bloque *Disponible* y un paciente inexistente | Intento reservar | El sistema rechaza la reserva indicando paciente no encontrado |
| 4 | Dos solicitudes simultáneas sobre el mismo bloque *Disponible* | Ambas se procesan | Solo una reserva tiene éxito; la otra es rechazada |
| 5 | Un bloque *Disponible* | Intento reservar sin motivo | El sistema rechaza la reserva indicando motivo obligatorio |

---

### HU-04 — Registro de atención médica (Prioridad: P2)

**Como** médico, **quiero** registrar el diagnóstico y la receta de una cita, **para** dejar constancia clínica y cerrar la atención.

**Por qué P2**: depende de que existan citas reservadas (HU-03).

**Prueba independiente**: sobre una cita *Pendiente*, registrar la atención y verificar el estado *Atendida* con los datos clínicos.

| # | Dado | Cuando | Entonces |
| :-: | :--- | :--- | :--- |
| 1 | Una cita *Pendiente* | Registro diagnóstico y receta | La cita pasa a *Atendida* y conserva diagnóstico y receta |
| 2 | Una cita *Pendiente* | Registro diagnóstico sin receta | La cita pasa a *Atendida* con receta vacía |
| 3 | Una cita *Pendiente* | Intento registrar la atención sin diagnóstico | El sistema rechaza la operación indicando diagnóstico obligatorio |
| 4 | Una cita *Atendida* | Intento registrar nuevamente la atención | El sistema rechaza la operación; los datos clínicos no cambian |
| 5 | Una cita *Cancelada* | Intento registrar la atención | El sistema rechaza la operación |

---

## 4. Requisitos Funcionales (EARS)

Patrones utilizados:
- **Ubicuo**: *El sistema deberá…*
- **Por evento**: *Cuando <evento>, el sistema deberá…*
- **Por estado**: *Mientras <estado>, el sistema deberá…*
- **No deseado**: *Si <condición>, entonces el sistema deberá…*

Cada ID es la referencia obligatoria para toda tarea e implementación (Constitución, principio 2).

### 4.1 Pacientes (HU-01)

| ID | Tipo | Requisito |
| :--- | :--- | :--- |
| **RF-001** | Por evento | Cuando la recepcionista registre un paciente con DNI, nombres, apellidos y teléfono válidos, el sistema deberá persistir el paciente y devolver su identificador. |
| **RF-002** | No deseado | Si el DNI ingresado ya pertenece a otro paciente, entonces el sistema deberá rechazar el registro con el error "DNI ya registrado". |
| **RF-003** | No deseado | Si el DNI no tiene exactamente 8 dígitos numéricos, entonces el sistema deberá rechazar el registro con el error "DNI inválido". |
| **RF-004** | No deseado | Si el email es informado y no tiene un formato válido, entonces el sistema deberá rechazar el registro con el error "Email inválido". |
| **RF-005** | No deseado | Si nombres, apellidos o teléfono están vacíos, entonces el sistema deberá rechazar el registro indicando el campo obligatorio faltante. |
| **RF-006** | Por evento | Cuando se busque un paciente por DNI, el sistema deberá devolver el paciente cuyo DNI coincida exactamente, o un resultado vacío si no existe. |
| **RF-007** | Por evento | Cuando se busque pacientes por apellidos, el sistema deberá devolver todos los pacientes cuyos apellidos contengan el texto buscado, sin distinguir mayúsculas, minúsculas ni tildes, ordenados por apellidos y nombres. |
| **RF-008** | No deseado | Si el criterio de búsqueda por apellidos tiene menos de 2 caracteres, entonces el sistema deberá rechazar la búsqueda con el error "Criterio de búsqueda demasiado corto". |

### 4.2 Agenda de médicos (HU-02)

| ID | Tipo | Requisito |
| :--- | :--- | :--- |
| **RF-009** | Por evento | Cuando la recepcionista cree un bloque de agenda para un médico con fecha, hora de inicio y hora de fin válidas, el sistema deberá registrarlo en estado *Disponible*. |
| **RF-010** | No deseado | Si el médico no existe o no está activo (`IsActivo = false`), entonces el sistema deberá rechazar la creación del bloque con el error "Médico no disponible para agenda". |
| **RF-011** | No deseado | Si la hora de inicio es mayor o igual a la hora de fin, entonces el sistema deberá rechazar el bloque con el error "Rango horario inválido". |
| **RF-012** | No deseado | Si la fecha del bloque es anterior a la fecha actual, o es la fecha actual con hora de inicio ya transcurrida, entonces el sistema deberá rechazar el bloque con el error "No se permiten bloques en el pasado". |
| **RF-013** | No deseado | Si el nuevo bloque se solapa con otro bloque existente del mismo médico en la misma fecha, entonces el sistema deberá rechazar el bloque con el error "Bloque solapado". |
| **RF-014** | Por evento | Cuando se consulte la agenda de un médico para una fecha, el sistema deberá devolver sus bloques ordenados por hora de inicio, indicando el estado de cada uno. |

### 4.3 Reserva de citas (HU-03)

| ID | Tipo | Requisito |
| :--- | :--- | :--- |
| **RF-015** | Por evento | Cuando la recepcionista reserve una cita indicando bloque de agenda, paciente y motivo, el sistema deberá crear la cita en estado *Pendiente* y cambiar el bloque a *Reservado*. |
| **RF-016** | No deseado | Si el bloque de agenda no está en estado *Disponible*, entonces el sistema deberá rechazar la reserva con el error "Horario no disponible". |
| **RF-017** | No deseado | Si el paciente o el bloque de agenda no existen, entonces el sistema deberá rechazar la reserva indicando la entidad no encontrada. |
| **RF-018** | No deseado | Si el motivo está vacío, entonces el sistema deberá rechazar la reserva con el error "Motivo obligatorio". |
| **RF-019** | No deseado | Si el médico del bloque ya no está activo o el bloque ya inició, entonces el sistema deberá rechazar la reserva con el error "Horario no disponible". |
| **RF-020** | Ubicuo | El sistema deberá ejecutar la creación de la cita y el cambio de estado del bloque como una única operación atómica: ambas se confirman o ninguna. |
| **RF-021** | No deseado | Si dos reservas concurrentes compiten por el mismo bloque, entonces el sistema deberá confirmar como máximo una y rechazar las demás con el error "Horario no disponible". |

### 4.4 Atención médica (HU-04)

| ID | Tipo | Requisito |
| :--- | :--- | :--- |
| **RF-022** | Por evento | Cuando el médico registre la atención de una cita con diagnóstico y, opcionalmente, receta, el sistema deberá guardar ambos datos y cambiar la cita a estado *Atendida*. |
| **RF-023** | No deseado | Si el diagnóstico está vacío, entonces el sistema deberá rechazar el registro con el error "Diagnóstico obligatorio". |
| **RF-024** | No deseado | Si la cita no está en estado *Pendiente*, entonces el sistema deberá rechazar el registro con el error "La cita no admite atención". |
| **RF-025** | Por estado | Mientras una cita esté en estado *Atendida*, el sistema deberá impedir cualquier modificación de su diagnóstico, receta, motivo o estado. |
| **RF-026** | Por evento | Cuando se consulte una cita, el sistema deberá devolver sus datos junto con el paciente, el médico, la fecha y el horario del bloque asociado. |

### 4.5 Reglas transversales

| ID | Tipo | Requisito |
| :--- | :--- | :--- |
| **RF-027** | Ubicuo | El sistema deberá evaluar todas las reglas de agenda y clínicas (RF-009 a RF-025) en la capa de Dominio/Aplicación, nunca en controladores ni vistas. |
| **RF-028** | Ubicuo | El sistema deberá acceder a los datos exclusivamente mediante interfaces de repositorio. |
| **RF-029** | Ubicuo | El sistema deberá devolver mensajes de error de negocio en español, con un código de error estable que permita su verificación en pruebas automatizadas. |

---

## 5. Casos Límite

- **Búsqueda con tildes**: buscar "Nunez" debe encontrar "Núñez" (RF-007).
- **Bloques contiguos**: 09:00–09:30 y 09:30–10:00 **no** se consideran solapados; el fin es exclusivo (RF-013).
- **Médico desactivado con bloques futuros**: los bloques existentes se conservan, pero no admiten nuevas reservas (RF-019). Las citas ya reservadas siguen *Pendientes*.
- **Reserva de un bloque del día actual ya iniciado**: se rechaza (RF-019).
- **Doble clic en "Reservar"**: la segunda solicitud se rechaza por "Horario no disponible" (RF-016, RF-021).
- **Espacios en blanco**: DNI, nombres, apellidos, motivo y diagnóstico se normalizan quitando espacios iniciales y finales; un valor que solo contiene espacios se considera vacío.
- **Atención registrada en fecha distinta a la del bloque**: se permite; esta feature no restringe la fecha de registro de la atención.

---

## 6. Entidades Clave

Resumen conceptual; el detalle de atributos, tipos y restricciones está en [data-model.md](./data-model.md).

- **Paciente**: persona que recibe atención; se identifica de forma única por su DNI.
- **Médico**: profesional identificado por su número de colegiatura; solo los activos pueden ofrecer agenda.
- **Agenda**: bloque horario de un médico en una fecha; está *Disponible* o *Reservado*.
- **Cita/Atención**: reserva de un paciente sobre un bloque; evoluciona de *Pendiente* a *Atendida* y almacena diagnóstico y receta.

---

## 7. Criterios de Éxito

| ID | Criterio medible |
| :--- | :--- |
| **CE-001** | El 100 % de los escenarios de aceptación de HU-01 a HU-04 están cubiertos por pruebas automatizadas que pasan con `dotnet test`. |
| **CE-002** | En ninguna circunstancia, incluida la concurrencia, existen dos citas no canceladas asociadas al mismo bloque de agenda. |
| **CE-003** | En ninguna circunstancia existe un bloque *Reservado* sin cita asociada, ni una cita *Pendiente* sobre un bloque *Disponible*. |
| **CE-004** | Una recepcionista puede localizar a un paciente por DNI o apellidos y completar una reserva en menos de 1 minuto. |
| **CE-005** | La búsqueda de pacientes devuelve resultados en menos de 1 segundo con hasta 10 000 pacientes registrados. |
| **CE-006** | Cada requisito RF-001 a RF-029 está referenciado por al menos una tarea en `tasks.md`. |

---

## 8. Supuestos

1. El **DNI** corresponde al documento peruano de 8 dígitos numéricos.
2. El **teléfono** es obligatorio y el **email** es opcional.
3. La **receta** es opcional al registrar la atención; el **diagnóstico** es obligatorio.
4. El catálogo de **médicos** se carga como datos iniciales; su alta, edición y activación no forman parte de esta feature.
5. La duración de los bloques es libre (definida por inicio y fin); no se impone una duración estándar.
6. Fechas y horas se manejan en la zona horaria local de la clínica (America/Lima).
7. Existe una única sede; no se modelan consultorios ni sedes.

---

## 9. Fuera de Alcance

- Cancelación y reprogramación de citas. El estado *Cancelada* se reserva en el modelo de datos para una feature posterior; esta feature no implementa transiciones hacia él.
- Alta, edición y desactivación de médicos (ver supuesto 4).
- Edición y eliminación de pacientes.
- Eliminación de bloques de agenda y generación masiva o recurrente de bloques.
- Autenticación, autorización y roles.
- Notificaciones (email, SMS) y recordatorios.
- Historia clínica consolidada, adjuntos y facturación.

---

## 10. Trazabilidad

| Historia | Requisitos |
| :--- | :--- |
| HU-01 Pacientes | RF-001 – RF-008 |
| HU-02 Agenda | RF-009 – RF-014 |
| HU-03 Reserva | RF-015 – RF-021 |
| HU-04 Atención | RF-022 – RF-026 |
| Transversal (Constitución §3, §4) | RF-027 – RF-029 |
