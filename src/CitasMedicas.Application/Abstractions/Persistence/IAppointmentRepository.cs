using CitasMedicas.Domain.Appointments;
using CitasMedicas.Domain.Common;
using CitasMedicas.Domain.Scheduling;

namespace CitasMedicas.Application.Abstractions.Persistence;

public interface IAppointmentRepository
{
    Task<Result<ScheduleSlot>> AddSlotAsync(ScheduleSlot slot, CancellationToken cancellationToken = default);
    Task<ScheduleSlot?> GetSlotByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ScheduleSlot>> GetSlotsByDoctorAndDateAsync(int doctorId, DateOnly date, CancellationToken cancellationToken = default);
    Task<Result<Appointment>> ReserveAsync(ScheduleSlot slot, Appointment appointment, CancellationToken cancellationToken = default);
    Task<Appointment?> GetAppointmentByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<Appointment>> RegisterAttentionAsync(Appointment appointment, CancellationToken cancellationToken = default);
}
