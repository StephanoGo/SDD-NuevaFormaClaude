using CitasMedicas.Domain.Common;

namespace CitasMedicas.Domain.Scheduling;

public sealed record TimeRange
{
    public TimeOnly Start { get; }
    public TimeOnly End { get; }

    private TimeRange(TimeOnly start, TimeOnly end)
    {
        Start = start;
        End = end;
    }

    public static Result<TimeRange> Create(TimeOnly start, TimeOnly end)
    {
        if (start >= end)
            return SchedulingErrors.InvalidTimeRange;

        return new TimeRange(start, end);
    }

    public bool Overlaps(TimeRange other)
    {
        return Start < other.End && other.Start < End;
    }
}
