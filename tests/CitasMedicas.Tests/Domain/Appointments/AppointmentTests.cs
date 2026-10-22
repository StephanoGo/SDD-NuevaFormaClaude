using CitasMedicas.Domain.Appointments;
using FluentAssertions;

namespace CitasMedicas.Tests.Domain.Appointments;

public class AppointmentTests
{
    [Fact]
    public void Cancel_WhenPending_ShouldTransitionToCancelled()
    {
        var appointment = Appointment.Rehydrate(100, 10, 1, "Consulta", AppointmentStatus.Pending, null, null);

        var result = appointment.Cancel();

        result.IsSuccess.Should().BeTrue();
        appointment.Status.Should().Be(AppointmentStatus.Cancelled);
    }

    [Theory]
    [InlineData(AppointmentStatus.Attended)]
    [InlineData(AppointmentStatus.Cancelled)]
    public void Cancel_WhenNotPending_ShouldRejectWithoutChangingStatus(AppointmentStatus status)
    {
        var appointment = Appointment.Rehydrate(100, 10, 1, "Consulta", status, "Diagnóstico", null);

        var result = appointment.Cancel();

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("APPOINTMENT_NOT_PENDING");
        appointment.Status.Should().Be(status);
        appointment.Diagnosis.Should().Be("Diagnóstico");
    }

    [Fact]
    [Trait("RF", "RF-022")]
    public void RegisterAttention_WithDiagnosisAndPrescription_ShouldTransitionToAttended()
    {
        var appointment = Appointment.Rehydrate(100, 10, 1, "Dolor de cabeza", AppointmentStatus.Pending, null, null);

        var result = appointment.RegisterAttention("  Migraña tensional  ", "  Paracetamol 500 mg c/8h por 3 días  ");

        result.IsSuccess.Should().BeTrue();
        appointment.Status.Should().Be(AppointmentStatus.Attended);
        appointment.Diagnosis.Should().Be("Migraña tensional");
        appointment.Prescription.Should().Be("Paracetamol 500 mg c/8h por 3 días");
    }

    [Theory]
    [Trait("RF", "RF-022")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void RegisterAttention_WithDiagnosisAndNullOrEmptyPrescription_ShouldTransitionToAttended(string? prescription)
    {
        var appointment = Appointment.Rehydrate(100, 10, 1, "Dolor de cabeza", AppointmentStatus.Pending, null, null);

        var result = appointment.RegisterAttention("Faringitis aguda", prescription);

        result.IsSuccess.Should().BeTrue();
        appointment.Status.Should().Be(AppointmentStatus.Attended);
        appointment.Diagnosis.Should().Be("Faringitis aguda");
        appointment.Prescription.Should().BeNull();
    }

    [Theory]
    [Trait("RF", "RF-023")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void RegisterAttention_WithEmptyDiagnosis_ShouldReturnAttentionDiagnosisRequiredAndRemainPending(string? diagnosis)
    {
        var appointment = Appointment.Rehydrate(100, 10, 1, "Dolor de cabeza", AppointmentStatus.Pending, null, null);

        var result = appointment.RegisterAttention(diagnosis, "Paracetamol");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ATTENTION_DIAGNOSIS_REQUIRED");
        appointment.Status.Should().Be(AppointmentStatus.Pending);
        appointment.Diagnosis.Should().BeNull();
        appointment.Prescription.Should().BeNull();
    }

    [Fact]
    [Trait("RF", "RF-024")]
    [Trait("RF", "RF-025")]
    public void RegisterAttention_WhenAlreadyAttended_ShouldReturnAppointmentNotPendingAndNotModifyData()
    {
        var appointment = Appointment.Rehydrate(
            id: 100,
            scheduleSlotId: 10,
            patientId: 1,
            reason: "Dolor de cabeza",
            status: AppointmentStatus.Attended,
            diagnosis: "Diagnóstico inicial",
            prescription: "Receta inicial");

        var result = appointment.RegisterAttention("Nuevo diagnóstico", "Nueva receta");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("APPOINTMENT_NOT_PENDING");
        appointment.Status.Should().Be(AppointmentStatus.Attended);
        appointment.Diagnosis.Should().Be("Diagnóstico inicial");
        appointment.Prescription.Should().Be("Receta inicial");
    }

    [Fact]
    [Trait("RF", "RF-024")]
    public void RegisterAttention_WhenCancelled_ShouldReturnAppointmentNotPending()
    {
        var appointment = Appointment.Rehydrate(
            id: 100,
            scheduleSlotId: 10,
            patientId: 1,
            reason: "Dolor de cabeza",
            status: AppointmentStatus.Cancelled,
            diagnosis: null,
            prescription: null);

        var result = appointment.RegisterAttention("Diagnóstico", null);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("APPOINTMENT_NOT_PENDING");
        appointment.Status.Should().Be(AppointmentStatus.Cancelled);
        appointment.Diagnosis.Should().BeNull();
    }
}
