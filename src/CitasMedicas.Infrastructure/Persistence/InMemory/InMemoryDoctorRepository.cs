using CitasMedicas.Application.Abstractions.Persistence;
using CitasMedicas.Domain.Doctors;
using CitasMedicas.Infrastructure.Seed;

namespace CitasMedicas.Infrastructure.Persistence.InMemory;

public sealed class InMemoryDoctorRepository : IDoctorRepository
{
    private readonly Dictionary<int, Doctor> _doctors = DoctorSeed.GetDoctors().ToDictionary(d => d.Id);

    public Task<Doctor?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        if (_doctors.TryGetValue(id, out var doctor))
        {
            var copy = Doctor.Rehydrate(doctor.Id, doctor.LicenseNumber, doctor.FirstName, doctor.LastName, doctor.Specialty, doctor.IsActive);
            return Task.FromResult<Doctor?>(copy);
        }

        return Task.FromResult<Doctor?>(null);
    }
}
