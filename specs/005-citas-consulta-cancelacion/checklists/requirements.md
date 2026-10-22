# Checklist de requisitos — Consulta/cancelación de citas

Las 16 comprobaciones constituyen línea base propuesta pendiente de aprobación del propietario.

| Comprobación | Estado | RF |
|---|---|---|
| IDs únicos, atómicos y observables | Pasa | RF-CIT-05–09 |
| Alcance, términos y precondiciones inequívocos | Pasa | RF-CIT-05–09 |
| Éxitos, validación, errores y concurrencia cubiertos | Pasa | RF-CIT-05–09 |
| Propiedad/invariantes de estado consistentes | Pasa | RF-CIT-06, 08 |
| Aceptación cubre todos los RF sin prescribir tecnologías | Pasa | RF-CIT-05–09 |

## Dominio / UX

- Pending es el único estado cancelable; slot liberado en la misma operación.
- Cancelled/Attended no pueden cancelarse otra vez.
- Confirmación antes de acción irreversible; feedback, error y doble envío cubiertos.
