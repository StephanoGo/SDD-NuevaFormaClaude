namespace CitasMedicas.Application.Catalogs;

public sealed record SpecialtyWriteRequest(string? Name);
public sealed record SpecialtyResponse(int Id, string Name, bool IsActive);
public sealed record DoctorWriteRequest(string? LicenseNumber, string? FirstName, string? LastName, int? SpecialtyId);
public sealed record DoctorCatalogResponse(int Id, string LicenseNumber, string FirstName, string LastName, int SpecialtyId, string Specialty, bool IsActive);
