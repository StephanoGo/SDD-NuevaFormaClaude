# Criterios comunes de experiencia web

La programación de médicos y especialidades tiene su entrega en [`006-programacion-web`](../006-programacion-web/spec.md).

Aplican a las pantallas de todos los módulos:

- Mantener contraste de texto normal de al menos 4.5:1.
- Adaptarse a móvil desde 360 px y a escritorio.
- Validar entradas en la interfaz para orientar al usuario; el servidor conserva la validación definitiva.
- Mostrar errores API en español y conservar el código estable cuando esté disponible.
- Indicar carga, éxito, error y ausencia de resultados; impedir envíos duplicados mientras una operación está pendiente.
- Los campos de una atención concluida se muestran en solo lectura.

## Requisitos y criterios

- **RF-WEB-01**: Mantener contraste mínimo 4.5:1 y diseño usable desde 360 px.
- **RF-WEB-02**: Presentar validación, carga, éxito, error y estado vacío en las acciones de cada módulo; impedir doble envío.
- **RF-WEB-03**: Mostrar errores API con mensaje en español y código estable cuando exista.
- **RF-WEB-04**: Mostrar en solo lectura los datos de una atención concluida.

| Dado | Cuando | Entonces |
| --- | --- | --- |
| Se muestra cualquier módulo | Se usa en móvil o escritorio | El contenido sigue legible, usable y con contraste suficiente (RF-WEB-01) |
| Se envía una acción o falla una llamada API | La operación está pendiente o devuelve error | Se indica el estado, se evita el doble envío y se explica el error en español (RF-WEB-02, 03) |
| Se abre una cita atendida | Se consulta su atención | Los datos aparecen sin edición habilitada (RF-WEB-04) |
