using CitasMedicas.Domain.Scheduling;
using CitasMedicas.Tests.Support;
using FluentAssertions;

namespace CitasMedicas.Tests.Domain.Scheduling;

public class ScheduleSlotCreateTests
{
    private readonly DateTime _now = new(2026, 10, 15, 8, 30, 0);

    [Fact]
    [Trait("RF", "RF-009")]
    public void Create_ValidSlot_ShouldBeInAvailableStatus()
    {
        var doctor = TestBuilders.CreateDoctor(isActive: true);
        var date = new DateOnly(2026, 10, 15);
        var range = TimeRange.Create(new TimeOnly(9, 0), new TimeOnly(9, 30)).Value;

        var result = ScheduleSlot.Create(doctor, date, range, _now, []);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(ScheduleSlotStatus.Available);
        result.Value.DoctorId.Should().Be(doctor.Id);
        result.Value.Date.Should().Be(date);
        result.Value.StartTime.Should().Be(range.Start);
        result.Value.EndTime.Should().Be(range.End);
    }

    [Fact]
    [Trait("RF", "RF-010")]
    public void Create_WithInactiveDoctor_ShouldReturnDoctorNotAvailable()
    {
        var doctor = TestBuilders.CreateDoctor(isActive: false);
        var date = new DateOnly(2026, 10, 15);
        var range = TimeRange.Create(new TimeOnly(9, 0), new TimeOnly(9, 30)).Value;

        var result = ScheduleSlot.Create(doctor, date, range, _now, []);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("DOCTOR_NOT_AVAILABLE");
    }

    [Fact]
    [Trait("RF", "RF-012")]
    public void Create_WithPastDate_ShouldReturnSlotInPast()
    {
        var doctor = TestBuilders.CreateDoctor(isActive: true);
        var pastDate = new DateOnly(2026, 10, 14);
        var range = TimeRange.Create(new TimeOnly(9, 0), new TimeOnly(9, 30)).Value;

        var result = ScheduleSlot.Create(doctor, pastDate, range, _now, []);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("SLOT_IN_PAST");
    }

    [Fact]
    [Trait("RF", "RF-012")]
    public void Create_TodayWithPastStartTime_ShouldReturnSlotInPast()
    {
        var doctor = TestBuilders.CreateDoctor(isActive: true);
        var today = new DateOnly(2026, 10, 15);
        // _now is 08:30, slot starts at 08:00
        var range = TimeRange.Create(new TimeOnly(8, 0), new TimeOnly(8, 30)).Value;

        var result = ScheduleSlot.Create(doctor, today, range, _now, []);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("SLOT_IN_PAST");
    }

    [Fact]
    [Trait("RF", "RF-012")]
    public void Create_TodayWithFutureStartTime_ShouldSucceed()
    {
        var doctor = TestBuilders.CreateDoctor(isActive: true);
        var today = new DateOnly(2026, 10, 15);
        // _now is 08:30, slot starts at 08:45
        var range = TimeRange.Create(new TimeOnly(8, 45), new TimeOnly(9, 15)).Value;

        var result = ScheduleSlot.Create(doctor, today, range, _now, []);

        result.IsSuccess.Should().BeTrue();
    }

    [Theory]
    [Trait("RF", "RF-013")]
    [InlineData(ScheduleSlotStatus.Available)]
    [InlineData(ScheduleSlotStatus.Reserved)]
    public void Create_WhenOverlapsWithExistingSlot_ShouldReturnSlotOverlap(ScheduleSlotStatus existingStatus)
    {
        var doctor = TestBuilders.CreateDoctor(id: 1, isActive: true);
        var today = new DateOnly(2026, 10, 15);
        var existingSlot = TestBuilders.CreateSlot(
            id: 1,
            doctorId: 1,
            date: today,
            startTime: new TimeOnly(9, 0),
            endTime: new TimeOnly(9, 30),
            status: existingStatus);

        var overlappingRange = TimeRange.Create(new TimeOnly(9, 15), new TimeOnly(9, 45)).Value;

        var result = ScheduleSlot.Create(doctor, today, overlappingRange, _now, [existingSlot]);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("SLOT_OVERLAP");
    }

    [Fact]
    [Trait("RF", "RF-013")]
    public void Create_SameTimeWithDifferentDoctor_ShouldSucceed()
    {
        var doctor2 = TestBuilders.CreateDoctor(id: 2, isActive: true);
        var today = new DateOnly(2026, 10, 15);
        var doctor1Slot = TestBuilders.CreateSlot(
            id: 1,
            doctorId: 1,
            date: today,
            startTime: new TimeOnly(9, 0),
            endTime: new TimeOnly(9, 30));

        var sameRange = TimeRange.Create(new TimeOnly(9, 0), new TimeOnly(9, 30)).Value;

        var result = ScheduleSlot.Create(doctor2, today, sameRange, _now, [doctor1Slot]);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    [Trait("RF", "RF-013")]
    public void Create_SameTimeWithDifferentDate_ShouldSucceed()
    {
        var doctor = TestBuilders.CreateDoctor(id: 1, isActive: true);
        var today = new DateOnly(2026, 10, 15);
        var tomorrow = new DateOnly(2026, 10, 16);
        var todaySlot = TestBuilders.CreateSlot(
            id: 1,
            doctorId: 1,
            date: today,
            startTime: new TimeOnly(9, 0),
            endTime: new TimeOnly(9, 30));

        var sameRange = TimeRange.Create(new TimeOnly(9, 0), new TimeOnly(9, 30)).Value;

        var result = ScheduleSlot.Create(doctor, tomorrow, sameRange, _now, [todaySlot]);

        result.IsSuccess.Should().BeTrue();
    }
}
