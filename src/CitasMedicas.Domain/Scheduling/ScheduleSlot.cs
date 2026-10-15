using CitasMedicas.Domain.Appointments;
using CitasMedicas.Domain.Common;
using CitasMedicas.Domain.Doctors;
using CitasMedicas.Domain.Patients;

namespace CitasMedicas.Domain.Scheduling;

public sealed class ScheduleSlot
{
    public int Id { get; private set; }
    public int DoctorId { get; private set; }
    public DateOnly Date { get; private set; }
    public TimeRange TimeRange { get; private set; }
    public ScheduleSlotStatus Status { get; private set; }

    public TimeOnly StartTime => TimeRange.Start;
    public TimeOnly EndTime => TimeRange.End;

    private ScheduleSlot(int id, int doctorId, DateOnly date, TimeRange timeRange, ScheduleSlotStatus status)
    {
        Id = id;
        DoctorId = doctorId;
        Date = date;
        TimeRange = timeRange;
        Status = status;
    }

    public static Result<ScheduleSlot> Create(
        Doctor doctor,
        DateOnly date,
        TimeRange timeRange,
        DateTime now,
        IEnumerable<ScheduleSlot> existingSlots)
    {
        if (!doctor.IsActive)
            return DoctorErrors.NotAvailable;

        var nowDate = DateOnly.FromDateTime(now);
        var nowTime = TimeOnly.FromDateTime(now);

        if (date < nowDate || (date == nowDate && timeRange.Start <= nowTime))
            return SchedulingErrors.InPast;

        if (existingSlots.Any(s => s.OverlapsWith(doctor.Id, date, timeRange)))
            return SchedulingErrors.Overlap;

        return new ScheduleSlot(0, doctor.Id, date, timeRange, ScheduleSlotStatus.Available);
    }

    public bool OverlapsWith(ScheduleSlot other)
    {
        return OverlapsWith(other.DoctorId, other.Date, other.TimeRange);
    }

    public bool OverlapsWith(int doctorId, DateOnly date, TimeRange timeRange)
    {
        return DoctorId == doctorId && Date == date && TimeRange.Overlaps(timeRange);
    }

    public Result<Appointment> Reserve(Doctor doctor, Patient patient, string? reason, DateTime now)
    {
        if (Status != ScheduleSlotStatus.Available)
            return SchedulingErrors.SlotNotAvailable;

        if (!doctor.IsActive)
            return SchedulingErrors.SlotNotAvailable;

        var nowDate = DateOnly.FromDateTime(now);
        var nowTime = TimeOnly.FromDateTime(now);

        if (Date < nowDate || (Date == nowDate && TimeRange.Start <= nowTime))
            return SchedulingErrors.SlotNotAvailable;

        if (string.IsNullOrWhiteSpace(reason))
            return AppointmentErrors.ReasonRequired;

        Status = ScheduleSlotStatus.Reserved;
        return Appointment.Create(Id, patient.Id, reason.Trim());
    }

    internal void AssignId(int id)
    {
        if (Id != 0)
            throw new InvalidOperationException("Entity already has an Id assigned.");
        if (id <= 0)
            throw new ArgumentOutOfRangeException(nameof(id), "Id must be greater than zero.");
        Id = id;
    }

    internal static ScheduleSlot Rehydrate(int id, int doctorId, DateOnly date, TimeOnly startTime, TimeOnly endTime, ScheduleSlotStatus status)
    {
        var timeRange = TimeRange.Create(startTime, endTime).Value;
        return new ScheduleSlot(id, doctorId, date, timeRange, status);
    }
}
