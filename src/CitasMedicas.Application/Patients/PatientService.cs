using CitasMedicas.Application.Abstractions.Persistence;
using CitasMedicas.Domain.Common;
using CitasMedicas.Domain.Patients;

namespace CitasMedicas.Application.Patients;

public sealed class PatientService
{
    private readonly IPatientRepository _patientRepository;

    public PatientService(IPatientRepository patientRepository)
    {
        _patientRepository = patientRepository;
    }

    public async Task<Result<PatientResponse>> RegisterAsync(RegisterPatientRequest request, CancellationToken cancellationToken = default)
    {
        var dniTrimmed = request.Dni?.Trim();
        if (!string.IsNullOrEmpty(dniTrimmed))
        {
            var existing = await _patientRepository.GetByDniAsync(dniTrimmed, cancellationToken);
            if (existing is not null)
            {
                return PatientErrors.DniDuplicate;
            }
        }

        var createResult = Patient.Create(request.Dni, request.FirstName, request.LastName, request.Phone, request.Email);
        if (createResult.IsFailure)
        {
            return createResult.Error;
        }

        var addResult = await _patientRepository.AddAsync(createResult.Value, cancellationToken);
        if (addResult.IsFailure)
        {
            return addResult.Error;
        }

        var p = addResult.Value;
        return new PatientResponse(p.Id, p.Dni.Value, p.FirstName, p.LastName, p.Phone, p.Email?.Value);
    }

    public async Task<IReadOnlyList<PatientResponse>> SearchByDniAsync(string dni, CancellationToken cancellationToken = default)
    {
        var found = await _patientRepository.GetByDniAsync(dni.Trim(), cancellationToken);
        if (found is null)
        {
            return [];
        }

        return [new PatientResponse(found.Id, found.Dni.Value, found.FirstName, found.LastName, found.Phone, found.Email?.Value)];
    }

    public async Task<Result<IReadOnlyList<PatientResponse>>> SearchByLastNameAsync(string lastName, CancellationToken cancellationToken = default)
    {
        var trimmed = lastName?.Trim() ?? string.Empty;
        if (trimmed.Length < 2)
        {
            return PatientErrors.SearchCriteriaTooShort;
        }

        var results = await _patientRepository.SearchByLastNameAsync(trimmed, cancellationToken);
        var responses = results
            .Select(p => new PatientResponse(p.Id, p.Dni.Value, p.FirstName, p.LastName, p.Phone, p.Email?.Value))
            .ToList();

        return Result<IReadOnlyList<PatientResponse>>.Success(responses);
    }
}
