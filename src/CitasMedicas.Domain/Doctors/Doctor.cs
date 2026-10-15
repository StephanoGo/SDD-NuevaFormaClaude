namespace CitasMedicas.Domain.Doctors;

public sealed class Doctor
{
    public int Id { get; private set; }
    public string LicenseNumber { get; private set; }
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public string Specialty { get; private set; }
    public bool IsActive { get; private set; }

    public string FullName => $"{FirstName} {LastName}".Trim();

    private Doctor(int id, string licenseNumber, string firstName, string lastName, string specialty, bool isActive)
    {
        Id = id;
        LicenseNumber = licenseNumber;
        FirstName = firstName;
        LastName = lastName;
        Specialty = specialty;
        IsActive = isActive;
    }

    internal void AssignId(int id)
    {
        if (Id != 0)
            throw new InvalidOperationException("Entity already has an Id assigned.");
        if (id <= 0)
            throw new ArgumentOutOfRangeException(nameof(id), "Id must be greater than zero.");
        Id = id;
    }

    internal static Doctor Rehydrate(int id, string licenseNumber, string firstName, string lastName, string specialty, bool isActive)
    {
        return new Doctor(id, licenseNumber, firstName, lastName, specialty, isActive);
    }
}
