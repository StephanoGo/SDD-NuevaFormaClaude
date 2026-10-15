namespace CitasMedicas.Tests.Support;

public sealed class FixedTimeProvider : TimeProvider
{
    private readonly DateTimeOffset _utcNow;

    public FixedTimeProvider(DateTimeOffset utcNow)
    {
        _utcNow = utcNow;
    }

    public override DateTimeOffset GetUtcNow() => _utcNow;

    public static FixedTimeProvider CreateForLima(DateTime localDateTime)
    {
        var limaZone = TimeZoneInfo.FindSystemTimeZoneById("America/Lima");
        var offset = limaZone.GetUtcOffset(localDateTime);
        var dto = new DateTimeOffset(localDateTime, offset);
        return new FixedTimeProvider(dto.ToUniversalTime());
    }
}
