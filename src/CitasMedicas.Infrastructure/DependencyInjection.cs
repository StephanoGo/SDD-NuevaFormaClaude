using CitasMedicas.Application.Abstractions.Persistence;
using CitasMedicas.Infrastructure.Persistence.InMemory;
using Microsoft.Extensions.DependencyInjection;

namespace CitasMedicas.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IPatientRepository, InMemoryPatientRepository>();
        services.AddSingleton<IDoctorRepository, InMemoryDoctorRepository>();
        services.AddSingleton<IAppointmentRepository, InMemoryAppointmentRepository>();

        return services;
    }
}
