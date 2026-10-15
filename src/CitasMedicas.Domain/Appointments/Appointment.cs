using CitasMedicas.Domain.Common;

namespace CitasMedicas.Domain.Appointments;

public sealed class Appointment
{
    public int Id { get; private set; }
    public int ScheduleSlotId { get; private set; }
    public int PatientId { get; private set; }
    public string Reason { get; private set; }
    public AppointmentStatus Status { get; private set; }
    public string? Diagnosis { get; private set; }
    public string? Prescription { get; private set; }

    private Appointment(
        int id,
        int scheduleSlotId,
        int patientId,
        string reason,
        AppointmentStatus status,
        string? diagnosis,
        string? prescription)
    {
        Id = id;
        ScheduleSlotId = scheduleSlotId;
        PatientId = patientId;
        Reason = reason;
        Status = status;
        Diagnosis = diagnosis;
        Prescription = prescription;
    }

    internal static Appointment Create(int scheduleSlotId, int patientId, string reason)
    {
        return new Appointment(
            id: 0,
            scheduleSlotId: scheduleSlotId,
            patientId: patientId,
            reason: reason,
            status: AppointmentStatus.Pending,
            diagnosis: null,
            prescription: null
        );
    }

    public Result RegisterAttention(string? diagnosis, string? prescription)
    {
        if (Status != AppointmentStatus.Pending)
            return AppointmentErrors.NotPending;

        if (string.IsNullOrWhiteSpace(diagnosis))
            return AppointmentErrors.DiagnosisRequired;

        Status = AppointmentStatus.Attended;
        Diagnosis = diagnosis.Trim();
        Prescription = string.IsNullOrWhiteSpace(prescription) ? null : prescription.Trim();

        return Result.Success();
    }

    internal void AssignId(int id)
    {
        if (Id != 0)
            throw new InvalidOperationException("Entity already has an Id assigned.");
        if (id <= 0)
            throw new ArgumentOutOfRangeException(nameof(id), "Id must be greater than zero.");
        Id = id;
    }

    internal static Appointment Rehydrate(
        int id,
        int scheduleSlotId,
        int patientId,
        string reason,
        AppointmentStatus status,
        string? diagnosis,
        string? prescription)
    {
        return new Appointment(id, scheduleSlotId, patientId, reason, status, diagnosis, prescription);
    }
}
