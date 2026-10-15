# Módulo: Pacientes

**Estado:** implementado. **Origen:** HU-01 / RF-001–008 de la entrega Core.

## Objetivo y alcance

Registrar y encontrar pacientes para vincularlos a sus citas y consultar su historia clínica. La identidad se basa en DNI; el módulo no registra información de atención.

## Reglas y aceptación

- Registrar nombres, apellidos y DNI válido de ocho dígitos; teléfono es obligatorio y correo opcional debe tener formato válido.
- Rechazar DNI duplicado y datos obligatorios inválidos con errores estables.
- Buscar por DNI exacto o por apellidos, ignorando mayúsculas y tildes; ordenar por apellidos y nombres.
- Rechazar búsquedas por apellido con menos de dos caracteres.
- La interfaz valida entradas y presenta resultados, estados vacíos y errores comprensibles.

## Requisitos y criterios

- **RF-PAC-01**: Registrar paciente válido con identificador y rechazar un DNI duplicado o inválido.
- **RF-PAC-02**: Exigir nombres, apellidos y teléfono; aceptar correo vacío y validar su formato cuando se informe.
- **RF-PAC-03**: Buscar por DNI exacto y devolver vacío cuando no exista.
- **RF-PAC-04**: Buscar por apellido con al menos dos caracteres, ignorar tildes y mayúsculas, y ordenar resultados.

| Dado | Cuando | Entonces |
| --- | --- | --- |
| No existe el DNI | Registro los datos válidos | Se crea el paciente y se devuelve su ID (RF-PAC-01, 02) |
| El DNI ya pertenece a un paciente | Intento registrarlo otra vez | Se rechaza con error estable (RF-PAC-01) |
| Hay un paciente registrado | Busco por su DNI exacto | Se devuelve ese paciente; si no existe, el resultado está vacío (RF-PAC-03) |
| Hay pacientes con apellidos que coinciden sin tildes | Busco por fragmento de apellido válido | Devuelve coincidencias ordenadas; un criterio corto se rechaza (RF-PAC-04) |
| Falta un campo obligatorio o el correo informado es inválido | Registro al paciente | Se rechaza indicando el error de validación (RF-PAC-02) |

**Verificación:** pruebas de registro, validación, duplicados y búsqueda; `dotnet build` y `dotnet test`.
