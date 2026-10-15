using CitasMedicas.Domain.Common;

namespace CitasMedicas.Api.Http;

public static class ApiErrors
{
    public static Error ValidationFormat(string detail) =>
        Error.Validation("VALIDATION_FORMAT", $"Formato de solicitud inválido: {detail}");
}
