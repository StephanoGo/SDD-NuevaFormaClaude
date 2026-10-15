using CitasMedicas.Api.Http;
using CitasMedicas.Application.Appointments;

namespace CitasMedicas.Api.Endpoints;

public static class AppointmentEndpoints
{
    public static RouteGroupBuilder MapAppointmentEndpoints(this RouteGroupBuilder group)
    {
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
