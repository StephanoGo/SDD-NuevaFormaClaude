using CitasMedicas.Application.Abstractions.Persistence;
using CitasMedicas.Application.Catalogs;
using CitasMedicas.Domain.Common;
using CitasMedicas.Infrastructure.Persistence.InMemory;
using CitasMedicas.Tests.Support;
using FluentAssertions;

namespace CitasMedicas.Tests.Application;

public sealed class CatalogServiceTests
{
    [Theory]
    [InlineData(null, "Ana", "Torres", 1, "DOCTOR_LICENSE_REQUIRED")]
    [InlineData("CMP-123", " ", "Torres", 1, "DOCTOR_FIRST_NAME_REQUIRED")]
    [InlineData("CMP-123", "Ana", "", 1, "DOCTOR_LAST_NAME_REQUIRED")]
    [InlineData("CMP-123", "Ana", "Torres", null, "DOCTOR_SPECIALTY_REQUIRED")]
    [InlineData("CMP-123", "Ana", "Torres", 0, "DOCTOR_SPECIALTY_REQUIRED")]
    [Trait("RF", "RF-PER-08")]
    public async Task DoctorWrites_RejectInvalidRequiredFieldsWithoutWriting(
        string? license, string? firstName, string? lastName, int? specialtyId, string errorCode)
    {
        var doctors = new InMemoryDoctorRepository();
        var service = new CatalogService(doctors, new TestSpecialties());
        var request = new DoctorWriteRequest(license, firstName, lastName, specialtyId);

        (await service.AddDoctorAsync(request)).Error.Code.Should().Be(errorCode);
        (await service.UpdateDoctorAsync(1, request)).Error.Code.Should().Be(errorCode);
        (await doctors.ListAsync(true)).Should().BeEmpty();
    }

    [Fact]
    [Trait("RF", "RF-PER-08")]
    public async Task SpecialtyWrites_RejectEmptyNameWithoutWriting()
    {
        var specialties = new TestSpecialties();
        var service = new CatalogService(new InMemoryDoctorRepository(), specialties);

        (await service.AddSpecialtyAsync(new SpecialtyWriteRequest("  "))).Error.Code.Should().Be("SPECIALTY_NAME_REQUIRED");
        (await service.UpdateSpecialtyAsync(1, new SpecialtyWriteRequest(null))).Error.Code.Should().Be("SPECIALTY_NAME_REQUIRED");
        (await specialties.ListAsync(true)).Should().BeEmpty();
    }

    [Fact]
    [Trait("RF", "RF-PER-05")]
    [Trait("RF", "RF-PER-10")]
    public async Task CatalogWrites_TrimFieldsAndListsIncludeInactiveOnlyWhenRequested()
    {
        var doctors = new InMemoryDoctorRepository();
        var specialties = new TestSpecialties();
        var service = new CatalogService(doctors, specialties);

        var specialty = (await service.AddSpecialtyAsync(new SpecialtyWriteRequest("  Cardiología  "))).Value;
        specialty.Name.Should().Be("Cardiología");
        var doctor = (await service.AddDoctorAsync(new DoctorWriteRequest(" CMP-123 ", " Ana ", " Torres ", specialty.Id))).Value;
        doctor.LicenseNumber.Should().Be("CMP-123");
        doctor.FirstName.Should().Be("Ana");
        doctor.LastName.Should().Be("Torres");

        (await service.UpdateDoctorAsync(doctor.Id, new DoctorWriteRequest(" CMP-456 ", " Ana ", " Rojas ", specialty.Id)))
            .Value.LastName.Should().Be("Rojas");
        (await service.UpdateSpecialtyAsync(specialty.Id, new SpecialtyWriteRequest(" Clínica ")))
            .Value.Name.Should().Be("Clínica");

        (await service.DeactivateDoctorAsync(doctor.Id)).IsSuccess.Should().BeTrue();
        (await service.DeactivateSpecialtyAsync(specialty.Id)).IsSuccess.Should().BeTrue();
        (await service.ListDoctorsAsync(false)).Should().BeEmpty();
        (await service.ListSpecialtiesAsync(false)).Should().BeEmpty();
        (await service.ListDoctorsAsync(true)).Should().ContainSingle(x => x.Id == doctor.Id && !x.IsActive);
        (await service.ListSpecialtiesAsync(true)).Should().ContainSingle(x => x.Id == specialty.Id && !x.IsActive);
    }

    private sealed class TestSpecialties : ISpecialtyRepository
    {
        private readonly Dictionary<int, SpecialtyResponse> _rows = new();

        public Task<IReadOnlyList<SpecialtyResponse>> ListAsync(bool includeInactive, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SpecialtyResponse>>(_rows.Values.Where(x => includeInactive || x.IsActive).ToList());

        public Task<Result<SpecialtyResponse>> AddAsync(SpecialtyWriteRequest request, CancellationToken cancellationToken = default)
        {
            var row = new SpecialtyResponse(_rows.Count + 1, request.Name!, true);
            _rows.Add(row.Id, row);
            return Task.FromResult<Result<SpecialtyResponse>>(row);
        }

        public Task<Result<SpecialtyResponse>> UpdateAsync(int id, SpecialtyWriteRequest request, CancellationToken cancellationToken = default)
        {
            if (!_rows.TryGetValue(id, out var row)) return Task.FromResult<Result<SpecialtyResponse>>(CatalogErrors.SpecialtyNotFound);
            row = row with { Name = request.Name! };
            _rows[id] = row;
            return Task.FromResult<Result<SpecialtyResponse>>(row);
        }

        public Task<Result> DeactivateAsync(int id, CancellationToken cancellationToken = default)
        {
            if (!_rows.TryGetValue(id, out var row)) return Task.FromResult<Result>(CatalogErrors.SpecialtyNotFound);
            _rows[id] = row with { IsActive = false };
            return Task.FromResult(Result.Success());
        }
    }
}
