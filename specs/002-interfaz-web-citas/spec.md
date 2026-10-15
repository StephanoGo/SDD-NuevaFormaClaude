# Especificación Funcional: Interfaz Web de Citas Médicas

| Campo | Valor |
| :--- | :--- |
| **Feature** | `002-interfaz-web-citas` |
| **Rama** | `002-interfaz-web-citas` |
| **Fecha** | 2026-10-15 |
| **Estado** | Borrador (Fase de Especificación) |
| **Dependencias** | [001-citas-medicas-core](../001-citas-medicas-core/spec.md) · [api-contracts.md](../001-citas-medicas-core/contracts/api-contracts.md) |

---

## 1. Resumen Ejecutivo

Esta especificación define los requisitos funcionales, de diseño UI/UX y de interacción para la interfaz web del Sistema de Citas Médicas. Se concibe como una Single Page Application (SPA) ultra ligera, construida con HTML5 semántico, CSS moderno puro (Vanilla CSS) y JavaScript estándar (ES2024), servida directamente desde el servidor Kestrel de la Web API existente mediante archivos estáticos en `src/CitasMedicas.Api/wwwroot`.

La interfaz permite a los actores del sistema (recepcionistas y médicos) operar la totalidad de los flujos clínicos de forma intuitiva, accesible y en tiempo real, interactuando con los 8 endpoints RESTful de `001-citas-medicas-core`.

---

## 2. Clarificaciones y Decisiones de Diseño (Clarifications)

| # | Pregunta de Diseño / Arquitectura | Decisión Acordada | Justificación |
| :-: | :--- | :--- | :--- |
| **C-01** | ¿Cómo se estructura la navegación en la interfaz? | Navegación por pestañas (*Tab Navigation*) fijas en cabecera: **1. Agenda Médica**, **2. Pacientes & Reservas**, **3. Atención Clínica**. | Permite cambio instantáneo de contexto sin recarga de página (SPA), manteniendo la simplicidad operativa para recepcionistas y doctores. |
| **C-02** | ¿Cómo se manejan los médicos disponibles? | Selector desplegable preconfigurado con los médicos del seed (`Ana Torres Vega`, `Luis Paredes Soto`, `María Quispe Rojas`). | La API no expone endpoint de listado de médicos (spec 001 supuesto 4), por lo que la UI consume los IDs conocidos del seed clínico. |
| **C-03** | ¿Cómo se presenta el feedback de operaciones exitosas y errores? | Sistema de notificaciones flotantes tipo *Toast* en la esquina superior derecha, con código de color (verde para éxito, rojo para error, ámbar para advertencias) y cierre automático (4s) o manual. | No interrumpe el flujo de trabajo del usuario (a diferencia de modales bloqueantes) y expone claramente el campo `code` y `detail` de las respuestas `ProblemDetails`. |
| **C-04** | ¿Cómo se visualiza la agenda de horarios? | Matriz visual en cuadrícula de tarjetas horarias fijas con píldoras de estado (`Disponible` en verde, `Reservado` en rojo/ámbar). | Facilita la lectura rápida de disponibilidad de un vistazo por médico y fecha. |
| **C-05** | ¿Qué validaciones ocurren en cliente antes de enviar la petición? | DNI (exactamente 8 dígitos numéricos), obligatoriedad de campos de texto tras recorte de espacios, formato de fecha `yyyy-MM-dd` y rango de horas `HH:mm`. | Previene llamadas innecesarias al backend y entrega retroalimentación visual inmediata con estados de error en los inputs. |

---

## 3. Actores y Flujos de Usuario

| Actor | Responsabilidades en la Interfaz |
| :--- | :--- |
| **Recepcionista** | - Consulta de disponibilidad de médicos por fecha.<br>- Creación de nuevos bloques de agenda.<br>- Búsqueda de pacientes por DNI o apellidos.<br>- Registro rápido de nuevos pacientes.<br>- Reserva atómica de citas en bloques disponibles. |
| **Médico** | - Visualización de citas programadas del día.<br>- Acceso a la ficha de la cita para registro de atención.<br>- Ingreso de diagnóstico clínico (obligatorio) y prescripción médica (opcional).<br>- Cierre definitivo e inmutable de la cita a estado *Atendida*. |

---

## 4. Historias de Usuario y Escenarios de Aceptación (Gherkin)

### HU-WEB-01: Gestión y Consulta Visual de Agenda Médica (Prioridad: P1)
**Como** recepcionista,  
**quiero** seleccionar un médico y una fecha en un calendario visual,  
**para** consultar sus bloques horarios y crear nuevos bloques de atención de manera inmediata.

| # | Dado | Cuando | Entonces |
| :-: | :--- | :--- | :--- |
| 1 | Selecciono al médico "Dra. Ana Torres Vega" y la fecha de hoy | La vista carga los bloques | Se muestran tarjetas horarias ordenadas cronológicamente con su hora de inicio, hora de fin y estado en píldora (`Disponible` o `Reservado`). |
| 2 | La agenda de un médico no tiene bloques en la fecha elegida | Se completa la consulta | Se muestra un estado vacío amigable (*Empty State*) indicando "No hay bloques configurados para esta fecha". |
| 3 | Completo el formulario de nuevo bloque con horario 09:00 a 09:30 | Presiono "Crear Bloque" | El sistema envía `POST /api/doctors/{id}/schedule-slots`, muestra un toast de éxito verde y recarga la cuadrícula agregando el nuevo bloque. |
| 4 | Intento crear un bloque que solapa con uno existente | La API responde 409 `SLOT_OVERLAP` | Se despliega un toast de error rojo con el detalle "Bloque solapado" y el formulario mantiene los datos para corrección. |

---

### HU-WEB-02: Búsqueda, Registro y Reserva de Citas (Prioridad: P1)
**Como** recepcionista,  
**quiero** buscar a un paciente o registrarlo y asignarlo a un bloque libre con su motivo de consulta,  
**para** asegurar su cita médica sin fricción.

| # | Dado | Cuando | Entonces |
| :-: | :--- | :--- | :--- |
| 1 | Ingreso un DNI de 8 dígitos en la barra de búsqueda | Presiono "Buscar" | Se muestra la tarjeta del paciente con su nombre completo, teléfono y correo electrónico. |
| 2 | Busco un paciente que no existe | La consulta devuelve lista vacía | Se ofrece un botón destacado "Registrar Nuevo Paciente" que abre el formulario de registro rápido. |
| 3 | Ingreso un DNI con letras o menos de 8 caracteres | Intento enviar el formulario de registro | El campo resalta con borde rojo y muestra el mensaje inline "El DNI debe tener exactamente 8 dígitos numéricos". |
| 4 | Tengo un paciente seleccionado y un bloque en estado `Disponible` | Ingreso el motivo "Chequeo preventivo" y presiono "Confirmar Reserva" | Se envía `POST /api/appointments`, se muestra notificación de éxito y el bloque cambia de estado a `Reservado` con el ID de cita generado. |

---

### HU-WEB-03: Panel de Atención Médica (Prioridad: P2)
**Como** médico,  
**quiero** abrir una cita en estado pendiente e ingresar el diagnóstico y receta,  
**para** formalizar la atención clínica y archivar la consulta.

| # | Dado | Cuando | Entonces |
| :-: | :--- | :--- | :--- |
| 1 | Ingreso el ID de una cita en estado `Pending` | Presiono "Cargar Cita" | Se despliegan los datos completos: nombre del paciente, DNI, médico asignado, fecha, horario y motivo original. |
| 2 | La cita está cargada en estado `Pending` | Escribo el diagnóstico "Faringoamigdalitis aguda", la receta "Amoxicilina 500mg" y presiono "Registrar Atención" | Se envía `POST /api/appointments/{id}/attention`, la cita pasa visualmente a estado `Atendida` (píldora verde) y los campos quedan bloqueados en modo solo lectura. |
| 3 | Intento registrar la atención dejando el diagnóstico en blanco | Presiono "Registrar Atención" | El cliente bloquea el envío y muestra error inline "El diagnóstico es obligatorio para cerrar la atención". |
| 4 | Consulto una cita que ya fue `Atendida` previamente | Se visualiza en pantalla | El formulario se muestra deshabilitado, indicando que una cita atendida es inmutable. |

---

### HU-WEB-04: Notificaciones y Retroalimentación No Intrusiva (Prioridad: P1)
**Como** usuario del sistema,  
**quiero** recibir confirmaciones visuales claras de cada acción y explicaciones comprensibles ante errores,  
**para** tener certeza del estado de las operaciones clínicas.

| # | Dado | Cuando | Entonces |
| :-: | :--- | :--- | :--- |
| 1 | Cualquier operación HTTP responde 200 o 201 | Se procesa la respuesta | Emerge un Toast verde en la parte superior derecha con ícono de éxito y texto descriptivo. |
| 2 | El servidor responde con error 400, 404 o 409 (ProblemDetails) | Se procesa la respuesta | Emerge un Toast rojo con el título del error, el mensaje en español y el código de negocio (`code`). |
| 3 | Se produce una pérdida de conexión o falla de red | Falla el `fetch` | Emerge un Toast de advertencia indicando "Error de comunicación con el servidor". |

---

## 5. Requisitos Funcionales (EARS)

| ID | Tipo | Requisito |
| :--- | :--- | :--- |
| **RF-WEB-001** | Ubicuo | El sistema deberá servir la aplicación web directamente desde la raíz `http://localhost:{puerto}/` mediante archivos estáticos (`index.html`, `app.css`, `app.js`). |
| **RF-WEB-002** | Ubicuo | La interfaz deberá mantener un contraste mínimo de 4.5:1 para texto normal contra el fondo (cumplimiento WCAG 2.1 Nivel AA). |
| **RF-WEB-003** | Por evento | Cuando el usuario seleccione un médico y fecha en la Vista de Agenda, el sistema deberá consultar `GET /api/doctors/{id}/schedule-slots?date={fecha}` y renderizar los bloques en orden cronológico. |
| **RF-WEB-004** | Por evento | Cuando el usuario cree un bloque horario, la interfaz deberá validar que la hora de inicio sea menor a la de fin antes de realizar el envío a `POST /api/doctors/{id}/schedule-slots`. |
| **RF-WEB-005** | Por evento | Cuando se busque un paciente por DNI o apellido, la interfaz deberá realizar la llamada al endpoint correspondiente (`GET /api/patients?dni=` o `GET /api/patients?lastName=`) respetando la exclusión mutua de parámetros. |
| **RF-WEB-006** | No deseado | Si el usuario busca por apellido con menos de 2 caracteres, el cliente deberá impedir la petición y advertir visualmente la longitud mínima requerida. |
| **RF-WEB-007** | Por evento | Cuando el usuario registre un paciente, el formulario deberá validar en cliente el DNI (8 dígitos) y campos obligatorios antes de enviar `POST /api/patients`. |
| **RF-WEB-008** | Por evento | Cuando el usuario confirme una reserva, la interfaz deberá enviar `POST /api/appointments` con `scheduleSlotId`, `patientId` y `reason`. |
| **RF-WEB-009** | Por estado | Mientras un bloque horario figure como `Reserved`, el botón de reserva sobre dicho bloque deberá mostrarse deshabilitado o con estilo distintivo no accionable. |
| **RF-WEB-010** | Por evento | Cuando el médico registre la atención, el formulario deberá exigir diagnóstico no vacío y permitir prescripción opcional antes de enviar `POST /api/appointments/{id}/attention`. |
| **RF-WEB-011** | Por estado | Mientras una cita esté en estado `Attended`, los campos de diagnóstico y receta deberán ser de solo lectura y el botón de registro deberá estar deshabilitado. |
| **RF-WEB-012** | Ubicuo | Toda respuesta de error HTTP con formato `ProblemDetails` deberá mostrarse en una notificación tipo Toast con el mensaje en español y el código de error (`code`). |
| **RF-WEB-013** | Ubicuo | La interfaz deberá ser responsiva y adaptarse fluidamente a pantallas móviles (mínimo 360px) y de escritorio (1024px o superior). |

---

## 6. Casos Límite y Reglas de Experiencia de Usuario

- **Búsqueda sin resultados**: Mostrar un banner informativo con acción directa para registrar nuevo paciente.
- **Doble clic en botones de acción**: Deshabilitar temporalmente los botones de envío (`is-loading`) durante la petición de red para prevenir llamadas duplicadas.
- **Fechas en zona horaria local**: El selector de fecha del navegador debe inicializarse con la fecha actual de la clínica (America/Lima).
- **Cierre automático de notificaciones**: Los toasts deben cerrarse tras 4 segundos, pero pausarse si el usuario coloca el cursor encima.

---

## 7. Criterios de Éxito (Medibles)

| ID | Criterio Medible |
| :--- | :--- |
| **CE-WEB-001** | Tiempo de carga inicial del frontend en navegador inferior a 500 ms (sin bundles externos ni descargas pesadas). |
| **CE-WEB-002** | 100% de los endpoints de la API (E1 a E8) consumibles y operativos desde la interfaz web. |
| **CE-WEB-003** | Cumplimiento estricto de WCAG AA verificado en contraste de colores de textos, botones y estados. |
| **CE-WEB-004** | 0 librerías o dependencias NPM/NuGet adicionales añadidas a la solución. |
