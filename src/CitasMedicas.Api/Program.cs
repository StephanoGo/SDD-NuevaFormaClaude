using System.Text.Json;
using System.Text.Json.Serialization;
using CitasMedicas.Api.Endpoints;
using CitasMedicas.Application;
using CitasMedicas.Infrastructure;

// Verificar zona horaria America/Lima al arrancar
try
{
    TimeZoneInfo.FindSystemTimeZoneById("America/Lima");
}
catch (TimeZoneNotFoundException ex)
{
    throw new InvalidOperationException("La zona horaria 'America/Lima' no se encuentra disponible en el sistema.", ex);
}

var builder = WebApplication.CreateBuilder(args);

// Servicios
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddApplication();
builder.Services.AddInfrastructure();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

var app = builder.Build();

app.UseHttpsRedirection();

app.UseDefaultFiles();
app.UseStaticFiles();

// Endpoints
app.MapGroup("/api/patients").MapPatientEndpoints();
app.MapGroup("/api/doctors").MapScheduleEndpoints();
app.MapGroup("/api/appointments").MapAppointmentEndpoints();

app.Run();
