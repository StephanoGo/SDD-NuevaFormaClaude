using System.Globalization;
using CitasMedicas.Api.Http;
using CitasMedicas.Application.Scheduling;

namespace CitasMedicas.Api.Endpoints;

public static class ScheduleEndpoints
{
    public static RouteGroupBuilder MapScheduleEndpoints(this RouteGroupBuilder group)
    {
        // E4: Crear bloque de agenda
        group.MapPost("/{doctorId:int}/schedule-slots", async (
            int doctorId,
            CreateScheduleSlotRequest request,
            ScheduleService service,
            CancellationToken ct) =>
        {
            if (!DateOnly.TryParseExact(request.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            {
                return ResultHttpMapper.ToProblemDetails(ApiErrors.ValidationFormat("La fecha debe tener formato yyyy-MM-dd."));
            }

            if (!TimeOnly.TryParseExact(request.StartTime, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var startTime))
            {
                return ResultHttpMapper.ToProblemDetails(ApiErrors.ValidationFormat("La hora de inicio debe tener formato HH:mm."));
            }

            if (!TimeOnly.TryParseExact(request.EndTime, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var endTime))
            {
                return ResultHttpMapper.ToProblemDetails(ApiErrors.ValidationFormat("La hora de fin debe tener formato HH:mm."));
            }

            var result = await service.CreateSlotAsync(doctorId, date, startTime, endTime, ct);
            return result.ToHttpResult(slot => Results.Created($"/api/doctors/{doctorId}/schedule-slots/{slot.Id}", slot));
        });

        // E5: Consultar agenda del día
        group.MapGet("/{doctorId:int}/schedule-slots", async (
            int doctorId,
            string? date,
            ScheduleService service,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(date) ||
                !DateOnly.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
            {
                return ResultHttpMapper.ToProblemDetails(ApiErrors.ValidationFormat("El parámetro 'date' es obligatorio y debe tener formato yyyy-MM-dd."));
            }

            var slots = await service.GetDoctorScheduleAsync(doctorId, parsedDate, ct);
            return Results.Ok(slots);
        });

        return group;
    }
}
