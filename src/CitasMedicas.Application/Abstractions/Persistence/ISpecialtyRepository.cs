using CitasMedicas.Application.Catalogs;
using CitasMedicas.Domain.Common;

namespace CitasMedicas.Application.Abstractions.Persistence;

public interface ISpecialtyRepository
{
    Task<IReadOnlyList<SpecialtyResponse>> ListAsync(bool includeInactive, CancellationToken cancellationToken = default);
    Task<Result<SpecialtyResponse>> AddAsync(SpecialtyWriteRequest request, CancellationToken cancellationToken = default);
    Task<Result<SpecialtyResponse>> UpdateAsync(int id, SpecialtyWriteRequest request, CancellationToken cancellationToken = default);
    Task<Result> DeactivateAsync(int id, CancellationToken cancellationToken = default);
}
