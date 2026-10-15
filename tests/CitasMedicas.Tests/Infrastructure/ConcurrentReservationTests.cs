using CitasMedicas.Domain.Appointments;
using CitasMedicas.Domain.Scheduling;
using CitasMedicas.Infrastructure.Persistence.InMemory;
using CitasMedicas.Tests.Support;
using FluentAssertions;

namespace CitasMedicas.Tests.Infrastructure;

public class ConcurrentReservationTests
{
    [Fact]
    [Trait("RF", "RF-020")]
    [Trait("RF", "RF-021")]
    public async Task ReserveAsync_With50ConcurrentTasksOnSameSlot_ShouldHaveExactlyOneSuccess()
    {
        var repo = new InMemoryAppointmentRepository();
        var slot = TestBuilders.CreateSlot(
            id: 0,
            doctorId: 1,
            date: new DateOnly(2026, 10, 15),
            startTime: new TimeOnly(9, 0),
            endTime: new TimeOnly(9, 30));

        var addSlotResult = await repo.AddSlotAsync(slot);
        addSlotResult.IsSuccess.Should().BeTrue();
        var persistentSlot = addSlotResult.Value;

        const int concurrencyLevel = 50;

        // Use a barrier / Task.WhenAll to maximize race condition
        var tasks = Enumerable.Range(1, concurrencyLevel).Select(async i =>
        {
            await Task.Yield();
            var appt = Appointment.Rehydrate(0, persistentSlot.Id, patientId: i, $"Motivo {i}", AppointmentStatus.Pending, null, null);
            return await repo.ReserveAsync(persistentSlot, appt);
        });

        var results = await Task.WhenAll(tasks);

        // Exactly 1 success, 49 failures with SLOT_NOT_AVAILABLE
        results.Count(r => r.IsSuccess).Should().Be(1);
        results.Count(r => r.IsFailure && r.Error.Code == "SLOT_NOT_AVAILABLE").Should().Be(concurrencyLevel - 1);

        // Verify slot is Reserved
        var storedSlot = await repo.GetSlotByIdAsync(persistentSlot.Id);
        storedSlot.Should().NotBeNull();
        storedSlot!.Status.Should().Be(ScheduleSlotStatus.Reserved);

        // Verify exactly one appointment stored
        var winningAppt = results.Single(r => r.IsSuccess).Value;
        var storedAppt = await repo.GetAppointmentByIdAsync(winningAppt.Id);
        storedAppt.Should().NotBeNull();
        storedAppt!.ScheduleSlotId.Should().Be(persistentSlot.Id);
    }
}
