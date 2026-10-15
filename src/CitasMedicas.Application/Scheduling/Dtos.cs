namespace CitasMedicas.Application.Scheduling;

public sealed record CreateScheduleSlotRequest(
    string Date,
    string StartTime,
    string EndTime);

public sealed record ScheduleSlotResponse(
    int Id,
    int DoctorId,
    string Date,
    string StartTime,
    string EndTime,
    string Status);
