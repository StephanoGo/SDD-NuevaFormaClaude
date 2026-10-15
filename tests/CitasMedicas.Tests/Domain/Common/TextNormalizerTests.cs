using CitasMedicas.Domain.Common;
using FluentAssertions;

namespace CitasMedicas.Tests.Domain.Common;

public class TextNormalizerTests
{
    [Theory]
    [Trait("RF", "RF-007")]
    [InlineData("Núñez", "NUNEZ")]
    [InlineData("garcía", "GARCIA")]
    [InlineData("  Pérez  ", "PEREZ")]
    [InlineData("", "")]
    [InlineData("   ", "")]
    [InlineData(null, "")]
    public void ForSearch_ShouldNormalizeAndUppercaseWithoutAccents(string? input, string expected)
    {
        var result = TextNormalizer.ForSearch(input);

        result.Should().Be(expected);
    }
}
