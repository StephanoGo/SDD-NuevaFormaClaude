using CitasMedicas.Domain.Doctors;
using CitasMedicas.Domain.Patients;
using CitasMedicas.Domain.Scheduling;

namespace CitasMedicas.Tests.Support;

public static class TestBuilders
{
    public static readonly DateTime ReferenceNow = new(2026, 10, 15, 8, 30, 0, DateTimeKind.Unspecified);

    public static Doctor CreateDoctor(
        int id = 1,
        bool isActive = true,
        string licenseNumber = "CMP-012345",
        string firstName = "Ana",
        string lastName = "Torres Vega",
        string specialty = "Medicina General")
    {
        return Doctor.Rehydrate(id, licenseNumber, firstName, lastName, specialty, isActive);
    }

    public static Patient CreatePatient(
        int id = 1,
        string dni = "45678912",
        string firstName = "Rosa",
        string lastName = "García López",
        string phone = "+51 987 654 321",
        string? email = "rosa.garcia@correo.pe")
    {
        return Patient.Rehydrate(id, dni, firstName, lastName, phone, email);
    }

    public static ScheduleSlot CreateSlot(
        int id = 10,
        int doctorId = 1,
        DateOnly? date = null,
        TimeOnly? startTime = null,
        TimeOnly? endTime = null,
        ScheduleSlotStatus status = ScheduleSlotStatus.Available)
    {
        var slotDate = date ?? new DateOnly(2026, 10, 15);
        var start = startTime ?? new TimeOnly(9, 0);
        var end = endTime ?? new TimeOnly(9, 30);
        return ScheduleSlot.Rehydrate(id, doctorId, slotDate, start, end, status);
    }
}
