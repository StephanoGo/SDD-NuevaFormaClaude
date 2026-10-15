using CitasMedicas.Application.Abstractions.Persistence;
using CitasMedicas.Domain.Common;
using CitasMedicas.Domain.Patients;

namespace CitasMedicas.Infrastructure.Persistence.InMemory;

public sealed class InMemoryPatientRepository : IPatientRepository
{
    private readonly Lock _lock = new();
    private readonly List<Patient> _patients = [];
    private int _nextId = 1;

    public Task<Result<Patient>> AddAsync(Patient patient, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (_patients.Any(p => p.Dni.Value == patient.Dni.Value))
            {
                return Task.FromResult<Result<Patient>>(PatientErrors.DniDuplicate);
            }

            var id = _nextId++;
            var stored = Patient.Rehydrate(
                id,
                patient.Dni.Value,
                patient.FirstName,
                patient.LastName,
                patient.Phone,
                patient.Email?.Value);

            _patients.Add(stored);

            var copy = Clone(stored);
            return Task.FromResult<Result<Patient>>(copy);
        }
    }

    public Task<Patient?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var found = _patients.FirstOrDefault(p => p.Id == id);
            return Task.FromResult(found is null ? null : Clone(found));
        }
    }

    public Task<Patient?> GetByDniAsync(string dni, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var found = _patients.FirstOrDefault(p => p.Dni.Value == dni);
            return Task.FromResult(found is null ? null : Clone(found));
        }
    }

    public Task<IReadOnlyList<Patient>> SearchByLastNameAsync(string text, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var normalizedQuery = TextNormalizer.ForSearch(text);
            var results = _patients
                .Where(p => TextNormalizer.ForSearch(p.LastName).Contains(normalizedQuery))
                .OrderBy(p => TextNormalizer.ForSearch(p.LastName), StringComparer.Ordinal)
                .ThenBy(p => TextNormalizer.ForSearch(p.FirstName), StringComparer.Ordinal)
                .Select(Clone)
                .ToList();

            return Task.FromResult<IReadOnlyList<Patient>>(results);
        }
    }

    private static Patient Clone(Patient source)
    {
        return Patient.Rehydrate(
            source.Id,
            source.Dni.Value,
            source.FirstName,
            source.LastName,
            source.Phone,
            source.Email?.Value);
    }
}
