using CitasMedicas.Application.Patients;
using CitasMedicas.Infrastructure.Persistence.InMemory;
using FluentAssertions;

namespace CitasMedicas.Tests.Application;

public class PatientServiceTests
{
    private readonly InMemoryPatientRepository _repository = new();
    private readonly PatientService _service;

    public PatientServiceTests()
    {
        _service = new PatientService(_repository);
    }

    [Fact]
    [Trait("HU", "HU-01")]
    [Trait("RF", "RF-001")]
    public async Task HU01_Scenario1_RegisterPatient_WithValidData_ShouldSucceedAndReturnId()
    {
        var request = new RegisterPatientRequest("45678912", "Rosa", "García López", "+51 987 654 321", "rosa.garcia@correo.pe");

        var result = await _service.RegisterAsync(request);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().BeGreaterThan(0);
        result.Value.Dni.Should().Be("45678912");
        result.Value.FirstName.Should().Be("Rosa");
        result.Value.LastName.Should().Be("García López");
        result.Value.Phone.Should().Be("+51 987 654 321");
        result.Value.Email.Should().Be("rosa.garcia@correo.pe");
    }

    [Fact]
    [Trait("HU", "HU-01")]
    [Trait("RF", "RF-002")]
    public async Task HU01_Scenario2_RegisterPatient_WithExistingDni_ShouldReturnPatientDniDuplicate()
    {
        var initial = new RegisterPatientRequest("45678912", "Rosa", "García López", "+51 987 654 321", null);
        await _service.RegisterAsync(initial);

        var duplicate = new RegisterPatientRequest("45678912", "Otra", "Persona", "+51 987 654 322", null);
        var result = await _service.RegisterAsync(duplicate);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("PATIENT_DNI_DUPLICATE");
    }

    [Fact]
    [Trait("HU", "HU-01")]
    [Trait("RF", "RF-006")]
    public async Task HU01_Scenario3_SearchByDni_WhenExists_ShouldReturnExactPatient()
    {
        var request = new RegisterPatientRequest("45678912", "Rosa", "García López", "+51 987 654 321", null);
        await _service.RegisterAsync(request);

        var patients = await _service.SearchByDniAsync("45678912");

        patients.Should().HaveCount(1);
        patients[0].Dni.Should().Be("45678912");
        patients[0].FirstName.Should().Be("Rosa");
    }

    [Fact]
    [Trait("HU", "HU-01")]
    [Trait("RF", "RF-007")]
    public async Task HU01_Scenario4_SearchByLastName_WithGar_ShouldReturnGarciaAndGarces()
    {
        await _service.RegisterAsync(new RegisterPatientRequest("11111111", "Rosa", "García López", "+51 987 654 321", null));
        await _service.RegisterAsync(new RegisterPatientRequest("22222222", "Luis", "Garcés Ruiz", "+51 987 654 322", null));
        await _service.RegisterAsync(new RegisterPatientRequest("33333333", "Pedro", "Torres Lima", "+51 987 654 323", null));

        var result = await _service.SearchByLastNameAsync("gar");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Select(p => p.LastName).Should().Contain(["García López", "Garcés Ruiz"]);
    }

    [Fact]
    [Trait("HU", "HU-01")]
    [Trait("RF", "RF-007")]
    public async Task HU01_Scenario5_SearchByLastName_WhenNoMatch_ShouldReturnEmptyListWithoutError()
    {
        await _service.RegisterAsync(new RegisterPatientRequest("11111111", "Rosa", "García López", "+51 987 654 321", null));

        var result = await _service.SearchByLastNameAsync("Zapata");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Theory]
    [Trait("RF", "RF-008")]
    [InlineData("G")]
    [InlineData(" G ")]
    [InlineData("")]
    public async Task SearchByLastName_WithShortCriteria_ShouldReturnSearchCriteriaTooShort(string criteria)
    {
        var result = await _service.SearchByLastNameAsync(criteria);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("SEARCH_CRITERIA_TOO_SHORT");
    }
}
