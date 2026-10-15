using CitasMedicas.Domain.Doctors;
using CitasMedicas.Domain.Common;
using CitasMedicas.Application.Catalogs;

namespace CitasMedicas.Application.Abstractions.Persistence;

public interface IDoctorRepository
{
    Task<Doctor?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DoctorCatalogResponse>> ListAsync(bool includeInactive, CancellationToken cancellationToken = default);
    Task<Result<DoctorCatalogResponse>> AddAsync(DoctorWriteRequest request, CancellationToken cancellationToken = default);
    Task<Result<DoctorCatalogResponse>> UpdateAsync(int id, DoctorWriteRequest request, CancellationToken cancellationToken = default);
    Task<Result> DeactivateAsync(int id, CancellationToken cancellationToken = default);
}
