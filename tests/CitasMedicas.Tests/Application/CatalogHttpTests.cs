using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CitasMedicas.Api.Endpoints;
using CitasMedicas.Application.Abstractions.Persistence;
using CitasMedicas.Application.Catalogs;
using CitasMedicas.Domain.Common;
using CitasMedicas.Domain.Doctors;
using CitasMedicas.Infrastructure.Persistence.InMemory;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;

namespace CitasMedicas.Tests.Application;

public sealed class CatalogHttpTests
{
    [Fact]
    [Trait("RF", "RF-PER-05")]
    [Trait("RF", "RF-PER-08")]
    public async Task CatalogWrites_ValidateEditDeactivateAndDoNotExposeDelete()
    {
        await using var host = await CatalogHost.StartAsync();
        var client = host.Client;
        using var invalidSpecialty = await client.PostAsJsonAsync("/api/specialties", new { name = " " });
        await AssertProblem(invalidSpecialty, HttpStatusCode.BadRequest, "SPECIALTY_NAME_REQUIRED");

        using var specialtyCreated = await client.PostAsJsonAsync("/api/specialties", new { name = "Cardiología" });
        Assert.Equal(HttpStatusCode.Created, specialtyCreated.StatusCode);
        var specialtyId = (await specialtyCreated.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        using var missing = await client.PostAsJsonAsync("/api/doctors", new { licenseNumber = "CMP-1", firstName = "Ana", lastName = "Sol", specialtyId = 99999 });
        await AssertProblem(missing, HttpStatusCode.NotFound, "SPECIALTY_NOT_FOUND");
        using var empty = await client.PostAsJsonAsync("/api/doctors", new { licenseNumber = " ", firstName = "Ana", lastName = "Sol", specialtyId });
        await AssertProblem(empty, HttpStatusCode.BadRequest, "DOCTOR_LICENSE_REQUIRED");

        using var doctorCreated = await client.PostAsJsonAsync("/api/doctors", new { licenseNumber = "CMP-1", firstName = "Ana", lastName = "Sol", specialtyId });
        Assert.Equal(HttpStatusCode.Created, doctorCreated.StatusCode);
        var doctorId = (await doctorCreated.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        using var specialtyEdited = await client.PutAsJsonAsync($"/api/specialties/{specialtyId}", new { name = "Clínica" });
        Assert.Equal(HttpStatusCode.OK, specialtyEdited.StatusCode);
        Assert.Equal("Clínica", (await specialtyEdited.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("name").GetString());
        using var doctorEdited = await client.PutAsJsonAsync($"/api/doctors/{doctorId}", new { licenseNumber = "CMP-2", firstName = "Ana", lastName = "Luz", specialtyId });
        Assert.Equal(HttpStatusCode.OK, doctorEdited.StatusCode);
        Assert.Equal("CMP-2", (await doctorEdited.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("licenseNumber").GetString());
        using var invalidEdit = await client.PutAsJsonAsync($"/api/specialties/{specialtyId}", new { name = " " });
        await AssertProblem(invalidEdit, HttpStatusCode.BadRequest, "SPECIALTY_NAME_REQUIRED");
        using var missingEdit = await client.PutAsJsonAsync("/api/specialties/99999", new { name = "Clínica" });
        await AssertProblem(missingEdit, HttpStatusCode.NotFound, "SPECIALTY_NOT_FOUND");
        using var missingDoctorEdit = await client.PutAsJsonAsync("/api/doctors/99999", new { licenseNumber = "CMP-2", firstName = "Ana", lastName = "Luz", specialtyId });
        await AssertProblem(missingDoctorEdit, HttpStatusCode.NotFound, "DOCTOR_NOT_FOUND");
        using var doctorDeactivated = await client.PostAsync($"/api/doctors/{doctorId}/deactivation", null);
        Assert.Equal(HttpStatusCode.NoContent, doctorDeactivated.StatusCode);
        using var specialtyDeactivated = await client.PostAsync($"/api/specialties/{specialtyId}/deactivation", null);
        Assert.Equal(HttpStatusCode.NoContent, specialtyDeactivated.StatusCode);
        using var inactiveSpecialty = await client.PostAsJsonAsync("/api/doctors", new { licenseNumber = "CMP-3", firstName = "Eva", lastName = "Sol", specialtyId });
        await AssertProblem(inactiveSpecialty, HttpStatusCode.Conflict, "DOCTOR_SPECIALTY_NOT_ACTIVE");
        using var inactiveEdit = await client.PutAsJsonAsync($"/api/doctors/{doctorId}", new { licenseNumber = "CMP-3", firstName = "Eva", lastName = "Sol", specialtyId });
        await AssertProblem(inactiveEdit, HttpStatusCode.Conflict, "DOCTOR_SPECIALTY_NOT_ACTIVE");
        using var cannotDeleteDoctor = await client.DeleteAsync($"/api/doctors/{doctorId}");
        Assert.Equal(HttpStatusCode.MethodNotAllowed, cannotDeleteDoctor.StatusCode);
        using var cannotDeleteSpecialty = await client.DeleteAsync($"/api/specialties/{specialtyId}");
        Assert.Equal(HttpStatusCode.MethodNotAllowed, cannotDeleteSpecialty.StatusCode);
    }

    [Theory]
    [InlineData(" ", "Ana", "Sol", 1, "DOCTOR_LICENSE_REQUIRED")]
    [InlineData("CMP-1", " ", "Sol", 1, "DOCTOR_FIRST_NAME_REQUIRED")]
    [InlineData("CMP-1", "Ana", " ", 1, "DOCTOR_LAST_NAME_REQUIRED")]
    [InlineData("CMP-1", "Ana", "Sol", null, "DOCTOR_SPECIALTY_REQUIRED")]
    [Trait("RF", "RF-PER-08")]
    public async Task DoctorHttpWrites_RejectEveryMissingFieldOnCreateAndEdit(
        string licenseNumber, string firstName, string lastName, int? specialtyId, string code)
    {
        await using var host = await CatalogHost.StartAsync();
        using var specialty = await host.Client.PostAsJsonAsync("/api/specialties", new { name = "Clínica" });
        using var created = await host.Client.PostAsJsonAsync("/api/doctors", new { licenseNumber = "CMP-2", firstName = "Ana", lastName = "Luz", specialtyId = 1 });
        var doctorId = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        var request = new { licenseNumber, firstName, lastName, specialtyId };
        using var add = await host.Client.PostAsJsonAsync("/api/doctors", request);
        await AssertProblem(add, HttpStatusCode.BadRequest, code);
        using var edit = await host.Client.PutAsJsonAsync($"/api/doctors/{doctorId}", request);
        await AssertProblem(edit, HttpStatusCode.BadRequest, code);
        using var listed = await host.Client.GetAsync("/api/doctors");
        Assert.Single((await listed.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray());
    }

    [Fact]
    [Trait("RF", "RF-PER-10")]
    public async Task CatalogLists_DefaultToActiveAndRejectInvalidFilter()
    {
        await using var host = await CatalogHost.StartAsync();
        var client = host.Client;
        using var created = await client.PostAsJsonAsync("/api/specialties", new { name = "Clínica" });
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        using var doctor = await client.PostAsJsonAsync("/api/doctors", new { licenseNumber = "CMP-1", firstName = "Ana", lastName = "Luz", specialtyId = id });
        var doctorId = (await doctor.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        using var deactivateDoctor = await client.PostAsync($"/api/doctors/{doctorId}/deactivation", null);
        using var deactivateSpecialty = await client.PostAsync($"/api/specialties/{id}/deactivation", null);
        foreach (var catalog in new[] { "doctors", "specialties" })
        {
            using var active = await client.GetAsync($"/api/{catalog}");
            Assert.Empty((await active.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray());
            using var all = await client.GetAsync($"/api/{catalog}?includeInactive=true");
            Assert.Single((await all.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray());
            using var invalid = await client.GetAsync($"/api/{catalog}?includeInactive=invalid");
            await AssertProblem(invalid, HttpStatusCode.BadRequest, "VALIDATION_FORMAT");
        }
    }

    private static async Task AssertProblem(HttpResponseMessage response, HttpStatusCode status, string code)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(code, (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
    }

    private sealed class CatalogHost(WebApplication app, HttpClient client) : IAsyncDisposable
    {
        public HttpClient Client => client;

        public static async Task<CatalogHost> StartAsync()
        {
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseUrls("http://127.0.0.1:0");
            builder.Services.AddSingleton<TestSpecialties>();
            builder.Services.AddSingleton<ISpecialtyRepository>(sp => sp.GetRequiredService<TestSpecialties>());
            builder.Services.AddSingleton<IDoctorRepository>(sp => new TestDoctors(sp.GetRequiredService<TestSpecialties>()));
            builder.Services.AddScoped<CatalogService>();
            var app = builder.Build();
            app.MapGroup("/api").MapCatalogEndpoints();
            await app.StartAsync();
            var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
            return new CatalogHost(app, new HttpClient { BaseAddress = new Uri(address) });
        }

        public async ValueTask DisposeAsync()
        {
            client.Dispose();
            await app.DisposeAsync();
        }
    }

    private sealed class TestSpecialties : ISpecialtyRepository
    {
        private readonly Dictionary<int, SpecialtyResponse> _rows = new();
        public SpecialtyResponse? Find(int id) => _rows.GetValueOrDefault(id);
        public Task<IReadOnlyList<SpecialtyResponse>> ListAsync(bool includeInactive, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<SpecialtyResponse>>(_rows.Values.Where(x => includeInactive || x.IsActive).ToList());
        public Task<Result<SpecialtyResponse>> AddAsync(SpecialtyWriteRequest request, CancellationToken ct = default)
        {
            var row = new SpecialtyResponse(_rows.Count + 1, request.Name!, true);
            _rows.Add(row.Id, row);
            return Task.FromResult<Result<SpecialtyResponse>>(row);
        }
        public Task<Result<SpecialtyResponse>> UpdateAsync(int id, SpecialtyWriteRequest request, CancellationToken ct = default)
        {
            if (!_rows.TryGetValue(id, out var row)) return Task.FromResult<Result<SpecialtyResponse>>(CatalogErrors.SpecialtyNotFound);
            row = row with { Name = request.Name! };
            _rows[id] = row;
            return Task.FromResult<Result<SpecialtyResponse>>(row);
        }
        public Task<Result> DeactivateAsync(int id, CancellationToken ct = default)
        {
            if (!_rows.TryGetValue(id, out var row)) return Task.FromResult<Result>(CatalogErrors.SpecialtyNotFound);
            _rows[id] = row with { IsActive = false };
            return Task.FromResult(Result.Success());
        }
    }

    private sealed class TestDoctors(TestSpecialties specialties) : IDoctorRepository
    {
        private readonly InMemoryDoctorRepository _inner = new();
        public Task<Doctor?> GetByIdAsync(int id, CancellationToken ct = default) => _inner.GetByIdAsync(id, ct);
        public Task<IReadOnlyList<DoctorCatalogResponse>> ListAsync(bool includeInactive, CancellationToken ct = default) => _inner.ListAsync(includeInactive, ct);
        public Task<Result<DoctorCatalogResponse>> AddAsync(DoctorWriteRequest request, CancellationToken ct = default) =>
            specialties.Find(request.SpecialtyId!.Value) switch
            {
                null => Task.FromResult<Result<DoctorCatalogResponse>>(CatalogErrors.SpecialtyNotFound),
                { IsActive: false } => Task.FromResult<Result<DoctorCatalogResponse>>(CatalogErrors.DoctorSpecialtyNotActive),
                _ => _inner.AddAsync(request, ct)
            };
        public Task<Result<DoctorCatalogResponse>> UpdateAsync(int id, DoctorWriteRequest request, CancellationToken ct = default) =>
            specialties.Find(request.SpecialtyId!.Value) switch
            {
                null => Task.FromResult<Result<DoctorCatalogResponse>>(CatalogErrors.SpecialtyNotFound),
                { IsActive: false } => Task.FromResult<Result<DoctorCatalogResponse>>(CatalogErrors.DoctorSpecialtyNotActive),
                _ => _inner.UpdateAsync(id, request, ct)
            };
        public Task<Result> DeactivateAsync(int id, CancellationToken ct = default) => _inner.DeactivateAsync(id, ct);
    }
}
