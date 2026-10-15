using CitasMedicas.Domain.Patients;
using FluentAssertions;

namespace CitasMedicas.Tests.Domain.Patients;

public class PatientTests
{
    [Fact]
    [Trait("RF", "RF-004")]
    [Trait("RF", "RF-005")]
    public void Create_WithValidDataAndNullEmail_ShouldSucceed()
    {
        var result = Patient.Create("45678912", "Rosa", "García López", "+51 987 654 321", null);

        result.IsSuccess.Should().BeTrue();
        result.Value.Dni.Value.Should().Be("45678912");
        result.Value.FirstName.Should().Be("Rosa");
        result.Value.LastName.Should().Be("García López");
        result.Value.Phone.Should().Be("+51 987 654 321");
        result.Value.Email.Should().BeNull();
        result.Value.FullName.Should().Be("Rosa García López");
    }

    [Fact]
    [Trait("RF", "RF-004")]
    public void Create_WithValidDataAndValidEmail_ShouldSucceed()
    {
        var result = Patient.Create("45678912", "Rosa", "García López", "+51 987 654 321", "rosa.garcia@correo.pe");

        result.IsSuccess.Should().BeTrue();
        result.Value.Email.Should().NotBeNull();
        result.Value.Email!.Value.Should().Be("rosa.garcia@correo.pe");
    }

    [Theory]
    [Trait("RF", "RF-005")]
    [InlineData("", "García López", "+51 987 654 321")]
    [InlineData("   ", "García López", "+51 987 654 321")]
    [InlineData(null, "García López", "+51 987 654 321")]
    [InlineData("Rosa", "", "+51 987 654 321")]
    [InlineData("Rosa", "   ", "+51 987 654 321")]
    [InlineData("Rosa", null, "+51 987 654 321")]
    [InlineData("Rosa", "García López", "")]
    [InlineData("Rosa", "García López", "   ")]
    [InlineData("Rosa", "García López", null)]
    public void Create_WithEmptyRequiredField_ShouldReturnPatientRequiredField(string? firstName, string? lastName, string? phone)
    {
        var result = Patient.Create("45678912", firstName, lastName, phone, "correo@valido.pe");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("PATIENT_REQUIRED_FIELD");
    }

    [Theory]
    [Trait("RF", "RF-004")]
    [InlineData("plainaddress")]
    [InlineData("@missingusername.com")]
    [InlineData("username@.com")]
    [InlineData("username@domain")]
    [InlineData("username space@domain.com")]
    public void Create_WithInvalidEmail_ShouldReturnPatientEmailInvalid(string email)
    {
        var result = Patient.Create("45678912", "Rosa", "García López", "+51 987 654 321", email);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("PATIENT_EMAIL_INVALID");
    }
}
