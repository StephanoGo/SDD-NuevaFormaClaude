using CitasMedicas.Domain.Doctors;

namespace CitasMedicas.Infrastructure.Seed;

public static class DoctorSeed
{
    public static IReadOnlyList<Doctor> GetDoctors()
    {
        return
        [
            Doctor.Rehydrate(1, "CMP-012345", "Ana", "Torres Vega", "Medicina General", isActive: true),
            Doctor.Rehydrate(2, "CMP-023456", "Luis", "Paredes Soto", "Pediatría", isActive: true),
            Doctor.Rehydrate(3, "CMP-034567", "María", "Quispe Rojas", "Cardiología", isActive: false)
        ];
    }
}
