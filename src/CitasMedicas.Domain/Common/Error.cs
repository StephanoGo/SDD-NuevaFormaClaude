namespace CitasMedicas.Domain.Common;

public sealed record Error(string Code, string Message, ErrorKind Kind)
{
    public static Error Validation(string code, string message) => new(code, message, ErrorKind.Validation);
    public static Error NotFound(string code, string message) => new(code, message, ErrorKind.NotFound);
    public static Error Conflict(string code, string message) => new(code, message, ErrorKind.Conflict);
}
