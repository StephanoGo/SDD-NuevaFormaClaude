# Contrato de consulta de historia clínica

## HTTP

`GET /api/patients/{patientId:int}/clinical-history`.
- `200`: JSON array `{ appointmentId, reason, diagnosis, prescription, doctor: { id, fullName, specialty }, slot: { id, date, startTime, endTime } }`, solo atendidas, orden descendente.
- `404 PATIENT_NOT_FOUND`: ID inexistente.
- `200 []`: paciente existente sin atenciones.

## Repositorio

Lectura async `GetClinicalHistoryAsync(patientId, cancellationToken)` con lista de DTOs; la consulta no escribe y excluye pendientes/canceladas. Contrato existente de `IPatientRepository.GetByIdAsync` valida existencia.
