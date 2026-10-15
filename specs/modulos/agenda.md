# Módulo: Agenda

**Estado:** implementado. **Origen:** HU-02 / RF-009–014 de la entrega Core.

## Objetivo y alcance

Crear y consultar bloques horarios de médicos. Reservar un bloque pertenece al módulo Citas.

## Reglas y aceptación

- Crear bloques solo para médicos existentes y activos, con hora inicial anterior a la final.
- Rechazar fechas u horas pasadas y solapamientos del mismo médico y día; bloques contiguos son válidos.
- Consultar bloques por médico y fecha, ordenados por hora e indicando disponibilidad.
- La interfaz permite elegir médico y fecha, crear bloques y presentar estados vacíos y errores.
- Evaluar zona horaria según la regla vigente de la clínica (`America/Lima`).

## Requisitos y criterios

- **RF-AGE-01**: Crear bloque solo para médico activo, con intervalo válido y no pasado.
- **RF-AGE-02**: Impedir solapamientos del mismo médico y día; permitir intervalos contiguos.
- **RF-AGE-03**: Consultar bloques por médico y fecha en orden de inicio, mostrando disponibilidad.

| Dado | Cuando | Entonces |
| --- | --- | --- |
| Médico activo sin bloque solapado | Creo un intervalo futuro válido | Se crea como Disponible (RF-AGE-01, 02) |
| Hay un bloque solapado del médico y día | Intento crear otro | Se rechaza; si solo son contiguos, se acepta (RF-AGE-02) |
| Hay varios bloques en la fecha | Consulto la agenda del médico | Se devuelven ordenados con su estado (RF-AGE-03) |

**Verificación:** pruebas de rangos, fechas, solapamientos, disponibilidad y consulta; `dotnet build` y `dotnet test`.
