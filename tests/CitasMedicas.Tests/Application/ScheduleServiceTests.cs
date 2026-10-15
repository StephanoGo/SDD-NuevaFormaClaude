using CitasMedicas.Application.Common;
using CitasMedicas.Application.Scheduling;
using CitasMedicas.Infrastructure.Persistence.InMemory;
using CitasMedicas.Tests.Support;
using FluentAssertions;

namespace CitasMedicas.Tests.Application;

public class ScheduleServiceTests
{
    private readonly InMemoryDoctorRepository _doctorRepo = new();
    private readonly InMemoryAppointmentRepository _appointmentRepo = new();
    private readonly ScheduleService _service;

    public ScheduleServiceTests()
    {
        var fixedTime = FixedTimeProvider.CreateForLima(new DateTime(2026, 10, 15, 8, 30, 0));
        var clock = new ClinicClock(fixedTime);
        _service = new ScheduleService(_doctorRepo, _appointmentRepo, clock);
    }

    [Fact]
    [Trait("HU", "HU-02")]
    [Trait("RF", "RF-009")]
    public async Task HU02_Scenario1_CreateSlot_WithActiveDoctorAndNoSlots_ShouldBeAvailable()
    {
        var date = new DateOnly(2026, 10, 15);
        var start = new TimeOnly(9, 0);
        var end = new TimeOnly(9, 30);

        var result = await _service.CreateSlotAsync(doctorId: 1, date, start, end);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be("Available");
        result.Value.DoctorId.Should().Be(1);
        result.Value.Date.Should().Be("2026-10-15");
        result.Value.StartTime.Should().Be("09:00");
        result.Value.EndTime.Should().Be("09:30");
    }

    [Fact]
    [Trait("HU", "HU-02")]
    [Trait("RF", "RF-010")]
    public async Task HU02_Scenario2_CreateSlot_WithInactiveDoctor_ShouldReturnDoctorNotAvailable()
    {
        // Doctor 3 is inactive in seed (María Quispe)
        var date = new DateOnly(2026, 10, 15);
        var start = new TimeOnly(9, 0);
        var end = new TimeOnly(9, 30);

        var result = await _service.CreateSlotAsync(doctorId: 3, date, start, end);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("DOCTOR_NOT_AVAILABLE");
    }

    [Fact]
    [Trait("HU", "HU-02")]
    [Trait("RF", "RF-011")]
    public async Task HU02_Scenario3_CreateSlot_WithStartTimeGreaterThanEndTime_ShouldReturnInvalidTimeRange()
    {
        var date = new DateOnly(2026, 10, 15);
        var start = new TimeOnly(10, 0);
        var end = new TimeOnly(9, 30);

        var result = await _service.CreateSlotAsync(doctorId: 1, date, start, end);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("SLOT_INVALID_TIME_RANGE");
    }

    [Fact]
    [Trait("HU", "HU-02")]
    [Trait("RF", "RF-013")]
    public async Task HU02_Scenario4_CreateSlot_WhenOverlappingWithExistingSlot_ShouldReturnSlotOverlap()
    {
        var date = new DateOnly(2026, 10, 15);
        await _service.CreateSlotAsync(doctorId: 1, date, new TimeOnly(9, 0), new TimeOnly(9, 30));

        var result = await _service.CreateSlotAsync(doctorId: 1, date, new TimeOnly(9, 15), new TimeOnly(9, 45));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("SLOT_OVERLAP");
    }

    [Fact]
    [Trait("HU", "HU-02")]
    [Trait("RF", "RF-012")]
    public async Task HU02_Scenario5_CreateSlot_WithPastDate_ShouldReturnSlotInPast()
    {
        var pastDate = new DateOnly(2026, 10, 14);

        var result = await _service.CreateSlotAsync(doctorId: 1, pastDate, new TimeOnly(9, 0), new TimeOnly(9, 30));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("SLOT_IN_PAST");
    }

    [Fact]
    [Trait("RF", "RF-014")]
    public async Task GetDoctorScheduleAsync_ShouldReturnSlotsOrderedByStartTime()
    {
        var date = new DateOnly(2026, 10, 16);
        await _service.CreateSlotAsync(doctorId: 1, date, new TimeOnly(11, 0), new TimeOnly(11, 30));
        await _service.CreateSlotAsync(doctorId: 1, date, new TimeOnly(9, 0), new TimeOnly(9, 30));
        await _service.CreateSlotAsync(doctorId: 1, date, new TimeOnly(10, 0), new TimeOnly(10, 30));

        var schedule = await _service.GetDoctorScheduleAsync(doctorId: 1, date);

        schedule.Select(s => s.StartTime).Should().ContainInOrder("09:00", "10:00", "11:00");
    }
}
