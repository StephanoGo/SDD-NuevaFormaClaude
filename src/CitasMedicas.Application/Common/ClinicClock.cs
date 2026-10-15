namespace CitasMedicas.Application.Common;

public sealed class ClinicClock
{
    private readonly TimeProvider _timeProvider;
    private readonly TimeZoneInfo _timeZone;

    public ClinicClock(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
        _timeZone = TimeZoneInfo.FindSystemTimeZoneById("America/Lima");
    }

    public DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(_timeProvider.GetUtcNow().UtcDateTime, _timeZone);
    public DateOnly Today => DateOnly.FromDateTime(Now);
    public TimeOnly CurrentTime => TimeOnly.FromDateTime(Now);
}
