using CitasMedicas.Domain.Scheduling;
using FluentAssertions;

namespace CitasMedicas.Tests.Domain.Scheduling;

public class TimeRangeTests
{
    [Theory]
    [Trait("RF", "RF-013")]
    [InlineData("09:00", "09:30", "08:45", "09:15", true)]  // Parcial al inicio
    [InlineData("09:00", "09:30", "09:15", "09:45", true)]  // Parcial al final
    [InlineData("09:00", "10:00", "09:15", "09:45", true)]  // Contenido
    [InlineData("09:15", "09:45", "09:00", "10:00", true)]  // Contenedor
    [InlineData("09:00", "09:30", "09:00", "09:30", true)]  // Idéntico
    [InlineData("09:00", "09:30", "09:30", "10:00", false)] // Contiguo después
    [InlineData("09:30", "10:00", "09:00", "09:30", false)] // Contiguo antes
    public void Overlaps_Evaluation_MatchesSpecification(
        string existingStart,
        string existingEnd,
        string newStart,
        string newEnd,
        bool expectedOverlap)
    {
        var existing = TimeRange.Create(TimeOnly.Parse(existingStart), TimeOnly.Parse(existingEnd)).Value;
        var incoming = TimeRange.Create(TimeOnly.Parse(newStart), TimeOnly.Parse(newEnd)).Value;

        var actualOverlap = existing.Overlaps(incoming);

        actualOverlap.Should().Be(expectedOverlap);
    }

    [Theory]
    [Trait("RF", "RF-011")]
    [InlineData("09:00", "09:00")] // start == end
    [InlineData("10:00", "09:30")] // start > end
    public void Create_WithInvalidRange_ShouldReturnSlotInvalidTimeRange(string start, string end)
    {
        var result = TimeRange.Create(TimeOnly.Parse(start), TimeOnly.Parse(end));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("SLOT_INVALID_TIME_RANGE");
    }

    [Fact]
    [Trait("RF", "RF-011")]
    public void Create_WithValidRange_ShouldSucceed()
    {
        var result = TimeRange.Create(new TimeOnly(9, 0), new TimeOnly(9, 30));

        result.IsSuccess.Should().BeTrue();
        result.Value.Start.Should().Be(new TimeOnly(9, 0));
        result.Value.End.Should().Be(new TimeOnly(9, 30));
    }
}
