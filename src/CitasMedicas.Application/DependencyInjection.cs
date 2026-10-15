using CitasMedicas.Application.Appointments;
using CitasMedicas.Application.Catalogs;
using CitasMedicas.Application.Common;
using CitasMedicas.Application.Patients;
using CitasMedicas.Application.Scheduling;
using Microsoft.Extensions.DependencyInjection;

namespace CitasMedicas.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<ClinicClock>();
        services.AddScoped<PatientService>();
        services.AddScoped<ScheduleService>();
        services.AddScoped<AppointmentService>();
        services.AddScoped<CatalogService>();

        return services;
    }
}
