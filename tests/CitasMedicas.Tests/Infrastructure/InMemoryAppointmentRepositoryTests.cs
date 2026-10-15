using CitasMedicas.Domain.Appointments;
using CitasMedicas.Domain.Scheduling;
using CitasMedicas.Infrastructure.Persistence.InMemory;
using CitasMedicas.Tests.Support;
using FluentAssertions;

namespace CitasMedicas.Tests.Infrastructure;

public class InMemoryAppointmentRepositoryTests
{
    [Fact]
    [Trait("RF", "RF-025")]
    public async Task Repository_ShouldReturnCopies_MutatingReturnedEntityDoesNotAlterPersistedState()
    {
        var repo = new InMemoryAppointmentRepository();
        var doctor = TestBuilders.CreateDoctor(id: 1, isActive: true);
        var slot = TestBuilders.CreateSlot(id: 0, doctorId: 1, date: new DateOnly(2026, 10, 15), startTime: new TimeOnly(9, 0), endTime: new TimeOnly(9, 30));

        var addResult = await repo.AddSlotAsync(slot);
        addResult.IsSuccess.Should().BeTrue();
        var returnedSlot = addResult.Value;

        // Try to mutate or verify that getting it again returns a clean independent copy
        var fetched1 = await repo.GetSlotByIdAsync(returnedSlot.Id);
        var fetched2 = await repo.GetSlotByIdAsync(returnedSlot.Id);

        fetched1.Should().NotBeNull();
        fetched2.Should().NotBeNull();
        fetched1.Should().NotBeSameAs(fetched2);
    }

    [Fact]
    [Trait("RF", "RF-013")]
    public async Task AddSlotAsync_ConcurrentOverlappingSlots_ShouldAllowExactlyOneSuccess()
    {
        var repo = new InMemoryAppointmentRepository();
        var date = new DateOnly(2026, 10, 15);
        const int concurrencyLevel = 10;

        var tasks = Enumerable.Range(1, concurrencyLevel).Select(async i =>
        {
            var slot = TestBuilders.CreateSlot(
                id: 0,
                doctorId: 1,
                date: date,
                startTime: new TimeOnly(9, 0),
                endTime: new TimeOnly(9, 30));
            return await repo.AddSlotAsync(slot);
        });

        var results = await Task.WhenAll(tasks);

        results.Count(r => r.IsSuccess).Should().Be(1);
        results.Count(r => r.IsFailure && r.Error.Code == "SLOT_OVERLAP").Should().Be(concurrencyLevel - 1);
    }

    [Fact]
    [Trait("RF", "RF-024")]
    [Trait("RF", "RF-025")]
    public async Task RegisterAttentionAsync_WhenCalledTwice_SecondCallShouldReturnAppointmentNotPending()
    {
        var repo = new InMemoryAppointmentRepository();
        var slot = TestBuilders.CreateSlot(id: 0, doctorId: 1, date: new DateOnly(2026, 10, 15), startTime: new TimeOnly(9, 0), endTime: new TimeOnly(9, 30));
        var addedSlot = (await repo.AddSlotAsync(slot)).Value;

        var apptResult = await repo.ReserveAsync(
            addedSlot,
            Appointment.Rehydrate(0, addedSlot.Id, 1, "Dolor", AppointmentStatus.Pending, null, null));
        apptResult.IsSuccess.Should().BeTrue();
        var appt = apptResult.Value;

        // Register attention first time
        appt.RegisterAttention("Diagnóstico A", "Receta A");
        var firstUpdate = await repo.RegisterAttentionAsync(appt);
        firstUpdate.IsSuccess.Should().BeTrue();
        firstUpdate.Value.Status.Should().Be(AppointmentStatus.Attended);

        // Try second time
        var secondAttempt = Appointment.Rehydrate(appt.Id, addedSlot.Id, 1, "Dolor", AppointmentStatus.Attended, "Diagnóstico B", "Receta B");
        var secondUpdate = await repo.RegisterAttentionAsync(secondAttempt);

        secondUpdate.IsFailure.Should().BeTrue();
        secondUpdate.Error.Code.Should().Be("APPOINTMENT_NOT_PENDING");

        var persisted = await repo.GetAppointmentByIdAsync(appt.Id);
        persisted!.Diagnosis.Should().Be("Diagnóstico A");
        persisted.Prescription.Should().Be("Receta A");
    }
}
