using CitasMedicas.Domain.Appointments;
using CitasMedicas.Domain.Scheduling;
using CitasMedicas.Tests.Support;
using FluentAssertions;

namespace CitasMedicas.Tests.Domain.Scheduling;

public class ScheduleSlotReserveTests
{
    private readonly DateTime _now = new(2026, 10, 15, 8, 30, 0);

    [Fact]
    [Trait("RF", "RF-015")]
    public void Reserve_ValidSlot_ShouldReserveSlotAndReturnPendingAppointment()
    {
        var doctor = TestBuilders.CreateDoctor(id: 1, isActive: true);
        var patient = TestBuilders.CreatePatient(id: 5);
        var slot = TestBuilders.CreateSlot(id: 10, doctorId: 1, date: new DateOnly(2026, 10, 15), startTime: new TimeOnly(9, 0), endTime: new TimeOnly(9, 30));

        var result = slot.Reserve(doctor, patient, "  Dolor de cabeza recurrente  ", _now);

        result.IsSuccess.Should().BeTrue();
        slot.Status.Should().Be(ScheduleSlotStatus.Reserved);
        result.Value.ScheduleSlotId.Should().Be(10);
        result.Value.PatientId.Should().Be(5);
        result.Value.Reason.Should().Be("Dolor de cabeza recurrente");
        result.Value.Status.Should().Be(AppointmentStatus.Pending);
        result.Value.Diagnosis.Should().BeNull();
        result.Value.Prescription.Should().BeNull();
    }

    [Fact]
    [Trait("RF", "RF-016")]
    public void Reserve_AlreadyReservedSlot_ShouldReturnSlotNotAvailableAndRemainReserved()
    {
        var doctor = TestBuilders.CreateDoctor(id: 1, isActive: true);
        var patient = TestBuilders.CreatePatient(id: 5);
        var slot = TestBuilders.CreateSlot(
            id: 10,
            doctorId: 1,
            date: new DateOnly(2026, 10, 15),
            startTime: new TimeOnly(9, 0),
            endTime: new TimeOnly(9, 30),
            status: ScheduleSlotStatus.Reserved);

        var result = slot.Reserve(doctor, patient, "Consulta general", _now);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("SLOT_NOT_AVAILABLE");
        slot.Status.Should().Be(ScheduleSlotStatus.Reserved);
    }

    [Fact]
    [Trait("RF", "RF-019")]
    public void Reserve_InactiveDoctor_ShouldReturnSlotNotAvailableAndRemainAvailable()
    {
        var doctor = TestBuilders.CreateDoctor(id: 1, isActive: false);
        var patient = TestBuilders.CreatePatient(id: 5);
        var slot = TestBuilders.CreateSlot(
            id: 10,
            doctorId: 1,
            date: new DateOnly(2026, 10, 15),
            startTime: new TimeOnly(9, 0),
            endTime: new TimeOnly(9, 30));

        var result = slot.Reserve(doctor, patient, "Consulta general", _now);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("SLOT_NOT_AVAILABLE");
        slot.Status.Should().Be(ScheduleSlotStatus.Available);
    }

    [Fact]
    [Trait("RF", "RF-019")]
    public void Reserve_AlreadyStartedSlot_ShouldReturnSlotNotAvailableAndRemainAvailable()
    {
        var doctor = TestBuilders.CreateDoctor(id: 1, isActive: true);
        var patient = TestBuilders.CreatePatient(id: 5);
        // _now is 08:30, slot is 08:00 - 08:30 (already started)
        var slot = TestBuilders.CreateSlot(
            id: 10,
            doctorId: 1,
            date: new DateOnly(2026, 10, 15),
            startTime: new TimeOnly(8, 0),
            endTime: new TimeOnly(8, 30));

        var result = slot.Reserve(doctor, patient, "Consulta general", _now);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("SLOT_NOT_AVAILABLE");
        slot.Status.Should().Be(ScheduleSlotStatus.Available);
    }

    [Theory]
    [Trait("RF", "RF-018")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Reserve_WithEmptyReason_ShouldReturnAppointmentReasonRequiredAndRemainAvailable(string? emptyReason)
    {
        var doctor = TestBuilders.CreateDoctor(id: 1, isActive: true);
        var patient = TestBuilders.CreatePatient(id: 5);
        var slot = TestBuilders.CreateSlot(
            id: 10,
            doctorId: 1,
            date: new DateOnly(2026, 10, 15),
            startTime: new TimeOnly(9, 0),
            endTime: new TimeOnly(9, 30));

        var result = slot.Reserve(doctor, patient, emptyReason, _now);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("APPOINTMENT_REASON_REQUIRED");
        slot.Status.Should().Be(ScheduleSlotStatus.Available);
    }
}
