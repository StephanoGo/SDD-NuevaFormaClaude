using CitasMedicas.Application.Abstractions.Persistence;
using CitasMedicas.Application.Common;
using CitasMedicas.Domain.Common;
using CitasMedicas.Domain.Doctors;
using CitasMedicas.Domain.Scheduling;

namespace CitasMedicas.Application.Scheduling;

public sealed class ScheduleService
{
    private readonly IDoctorRepository _doctorRepository;
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly ClinicClock _clock;

    public ScheduleService(
        IDoctorRepository doctorRepository,
        IAppointmentRepository appointmentRepository,
        ClinicClock clock)
    {
        _doctorRepository = doctorRepository;
        _appointmentRepository = appointmentRepository;
        _clock = clock;
    }

    public async Task<Result<ScheduleSlotResponse>> CreateSlotAsync(
        int doctorId,
        DateOnly date,
        TimeOnly startTime,
        TimeOnly endTime,
        CancellationToken cancellationToken = default)
    {
        var doctor = await _doctorRepository.GetByIdAsync(doctorId, cancellationToken);
        if (doctor is null || !doctor.IsActive)
        {
            return DoctorErrors.NotAvailable;
        }

        var timeRangeResult = TimeRange.Create(startTime, endTime);
        if (timeRangeResult.IsFailure)
        {
            return timeRangeResult.Error;
        }

        var existingSlots = await _appointmentRepository.GetSlotsByDoctorAndDateAsync(doctorId, date, cancellationToken);

        var slotResult = ScheduleSlot.Create(doctor, date, timeRangeResult.Value, _clock.Now, existingSlots);
        if (slotResult.IsFailure)
        {
            return slotResult.Error;
        }

        var addResult = await _appointmentRepository.AddSlotAsync(slotResult.Value, cancellationToken);
        if (addResult.IsFailure)
        {
            return addResult.Error;
        }

        var s = addResult.Value;
        return new ScheduleSlotResponse(
            s.Id,
            s.DoctorId,
            s.Date.ToString("yyyy-MM-dd"),
            s.StartTime.ToString("HH:mm"),
            s.EndTime.ToString("HH:mm"),
            s.Status.ToString());
    }

    public async Task<IReadOnlyList<ScheduleSlotResponse>> GetDoctorScheduleAsync(
        int doctorId,
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        var slots = await _appointmentRepository.GetSlotsByDoctorAndDateAsync(doctorId, date, cancellationToken);

        return slots
            .Select(s => new ScheduleSlotResponse(
                s.Id,
                s.DoctorId,
                s.Date.ToString("yyyy-MM-dd"),
                s.StartTime.ToString("HH:mm"),
                s.EndTime.ToString("HH:mm"),
                s.Status.ToString()))
            .ToList();
    }
}
