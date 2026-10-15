using CitasMedicas.Domain.Doctors;

namespace CitasMedicas.Application.Abstractions.Persistence;

public interface IDoctorRepository
{
    Task<Doctor?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
}
