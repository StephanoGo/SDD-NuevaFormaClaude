namespace CitasMedicas.Application.Appointments;

public sealed record ReserveAppointmentRequest(
    int ScheduleSlotId,
    int PatientId,
    string Reason);

public sealed record RegisterAttentionRequest(
    string Diagnosis,
    string? Prescription);

public sealed record PatientSummaryResponse(
    int Id,
    string Dni,
    string FullName);

public sealed record DoctorSummaryResponse(
    int Id,
    string FullName,
    string Specialty);

public sealed record SlotSummaryResponse(
    int Id,
    string Date,
    string StartTime,
    string EndTime);

public sealed record AppointmentResponse(
    int Id,
    string Status,
    string Reason,
    string? Diagnosis,
    string? Prescription,
    PatientSummaryResponse Patient,
    DoctorSummaryResponse Doctor,
    SlotSummaryResponse Slot);

public sealed record ClinicalHistoryEntryResponse(
    int AppointmentId,
    string Reason,
    string Diagnosis,
    string? Prescription,
    DoctorSummaryResponse Doctor,
    SlotSummaryResponse Slot);
