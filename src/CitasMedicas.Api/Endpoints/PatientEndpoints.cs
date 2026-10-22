using CitasMedicas.Api.Http;
using CitasMedicas.Application.Patients;
using CitasMedicas.Application.Appointments;

namespace CitasMedicas.Api.Endpoints;

public static class PatientEndpoints
{
    public static RouteGroupBuilder MapPatientEndpoints(this RouteGroupBuilder group)
    {
        // E1: Registrar paciente
        group.MapPost("/", async (RegisterPatientRequest request, PatientService service, CancellationToken ct) =>
        {
            var result = await service.RegisterAsync(request, ct);
            return result.ToHttpResult(patient => Results.Created($"/api/patients/{patient.Id}", patient));
        });

        // E2 y E3: Buscar pacientes por DNI o Apellidos
        group.MapGet("/", async (string? dni, string? lastName, PatientService service, CancellationToken ct) =>
        {
            var hasDni = !string.IsNullOrWhiteSpace(dni);
            var hasLastName = !string.IsNullOrWhiteSpace(lastName);

            if ((hasDni && hasLastName) || (!hasDni && !hasLastName))
            {
                return ResultHttpMapper.ToProblemDetails(
                    ApiErrors.ValidationFormat("Se debe enviar exactamente un parámetro de consulta ('dni' o 'lastName')."));
            }

            if (hasDni)
            {
                var patients = await service.SearchByDniAsync(dni!, ct);
                return Results.Ok(patients);
            }

            var result = await service.SearchByLastNameAsync(lastName!, ct);
            return result.ToHttpResult(Results.Ok);
        });

        group.MapGet("/{patientId:int}/clinical-history", async (
            int patientId, ClinicalHistoryService service, CancellationToken ct) =>
            (await service.GetByPatientAsync(patientId, ct)).ToHttpResult(Results.Ok));

        return group;
    }
}
