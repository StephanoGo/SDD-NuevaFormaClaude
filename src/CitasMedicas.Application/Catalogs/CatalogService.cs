using CitasMedicas.Application.Abstractions.Persistence;
using CitasMedicas.Domain.Common;

namespace CitasMedicas.Application.Catalogs;

public sealed class CatalogService(IDoctorRepository doctors, ISpecialtyRepository specialties)
{
    public Task<IReadOnlyList<DoctorCatalogResponse>> ListDoctorsAsync(bool includeInactive, CancellationToken ct = default) =>
        doctors.ListAsync(includeInactive, ct);

    public Task<IReadOnlyList<SpecialtyResponse>> ListSpecialtiesAsync(bool includeInactive, CancellationToken ct = default) =>
        specialties.ListAsync(includeInactive, ct);

    public Task<Result<DoctorCatalogResponse>> AddDoctorAsync(DoctorWriteRequest request, CancellationToken ct = default) =>
        ValidateDoctor(request) is { } error ? Task.FromResult<Result<DoctorCatalogResponse>>(error) :
        doctors.AddAsync(Normalize(request), ct);

    public Task<Result<DoctorCatalogResponse>> UpdateDoctorAsync(int id, DoctorWriteRequest request, CancellationToken ct = default) =>
        ValidateDoctor(request) is { } error ? Task.FromResult<Result<DoctorCatalogResponse>>(error) :
        doctors.UpdateAsync(id, Normalize(request), ct);

    public Task<Result> DeactivateDoctorAsync(int id, CancellationToken ct = default) => doctors.DeactivateAsync(id, ct);

    public Task<Result<SpecialtyResponse>> AddSpecialtyAsync(SpecialtyWriteRequest request, CancellationToken ct = default) =>
        string.IsNullOrWhiteSpace(request.Name)
            ? Task.FromResult<Result<SpecialtyResponse>>(CatalogErrors.SpecialtyNameRequired)
            : specialties.AddAsync(request with { Name = request.Name.Trim() }, ct);

    public Task<Result<SpecialtyResponse>> UpdateSpecialtyAsync(int id, SpecialtyWriteRequest request, CancellationToken ct = default) =>
        string.IsNullOrWhiteSpace(request.Name)
            ? Task.FromResult<Result<SpecialtyResponse>>(CatalogErrors.SpecialtyNameRequired)
            : specialties.UpdateAsync(id, request with { Name = request.Name.Trim() }, ct);

    public Task<Result> DeactivateSpecialtyAsync(int id, CancellationToken ct = default) => specialties.DeactivateAsync(id, ct);

    private static Error? ValidateDoctor(DoctorWriteRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.LicenseNumber)) return CatalogErrors.DoctorLicenseRequired;
        if (string.IsNullOrWhiteSpace(request.FirstName)) return CatalogErrors.DoctorFirstNameRequired;
        if (string.IsNullOrWhiteSpace(request.LastName)) return CatalogErrors.DoctorLastNameRequired;
        if (request.SpecialtyId is null or <= 0) return CatalogErrors.DoctorSpecialtyRequired;
        return null;
    }

    private static DoctorWriteRequest Normalize(DoctorWriteRequest request) => request with
    {
        LicenseNumber = request.LicenseNumber!.Trim(),
        FirstName = request.FirstName!.Trim(),
        LastName = request.LastName!.Trim()
    };
}
