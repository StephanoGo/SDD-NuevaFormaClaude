using CitasMedicas.Application.Appointments;
using CitasMedicas.Application.Common;
using CitasMedicas.Application.Patients;
using CitasMedicas.Application.Scheduling;
using CitasMedicas.Infrastructure.Persistence.InMemory;
using CitasMedicas.Tests.Support;
using FluentAssertions;

namespace CitasMedicas.Tests.Application;

public class AppointmentServiceTests
{
    private readonly InMemoryDoctorRepository _doctorRepo = new([TestBuilders.CreateDoctor(id: 1, isActive: true)]);
    private readonly InMemoryPatientRepository _patientRepo = new();
    private readonly InMemoryAppointmentRepository _appointmentRepo = new();
    private readonly ScheduleService _scheduleService;
    private readonly PatientService _patientService;
    private readonly AppointmentService _appointmentService;

    public AppointmentServiceTests()
    {
        var fixedTime = FixedTimeProvider.CreateForLima(new DateTime(2026, 10, 15, 8, 30, 0));
        var clock = new ClinicClock(fixedTime);
        _scheduleService = new ScheduleService(_doctorRepo, _appointmentRepo, clock);
        _patientService = new PatientService(_patientRepo);
        _appointmentService = new AppointmentService(_appointmentRepo, _patientRepo, _doctorRepo, clock);
    }

    private async Task<(int patientId, int slotId)> CreatePatientAndSlotAsync()
    {
        var patientRes = await _patientService.RegisterAsync(new RegisterPatientRequest(
            "45678912", "Rosa", "García López", "+51 987 654 321", "rosa.garcia@correo.pe"));

        var slotRes = await _scheduleService.CreateSlotAsync(
            doctorId: 1,
            new DateOnly(2026, 10, 15),
            new TimeOnly(9, 0),
            new TimeOnly(9, 30));

        return (patientRes.Value.Id, slotRes.Value.Id);
    }

    [Fact]
    [Trait("HU", "HU-03")]
    [Trait("RF", "RF-015")]
    public async Task HU03_Scenario1_ReserveAppointment_WhenValid_ShouldCreatePendingAppointmentAndReserveSlot()
    {
        var (patientId, slotId) = await CreatePatientAndSlotAsync();

        var result = await _appointmentService.ReserveAsync(slotId, patientId, "Dolor de cabeza recurrente");

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be("Pending");
        result.Value.Reason.Should().Be("Dolor de cabeza recurrente");
        result.Value.Diagnosis.Should().BeNull();
        result.Value.Prescription.Should().BeNull();
        result.Value.Patient.Id.Should().Be(patientId);
        result.Value.Doctor.Id.Should().Be(1);
        result.Value.Slot.Id.Should().Be(slotId);

        var slot = await _appointmentRepo.GetSlotByIdAsync(slotId);
        slot!.Status.Should().Be(CitasMedicas.Domain.Scheduling.ScheduleSlotStatus.Reserved);
    }

    [Fact]
    [Trait("HU", "HU-03")]
    [Trait("RF", "RF-016")]
    public async Task HU03_Scenario2_ReserveAppointment_WhenSlotAlreadyReserved_ShouldReturnSlotNotAvailable()
    {
        var (patientId, slotId) = await CreatePatientAndSlotAsync();
        await _appointmentService.ReserveAsync(slotId, patientId, "Primera reserva");

        var secondReserve = await _appointmentService.ReserveAsync(slotId, patientId, "Segunda reserva");

        secondReserve.IsFailure.Should().BeTrue();
        secondReserve.Error.Code.Should().Be("SLOT_NOT_AVAILABLE");
    }

    [Fact]
    [Trait("HU", "HU-03")]
    [Trait("RF", "RF-017")]
    public async Task HU03_Scenario3_ReserveAppointment_WhenPatientNotFound_ShouldReturnPatientNotFound()
    {
        var (_, slotId) = await CreatePatientAndSlotAsync();

        var result = await _appointmentService.ReserveAsync(slotId, patientId: 9999, "Consulta");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("PATIENT_NOT_FOUND");
    }

    [Fact]
    [Trait("RF", "RF-017")]
    public async Task ReserveAppointment_WhenSlotNotFound_ShouldReturnSlotNotFound()
    {
        var (patientId, _) = await CreatePatientAndSlotAsync();

        var result = await _appointmentService.ReserveAsync(scheduleSlotId: 9999, patientId, "Consulta");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("SLOT_NOT_FOUND");
    }

    [Fact]
    [Trait("HU", "HU-03")]
    [Trait("RF", "RF-018")]
    public async Task HU03_Scenario5_ReserveAppointment_WithoutReason_ShouldReturnAppointmentReasonRequired()
    {
        var (patientId, slotId) = await CreatePatientAndSlotAsync();

        var result = await _appointmentService.ReserveAsync(slotId, patientId, "   ");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("APPOINTMENT_REASON_REQUIRED");
    }

    [Fact]
    [Trait("HU", "HU-04")]
    [Trait("RF", "RF-022")]
    public async Task HU04_Scenario1_RegisterAttention_WithDiagnosisAndPrescription_ShouldTransitionToAttended()
    {
        var (patientId, slotId) = await CreatePatientAndSlotAsync();
        var appt = (await _appointmentService.ReserveAsync(slotId, patientId, "Control")).Value;

        var result = await _appointmentService.RegisterAttentionAsync(
            appt.Id,
            "Migraña tensional",
            "Paracetamol 500 mg c/8h por 3 días");

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be("Attended");
        result.Value.Diagnosis.Should().Be("Migraña tensional");
        result.Value.Prescription.Should().Be("Paracetamol 500 mg c/8h por 3 días");
    }

    [Fact]
    [Trait("HU", "HU-04")]
    [Trait("RF", "RF-022")]
    public async Task HU04_Scenario2_RegisterAttention_WithDiagnosisAndNoPrescription_ShouldTransitionToAttendedWithNullPrescription()
    {
        var (patientId, slotId) = await CreatePatientAndSlotAsync();
        var appt = (await _appointmentService.ReserveAsync(slotId, patientId, "Control")).Value;

        var result = await _appointmentService.RegisterAttentionAsync(
            appt.Id,
            "Faringitis aguda",
            prescription: null);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be("Attended");
        result.Value.Diagnosis.Should().Be("Faringitis aguda");
        result.Value.Prescription.Should().BeNull();
    }

    [Fact]
    [Trait("HU", "HU-04")]
    [Trait("RF", "RF-023")]
    public async Task HU04_Scenario3_RegisterAttention_WithoutDiagnosis_ShouldReturnAttentionDiagnosisRequired()
    {
        var (patientId, slotId) = await CreatePatientAndSlotAsync();
        var appt = (await _appointmentService.ReserveAsync(slotId, patientId, "Control")).Value;

        var result = await _appointmentService.RegisterAttentionAsync(
            appt.Id,
            diagnosis: "   ",
            prescription: "Paracetamol");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ATTENTION_DIAGNOSIS_REQUIRED");
    }

    [Fact]
    [Trait("HU", "HU-04")]
    [Trait("RF", "RF-024")]
    [Trait("RF", "RF-025")]
    public async Task HU04_Scenario4_RegisterAttention_WhenAlreadyAttended_ShouldReturnAppointmentNotPending()
    {
        var (patientId, slotId) = await CreatePatientAndSlotAsync();
        var appt = (await _appointmentService.ReserveAsync(slotId, patientId, "Control")).Value;

        await _appointmentService.RegisterAttentionAsync(appt.Id, "Diagnóstico original", "Receta original");

        var secondAttempt = await _appointmentService.RegisterAttentionAsync(appt.Id, "Nuevo diagnóstico", "Nueva receta");

        secondAttempt.IsFailure.Should().BeTrue();
        secondAttempt.Error.Code.Should().Be("APPOINTMENT_NOT_PENDING");

        var check = await _appointmentService.GetByIdAsync(appt.Id);
        check.Value.Diagnosis.Should().Be("Diagnóstico original");
        check.Value.Prescription.Should().Be("Receta original");
    }

    [Fact]
    [Trait("RF", "RF-026")]
    public async Task GetByIdAsync_ShouldReturnFullCompositeResponse()
    {
        var (patientId, slotId) = await CreatePatientAndSlotAsync();
        var appt = (await _appointmentService.ReserveAsync(slotId, patientId, "Consulta general")).Value;

        var result = await _appointmentService.GetByIdAsync(appt.Id);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(appt.Id);
        result.Value.Status.Should().Be("Pending");
        result.Value.Reason.Should().Be("Consulta general");
        result.Value.Patient.Dni.Should().Be("45678912");
        result.Value.Patient.FullName.Should().Be("Rosa García López");
        result.Value.Doctor.FullName.Should().Be("Ana Torres Vega");
        result.Value.Doctor.Specialty.Should().Be("Medicina General");
        result.Value.Slot.Date.Should().Be("2026-10-15");
        result.Value.Slot.StartTime.Should().Be("09:00");
        result.Value.Slot.EndTime.Should().Be("09:30");
    }
}
