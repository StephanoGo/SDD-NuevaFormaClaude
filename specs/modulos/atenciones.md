# Módulo: Atenciones

**Estado:** registro básico implementado dentro de Citas. **Origen:** HU-04 / RF-022–026 de la entrega Core.

## Objetivo y alcance

Registrar el resultado clínico de una cita pendiente mediante diagnóstico obligatorio y receta opcional. La Historia Clínica reunirá estos registros; no se duplican en otro módulo.

## Reglas y aceptación

- Solo una cita pendiente admite el registro de atención.
- Diagnóstico vacío se rechaza; receta vacía se trata como ausente.
- Al guardar, la cita pasa a atendida y conserva sus datos clínicos.
- Una atención concluida es inmutable y no puede registrarse por segunda vez.
- La interfaz carga la cita, presenta sus datos y deja el registro concluido en solo lectura.

## Requisitos y criterios

- **RF-ATT-01**: Registrar diagnóstico obligatorio y receta opcional solo para cita Pendiente.
- **RF-ATT-02**: Guardar datos clínicos y cambiar la cita a Atendida en una operación protegida contra concurrencia.
- **RF-ATT-03**: Impedir doble registro y modificación posterior de la atención concluida.
- **RF-ATT-04**: Consultar el registro junto con el paciente, médico, fecha y horario asociados.

| Dado | Cuando | Entonces |
| --- | --- | --- |
| La cita está Pendiente | Registro diagnóstico y receta opcional | Se guarda y pasa a Atendida (RF-ATT-01, 02) |
| Falta el diagnóstico o la cita no está Pendiente | Intento registrar atención | Se rechaza sin cambiar los datos existentes (RF-ATT-01, 03) |
| La atención ya está concluida | Intento registrarla de nuevo | Se rechaza y conserva el registro original (RF-ATT-03) |
| Existe una cita atendida | La consulto por ID | Recibo el registro clínico y sus datos relacionados (RF-ATT-04) |

**Verificación:** pruebas de transición, validación, inmutabilidad y errores; `dotnet build` y `dotnet test`.

**Límite conocido:** la atención todavía forma parte del modelo `Appointment`; separarla técnicamente requiere una decisión de diseño respaldada por una spec.
