# Plan: Consulta del historial clínico

## Diseño

La historia es una consulta de lectura dentro de la capa Application: `ClinicalHistoryService` valida existencia del paciente con `IPatientRepository` y consulta los agregados relacionados por un método de lectura del repositorio de citas/atenciones. La implementación SQL realiza un join de appointments, attentions, slots, doctors y specialties, filtra por paciente y estado Attended, ordena por fecha/hora descendente y proyecta el DTO de historia. La API expone `GET /api/patients/{patientId}/clinical-history`. La web añade una ficha de historia y reutiliza la búsqueda existente de pacientes.

## Invariantes y persistencia

No hay nuevas tablas, columnas ni escrituras: motivo y diagnóstico/receta permanecen propiedad de Citas/Atenciones; fecha/hora procede del bloque. El acceso sigue el modelo actual sin autenticación. Paciente inexistente devuelve `PATIENT_NOT_FOUND`; paciente válido sin atenciones devuelve `200 []`.

## Verificación

Pruebas de servicio/repositorio y HTTP: orden descendente, filtro de atendidas, respuesta vacía y paciente inexistente; revisión de render web y `dotnet build`/`dotnet test`.
