using CitasMedicas.Domain.Patients;
using FluentAssertions;

namespace CitasMedicas.Tests.Domain.Patients;

public class DniTests
{
    [Theory]
    [Trait("RF", "RF-003")]
    [InlineData("45678912", "45678912")]
    [InlineData("00012345", "00012345")]
    [InlineData(" 45678912 ", "45678912")]
    public void Create_WithValidDni_ShouldSucceed(string input, string expected)
    {
        var result = Dni.Create(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(expected);
    }

    [Theory]
    [Trait("RF", "RF-003")]
    [InlineData("1234567")]
    [InlineData("123456789")]
    [InlineData("4567891A")]
    [InlineData("4567 8912")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("٤٥٦٧٨٩١٢")]
    [InlineData(null)]
    public void Create_WithInvalidDni_ShouldReturnPatientDniInvalid(string? input)
    {
        var result = Dni.Create(input);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("PATIENT_DNI_INVALID");
    }
}
