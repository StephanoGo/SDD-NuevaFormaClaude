using System.Diagnostics;
using CitasMedicas.Application.Abstractions.Persistence;
using CitasMedicas.Infrastructure;
using CitasMedicas.Infrastructure.Persistence.SqlServer;
using CitasMedicas.Application;
using CitasMedicas.Api.Endpoints;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CitasMedicas.Tests.Infrastructure;

public sealed class SqlConfigurationTests
{
    [Fact]
    [Trait("RF", "RF-PER-03")]
    public async Task InvalidMigrationAssembly_FailsBeforeStartingIsolatedHost()
    {
        // Resolving a non-existent migrations assembly fails before EF opens a connection.
        var options = new DbContextOptionsBuilder<ClinicDbContext>()
            .UseSqlServer("Server=localhost;Database=BD_MedicinaGen_Tests;Integrated Security=True",
                sql => sql.MigrationsAssembly("Missing.Clinic.Migrations.ForTest"))
            .Options;
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        await using var app = builder.Build();
        await using var db = new ClinicDbContext(options);

        var migrate = async () => await db.Database.MigrateAsync();
        await migrate.Should().ThrowAsync<Exception>();
        app.Urls.Should().BeEmpty();
        app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()?.Addresses
            .Should().BeNullOrEmpty();
    }

    [Fact]
    [Trait("RF", "RF-PER-07")]
    public async Task ApiRoutes_ContainNoUserAuthenticationAuthorizationOrRoleFeatures()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddApplication();
        await using var app = builder.Build();
        app.MapGroup("/api/patients").MapPatientEndpoints();
        app.MapGroup("/api").MapCatalogEndpoints();
        app.MapGroup("/api/doctors").MapScheduleEndpoints();
        app.MapGroup("/api/appointments").MapAppointmentEndpoints();
        await app.StartAsync();
        try
        {
            var routes = app.Services.GetRequiredService<EndpointDataSource>().Endpoints
                .OfType<RouteEndpoint>().ToArray();
            routes.Should().NotBeEmpty();
            routes.Should().OnlyContain(route => !route.RoutePattern.RawText!.Contains("user", StringComparison.OrdinalIgnoreCase)
                && !route.RoutePattern.RawText.Contains("auth", StringComparison.OrdinalIgnoreCase)
                && !route.RoutePattern.RawText.Contains("role", StringComparison.OrdinalIgnoreCase));
            routes.Should().OnlyContain(route => !route.Metadata.OfType<IAuthorizeData>().Any());
            typeof(CatalogEndpoints).Assembly.GetTypes().Should().OnlyContain(type =>
                !type.Name.Contains("User", StringComparison.OrdinalIgnoreCase)
                && !type.Name.Contains("Auth", StringComparison.OrdinalIgnoreCase)
                && !type.Name.Contains("Role", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            await app.StopAsync();
        }
    }

    [Fact]
    [Trait("RF", "RF-PER-03")]
    public async Task ApiStartup_UnreachableIsolatedTestEndpoint_FailsBeforeServingRequests()
    {
        // Force a non-listening local port and the dedicated test catalog. Never inherit the demo connection.
        var apiAssembly = typeof(CitasMedicas.Api.Endpoints.CatalogEndpoints).Assembly.Location;
        var start = new ProcessStartInfo("dotnet")
        {
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            WorkingDirectory = AppContext.BaseDirectory
        };
        start.ArgumentList.Add(apiAssembly);
        start.Environment["ConnectionStrings__Clinic"] =
            "Server=tcp:localhost,1;Database=BD_MedicinaGen_Tests;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;Connect Timeout=1";
        start.Environment["ASPNETCORE_URLS"] = "http://127.0.0.1:0";
        using var process = Process.Start(start)!;
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync();
            throw new Xunit.Sdk.XunitException("El inicio aislado no terminó tras agotar el tiempo límite.");
        }
        // Do not print captured process output: it could contain environment-specific diagnostics.
        var output = await stdout + await stderr;
        process.ExitCode.Should().NotBe(0);
        output.Should().NotContain("Now listening on");
    }

    [Fact]
    [Trait("RF", "RF-PER-03")]
    public void MissingClinicConnection_FailsCompositionWithoutInMemoryFallback()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?>()).Build();
        var services = new ServiceCollection();

        var act = () => services.AddInfrastructure(config);

        act.Should().Throw<InvalidOperationException>().WithMessage("*ConnectionStrings:Clinic*");
        services.Should().NotContain(descriptor => descriptor.ServiceType == typeof(IPatientRepository));
    }

    [Fact]
    [Trait("RF", "RF-PER-04")]
    [Trait("RF", "RF-PER-07")]
    public void SqlModel_DefinesOnlyBusinessTablesWithoutSeedOrUserRoles()
    {
        var options = new DbContextOptionsBuilder<ClinicDbContext>()
            .UseSqlServer("Server=localhost;Database=BD_MedicinaGen_Tests;Integrated Security=True")
            .Options;
        using var db = new ClinicDbContext(options); // Inspect metadata only; do not open a connection.
        var entities = db.GetService<IDesignTimeModel>().Model.GetEntityTypes().ToArray();
        entities.Select(entity => entity.GetTableName()).Should().BeEquivalentTo(
            ["Patients", "Specialties", "Doctors", "ScheduleSlots", "Appointments", "Attentions"]);
        entities.Should().OnlyContain(entity => !entity.GetSeedData(false).Any());
    }
}
