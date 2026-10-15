using CitasMedicas.Domain.Common;
using CitasMedicas.Domain.Patients;

namespace CitasMedicas.Application.Abstractions.Persistence;

public interface IPatientRepository
{
    Task<Result<Patient>> AddAsync(Patient patient, CancellationToken cancellationToken = default);
    Task<Patient?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Patient?> GetByDniAsync(string dni, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Patient>> SearchByLastNameAsync(string text, CancellationToken cancellationToken = default);
}
