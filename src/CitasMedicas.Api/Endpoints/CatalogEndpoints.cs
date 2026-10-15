using CitasMedicas.Api.Http;
using CitasMedicas.Application.Catalogs;
using CitasMedicas.Domain.Common;

namespace CitasMedicas.Api.Endpoints;

public static class CatalogEndpoints
{
    public static RouteGroupBuilder MapCatalogEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/specialties", async (string? includeInactive, CatalogService service, CancellationToken ct) =>
        {
            if (!TryParseIncludeInactive(includeInactive, out var include, out var error))
                return ResultHttpMapper.ToProblemDetails(error!);
            return Results.Ok(await service.ListSpecialtiesAsync(include, ct));
        });
        group.MapPost("/specialties", async (SpecialtyWriteRequest request, CatalogService service, CancellationToken ct) =>
        {
            var result = await service.AddSpecialtyAsync(request, ct);
            return result.ToHttpResult(value => Results.Created($"/api/specialties/{value.Id}", value));
        });
        group.MapPut("/specialties/{specialtyId:int}", async (int specialtyId, SpecialtyWriteRequest request, CatalogService service, CancellationToken ct) =>
            (await service.UpdateSpecialtyAsync(specialtyId, request, ct)).ToHttpResult());
        group.MapPost("/specialties/{specialtyId:int}/deactivation", async (int specialtyId, CatalogService service, CancellationToken ct) =>
            ToNoContent(await service.DeactivateSpecialtyAsync(specialtyId, ct)));

        group.MapGet("/doctors", async (string? includeInactive, CatalogService service, CancellationToken ct) =>
        {
            if (!TryParseIncludeInactive(includeInactive, out var include, out var error))
                return ResultHttpMapper.ToProblemDetails(error!);
            return Results.Ok(await service.ListDoctorsAsync(include, ct));
        });
        group.MapPost("/doctors", async (DoctorWriteRequest request, CatalogService service, CancellationToken ct) =>
        {
            var result = await service.AddDoctorAsync(request, ct);
            return result.ToHttpResult(value => Results.Created($"/api/doctors/{value.Id}", value));
        });
        group.MapPut("/doctors/{doctorId:int}", async (int doctorId, DoctorWriteRequest request, CatalogService service, CancellationToken ct) =>
            (await service.UpdateDoctorAsync(doctorId, request, ct)).ToHttpResult());
        group.MapPost("/doctors/{doctorId:int}/deactivation", async (int doctorId, CatalogService service, CancellationToken ct) =>
            ToNoContent(await service.DeactivateDoctorAsync(doctorId, ct)));

        return group;
    }

    private static bool TryParseIncludeInactive(string? value, out bool include, out Error? error)
    {
        if (value is null)
        {
            include = false;
            error = null;
            return true;
        }
        if (bool.TryParse(value, out include))
        {
            error = null;
            return true;
        }
        error = ApiErrors.ValidationFormat("includeInactive debe ser true o false.");
        return false;
    }

    private static IResult ToNoContent(Result result) =>
        result.IsSuccess ? Results.NoContent() : ResultHttpMapper.ToProblemDetails(result.Error);
}
