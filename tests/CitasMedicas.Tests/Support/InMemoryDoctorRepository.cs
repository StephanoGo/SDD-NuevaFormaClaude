using CitasMedicas.Application.Abstractions.Persistence;
using CitasMedicas.Application.Catalogs;
using CitasMedicas.Domain.Common;
using CitasMedicas.Domain.Doctors;

namespace CitasMedicas.Infrastructure.Persistence.InMemory;

public sealed class InMemoryDoctorRepository : IDoctorRepository
{
    private readonly Dictionary<int, Doctor> _doctors;
    private int _nextId;

    public InMemoryDoctorRepository(IEnumerable<Doctor>? doctors = null)
    {
        _doctors = (doctors ?? []).ToDictionary(d => d.Id);
        _nextId = _doctors.Keys.DefaultIfEmpty(0).Max() + 1;
    }

    public Task<Doctor?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        if (_doctors.TryGetValue(id, out var doctor))
        {
            var copy = Doctor.Rehydrate(doctor.Id, doctor.LicenseNumber, doctor.FirstName, doctor.LastName, doctor.Specialty, doctor.IsActive);
            return Task.FromResult<Doctor?>(copy);
        }
        return Task.FromResult<Doctor?>(null);
    }

    public Task<IReadOnlyList<DoctorCatalogResponse>> ListAsync(bool includeInactive, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<DoctorCatalogResponse> result = _doctors.Values
            .Where(d => includeInactive || d.IsActive)
            .Select(d => new DoctorCatalogResponse(d.Id, d.LicenseNumber, d.FirstName, d.LastName, 1, d.Specialty, d.IsActive))
            .ToList();
        return Task.FromResult(result);
    }

    public Task<Result<DoctorCatalogResponse>> AddAsync(DoctorWriteRequest request, CancellationToken cancellationToken = default)
    {
        var doctor = Doctor.Rehydrate(_nextId++, request.LicenseNumber!, request.FirstName!, request.LastName!, "Test specialty", true);
        _doctors.Add(doctor.Id, doctor);
        return Task.FromResult<Result<DoctorCatalogResponse>>(new DoctorCatalogResponse(doctor.Id, doctor.LicenseNumber, doctor.FirstName, doctor.LastName, request.SpecialtyId!.Value, doctor.Specialty, doctor.IsActive));
    }

    public Task<Result<DoctorCatalogResponse>> UpdateAsync(int id, DoctorWriteRequest request, CancellationToken cancellationToken = default)
    {
        if (!_doctors.TryGetValue(id, out var current))
            return Task.FromResult<Result<DoctorCatalogResponse>>(Error.NotFound("DOCTOR_NOT_FOUND", "Médico no encontrado"));
        var doctor = Doctor.Rehydrate(id, request.LicenseNumber!, request.FirstName!, request.LastName!, current.Specialty, current.IsActive);
        _doctors[id] = doctor;
        return Task.FromResult<Result<DoctorCatalogResponse>>(new DoctorCatalogResponse(id, doctor.LicenseNumber, doctor.FirstName, doctor.LastName, request.SpecialtyId!.Value, doctor.Specialty, doctor.IsActive));
    }

    public Task<Result> DeactivateAsync(int id, CancellationToken cancellationToken = default)
    {
        if (!_doctors.TryGetValue(id, out var current))
            return Task.FromResult<Result>(Error.NotFound("DOCTOR_NOT_FOUND", "Médico no encontrado"));
        _doctors[id] = Doctor.Rehydrate(id, current.LicenseNumber, current.FirstName, current.LastName, current.Specialty, false);
        return Task.FromResult(Result.Success());
    }
}
