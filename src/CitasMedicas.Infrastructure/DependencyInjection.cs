using CitasMedicas.Application.Abstractions.Persistence;
using CitasMedicas.Infrastructure.Persistence.SqlServer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CitasMedicas.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Clinic")
            ?? throw new InvalidOperationException("Falta configurar ConnectionStrings:Clinic.");
        services.AddDbContext<ClinicDbContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped<SqlServerClinicRepository>();
        services.AddScoped<IPatientRepository>(sp => sp.GetRequiredService<SqlServerClinicRepository>());
        services.AddScoped<IDoctorRepository>(sp => sp.GetRequiredService<SqlServerClinicRepository>());
        services.AddScoped<ISpecialtyRepository>(sp => sp.GetRequiredService<SqlServerClinicRepository>());
        services.AddScoped<IAppointmentRepository>(sp => sp.GetRequiredService<SqlServerClinicRepository>());

        return services;
    }
}
