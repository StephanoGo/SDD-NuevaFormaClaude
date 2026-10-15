using CitasMedicas.Domain.Patients;
using CitasMedicas.Infrastructure.Persistence.InMemory;
using FluentAssertions;

namespace CitasMedicas.Tests.Infrastructure;

public class InMemoryPatientRepositoryTests
{
    [Fact]
    [Trait("RF", "RF-002")]
    public async Task AddAsync_WithDuplicateDni_ShouldReturnPatientDniDuplicate()
    {
        var repo = new InMemoryPatientRepository();
        var patient1 = Patient.Create("45678912", "Rosa", "García", "+51 987 654 321", null).Value;
        var patient2 = Patient.Create("45678912", "María", "López", "+51 987 654 322", null).Value;

        var result1 = await repo.AddAsync(patient1);
        var result2 = await repo.AddAsync(patient2);

        result1.IsSuccess.Should().BeTrue();
        result2.IsFailure.Should().BeTrue();
        result2.Error.Code.Should().Be("PATIENT_DNI_DUPLICATE");
    }

    [Fact]
    [Trait("RF", "RF-002")]
    public async Task AddAsync_With20ConcurrentRegistrationsSameDni_ShouldHaveExactlyOneSuccess()
    {
        var repo = new InMemoryPatientRepository();
        const int concurrencyLevel = 20;

        var tasks = Enumerable.Range(1, concurrencyLevel).Select(async i =>
        {
            var patient = Patient.Create("45678912", $"Nombre{i}", $"Apellido{i}", "+51 987 654 321", null).Value;
            return await repo.AddAsync(patient);
        });

        var results = await Task.WhenAll(tasks);

        results.Count(r => r.IsSuccess).Should().Be(1);
        results.Count(r => r.IsFailure && r.Error.Code == "PATIENT_DNI_DUPLICATE").Should().Be(concurrencyLevel - 1);
    }

    [Fact]
    [Trait("RF", "RF-007")]
    public async Task SearchByLastNameAsync_WithNunez_ShouldFindNuñezIgnoringAccentsAndCase()
    {
        var repo = new InMemoryPatientRepository();
        var patient = Patient.Create("45678912", "Rosa", "Núñez Flores", "+51 987 654 321", null).Value;
        await repo.AddAsync(patient);

        var results = await repo.SearchByLastNameAsync("nunez");

        results.Should().HaveCount(1);
        results[0].LastName.Should().Be("Núñez Flores");
    }

    [Fact]
    [Trait("RF", "RF-007")]
    public async Task SearchByLastNameAsync_ShouldReturnOrderedByLastNameThenFirstName()
    {
        var repo = new InMemoryPatientRepository();
        var p1 = Patient.Create("11111111", "Carlos", "García", "123", null).Value;
        var p2 = Patient.Create("22222222", "Ana", "García", "123", null).Value;
        var p3 = Patient.Create("33333333", "Beatriz", "Álvarez", "123", null).Value;

        await repo.AddAsync(p1);
        await repo.AddAsync(p2);
        await repo.AddAsync(p3);

        var results = await repo.SearchByLastNameAsync("a");

        results.Select(p => $"{p.LastName} {p.FirstName}").Should().ContainInOrder(
            "Álvarez Beatriz",
            "García Ana",
            "García Carlos"
        );
    }

    [Fact]
    [Trait("RF", "RF-006")]
    public async Task SearchByLastNameAsync_WithoutMatches_ShouldReturnEmptyList()
    {
        var repo = new InMemoryPatientRepository();
        var patient = Patient.Create("45678912", "Rosa", "García", "123", null).Value;
        await repo.AddAsync(patient);

        var results = await repo.SearchByLastNameAsync("Zapata");

        results.Should().BeEmpty();
    }

    [Fact]
    [Trait("RF", "RF-006")]
    public async Task GetByDniAsync_WhenExists_ShouldReturnMatchingPatient()
    {
        var repo = new InMemoryPatientRepository();
        var patient = Patient.Create("45678912", "Rosa", "García", "123", null).Value;
        await repo.AddAsync(patient);

        var found = await repo.GetByDniAsync("45678912");

        found.Should().NotBeNull();
        found!.Dni.Value.Should().Be("45678912");
    }
}
