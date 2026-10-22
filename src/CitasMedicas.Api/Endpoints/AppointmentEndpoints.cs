using CitasMedicas.Api.Http;
using CitasMedicas.Application.Appointments;
using System.Globalization;

namespace CitasMedicas.Api.Endpoints;

public static class AppointmentEndpoints
{
    public static RouteGroupBuilder MapAppointmentEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", async (string? patientId, string? doctorId, string? date, AppointmentService service, CancellationToken ct) =>
        {
            int? parsedPatientId = null;
            int? parsedDoctorId = null;
            DateOnly? parsedDate = null;
            if (patientId is not null)
            {
                if (!int.TryParse(patientId, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed) || parsed <= 0)
                    return Results.Problem(statusCode: 400, title: "Formato inválido", detail: "patientId debe ser un entero positivo.", extensions: new Dictionary<string, object?> { ["code"] = "VALIDATION_FORMAT" });
                parsedPatientId = parsed;
            }
            if (doctorId is not null)
            {
                if (!int.TryParse(doctorId, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed) || parsed <= 0)
                    return Results.Problem(statusCode: 400, title: "Formato inválido", detail: "doctorId debe ser un entero positivo.", extensions: new Dictionary<string, object?> { ["code"] = "VALIDATION_FORMAT" });
                parsedDoctorId = parsed;
            }
            if (date is not null)
            {
                if (!DateOnly.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
                    return Results.Problem(statusCode: 400, title: "Formato inválido", detail: "La fecha debe tener formato yyyy-MM-dd.", extensions: new Dictionary<string, object?> { ["code"] = "VALIDATION_FORMAT" });
                parsedDate = parsed;
            }
            return (await service.ListAsync(parsedPatientId, parsedDoctorId, parsedDate, ct)).ToHttpResult(Results.Ok);
        });

        // E6: Reservar cita
        group.MapPost("/", async (
            ReserveAppointmentRequest request,
            AppointmentService service,
            CancellationToken ct) =>
        {
            var result = await service.ReserveAsync(request.ScheduleSlotId, request.PatientId, request.Reason, ct);
            return result.ToHttpResult(appt => Results.Created($"/api/appointments/{appt.Id}", appt));
        });

        // E7: Registrar atención
        group.MapPost("/{appointmentId:int}/attention", async (
            int appointmentId,
            RegisterAttentionRequest request,
            AppointmentService service,
            CancellationToken ct) =>
        {
            var result = await service.RegisterAttentionAsync(appointmentId, request.Diagnosis, request.Prescription, ct);
            return result.ToHttpResult(Results.Ok);
        });

        group.MapPost("/{appointmentId:int}/cancellation", async (
            int appointmentId, AppointmentService service, CancellationToken ct) =>
            (await service.CancelAsync(appointmentId, ct)).ToHttpResult(Results.Ok));

        // E8: Consultar cita
        group.MapGet("/{appointmentId:int}", async (
            int appointmentId,
            AppointmentService service,
            CancellationToken ct) =>
        {
            var result = await service.GetByIdAsync(appointmentId, ct);
            return result.ToHttpResult(Results.Ok);
        });

        return group;
    }
}
