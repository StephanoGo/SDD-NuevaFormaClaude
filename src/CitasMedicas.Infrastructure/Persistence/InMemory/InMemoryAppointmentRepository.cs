using CitasMedicas.Application.Abstractions.Persistence;
using CitasMedicas.Domain.Appointments;
using CitasMedicas.Domain.Common;
using CitasMedicas.Domain.Scheduling;

namespace CitasMedicas.Infrastructure.Persistence.InMemory;

public sealed class InMemoryAppointmentRepository : IAppointmentRepository
{
    private readonly Lock _lock = new();
    private readonly List<ScheduleSlot> _slots = [];
    private readonly List<Appointment> _appointments = [];
    private int _nextSlotId = 1;
    private int _nextAppointmentId = 1;

    public Task<Result<ScheduleSlot>> AddSlotAsync(ScheduleSlot slot, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var doctorSlotsOnDate = _slots.Where(s => s.DoctorId == slot.DoctorId && s.Date == slot.Date);
            if (doctorSlotsOnDate.Any(s => s.OverlapsWith(slot)))
            {
                return Task.FromResult<Result<ScheduleSlot>>(SchedulingErrors.Overlap);
            }

            var id = _nextSlotId++;
            var storedSlot = ScheduleSlot.Rehydrate(
                id,
                slot.DoctorId,
                slot.Date,
                slot.StartTime,
                slot.EndTime,
                slot.Status);

            _slots.Add(storedSlot);
            return Task.FromResult<Result<ScheduleSlot>>(Clone(storedSlot));
        }
    }

    public Task<ScheduleSlot?> GetSlotByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var found = _slots.FirstOrDefault(s => s.Id == id);
            return Task.FromResult(found is null ? null : Clone(found));
        }
    }

    public Task<IReadOnlyList<ScheduleSlot>> GetSlotsByDoctorAndDateAsync(int doctorId, DateOnly date, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var results = _slots
                .Where(s => s.DoctorId == doctorId && s.Date == date)
                .OrderBy(s => s.StartTime)
                .Select(Clone)
                .ToList();

            return Task.FromResult<IReadOnlyList<ScheduleSlot>>(results);
        }
    }

    public Task<Result<Appointment>> ReserveAsync(ScheduleSlot slot, Appointment appointment, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var slotIndex = _slots.FindIndex(s => s.Id == slot.Id);
            if (slotIndex < 0 || _slots[slotIndex].Status != ScheduleSlotStatus.Available)
            {
                return Task.FromResult<Result<Appointment>>(SchedulingErrors.SlotNotAvailable);
            }

            var currentSlot = _slots[slotIndex];
            var reservedSlot = ScheduleSlot.Rehydrate(
                currentSlot.Id,
                currentSlot.DoctorId,
                currentSlot.Date,
                currentSlot.StartTime,
                currentSlot.EndTime,
                ScheduleSlotStatus.Reserved);

            _slots[slotIndex] = reservedSlot;

            var appointmentId = _nextAppointmentId++;
            var storedAppointment = Appointment.Rehydrate(
                appointmentId,
                reservedSlot.Id,
                appointment.PatientId,
                appointment.Reason,
                AppointmentStatus.Pending,
                diagnosis: null,
                prescription: null);

            _appointments.Add(storedAppointment);
            return Task.FromResult<Result<Appointment>>(Clone(storedAppointment));
        }
    }

    public Task<Appointment?> GetAppointmentByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var found = _appointments.FirstOrDefault(a => a.Id == id);
            return Task.FromResult(found is null ? null : Clone(found));
        }
    }

    public Task<Result<Appointment>> RegisterAttentionAsync(Appointment appointment, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var appointmentIndex = _appointments.FindIndex(a => a.Id == appointment.Id);
            if (appointmentIndex < 0 || _appointments[appointmentIndex].Status != AppointmentStatus.Pending)
            {
                return Task.FromResult<Result<Appointment>>(AppointmentErrors.NotPending);
            }

            var current = _appointments[appointmentIndex];
            var updatedAppointment = Appointment.Rehydrate(
                current.Id,
                current.ScheduleSlotId,
                current.PatientId,
                current.Reason,
                AppointmentStatus.Attended,
                appointment.Diagnosis,
                appointment.Prescription);

            _appointments[appointmentIndex] = updatedAppointment;
            return Task.FromResult<Result<Appointment>>(Clone(updatedAppointment));
        }
    }

    private static ScheduleSlot Clone(ScheduleSlot source)
    {
        return ScheduleSlot.Rehydrate(
            source.Id,
            source.DoctorId,
            source.Date,
            source.StartTime,
            source.EndTime,
            source.Status);
    }

    private static Appointment Clone(Appointment source)
    {
        return Appointment.Rehydrate(
            source.Id,
            source.ScheduleSlotId,
            source.PatientId,
            source.Reason,
            source.Status,
            source.Diagnosis,
            source.Prescription);
    }
}
