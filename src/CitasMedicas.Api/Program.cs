using System.Text.Json;
using System.Text.Json.Serialization;
using CitasMedicas.Api.Endpoints;
using CitasMedicas.Application;
using CitasMedicas.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

// Verificar zona horaria America/Lima al arrancar
try
{
    TimeZoneInfo.FindSystemTimeZoneById("America/Lima");
}
catch (TimeZoneNotFoundException ex)
{
    throw new InvalidOperationException("La zona horaria 'America/Lima' no se encuentra disponible en el sistema.", ex);
}

var frontendWebRoot = new[]
{
    Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "CitasMedicas.Web", "wwwroot")),
    Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "src", "CitasMedicas.Web", "wwwroot")),
    Path.Combine(AppContext.BaseDirectory, "wwwroot")
}.FirstOrDefault(Directory.Exists)
    ?? throw new DirectoryNotFoundException("No se encontró el frontend en src/CitasMedicas.Web/wwwroot ni en wwwroot junto a la aplicación publicada.");

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    WebRootPath = frontendWebRoot
});
var frontendFileProvider = new PhysicalFileProvider(frontendWebRoot);

// Servicios
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CitasMedicas.Infrastructure.Persistence.SqlServer.ClinicDbContext>();
    await db.Database.MigrateAsync();
}

app.UseHttpsRedirection();

app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = frontendFileProvider });
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = frontendFileProvider,
    OnPrepareResponse = ctx =>
    {
        // Evita que el navegador cachee HTML/CSS/JS y siempre sirva la última versión.
        ctx.Context.Response.Headers.CacheControl = "no-cache, no-store, must-revalidate";
    }
});

// Endpoints
app.MapGroup("/api/patients").MapPatientEndpoints();
app.MapGroup("/api").MapCatalogEndpoints();
app.MapGroup("/api/doctors").MapScheduleEndpoints();
app.MapGroup("/api/appointments").MapAppointmentEndpoints();

// Rutas de cliente de la SPA: servir index.html en rutas conocidas.
// Las rutas /api desconocidas conservan 404.
app.MapFallback("/api/{**path}", () => Results.NotFound());
app.MapFallbackToFile("index.html", new StaticFileOptions { FileProvider = frontendFileProvider });

app.Run();
