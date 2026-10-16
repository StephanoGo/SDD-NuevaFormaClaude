using FluentAssertions;

namespace CitasMedicas.Tests.Infrastructure;

public sealed class SqlTestCatalogPolicyTests
{
    private static readonly IReadOnlySet<string> KnownMigrations =
        new HashSet<string>(["202610150001_InitialClinicSchema"], StringComparer.Ordinal);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not a SQL Server connection string")]
    [InlineData("Server=localhost;Database=BD_MedicinaGen;Integrated Security=True")]
    [InlineData("Server=remote-server;Database=BD_MedicinaGen_Tests;Integrated Security=True")]
    public void ConnectionValidation_RejectsMissingMalformedOrUnapprovedConfiguration(string? connectionString)
    {
        SqlTestCatalogPolicy.TryParseApprovedConnectionString(connectionString, out var builder).Should().BeFalse();
        builder.Should().BeNull();
    }

    [Fact]
    public void ConnectionValidation_AcceptsOnlyApprovedDestination()
    {
        var parsed = SqlTestCatalogPolicy.TryParseApprovedConnectionString(
            "Server=localhost;Database=BD_MedicinaGen_Tests;Integrated Security=True",
            out var builder);

        parsed.Should().BeTrue();
        builder!.DataSource.Should().Be("localhost");
        builder.InitialCatalog.Should().Be("BD_MedicinaGen_Tests");
    }

    [Theory]
    [InlineData("localhost", "BD_MedicinaGen_Tests", true)]
    [InlineData("localhost", "BD_MedicinaGen", false)]
    [InlineData("localhost", "master", false)]
    [InlineData("remote-server", "BD_MedicinaGen_Tests", false)]
    public void DestinationValidation_RejectsNonApprovedTargetsWithoutOpeningConnection(
        string server, string catalog, bool expected)
    {
        SqlTestCatalogPolicy.IsApprovedDestination(server, catalog).Should().Be(expected);
    }

    [Fact]
    public void CatalogPolicy_AllowsOnlyConfirmedEmptyCatalogOrRecognizedHistory()
    {
        SqlTestCatalogPolicy.ShouldBlockBeforeMigration(
            true, 0, false, false, false, [], KnownMigrations).Should().BeFalse();

        // EF history outside dbo is discovered and rejected before MigrateAsync.
        SqlTestCatalogPolicy.ShouldBlockBeforeMigration(
            true, 1, true, false, true, [], KnownMigrations).Should().BeTrue();

        // A business table without migration history is rejected.
        SqlTestCatalogPolicy.ShouldBlockBeforeMigration(
            true, 1, false, false, false, [], KnownMigrations).Should().BeTrue();

        // Unknown migration IDs are rejected before MigrateAsync.
        SqlTestCatalogPolicy.ShouldBlockBeforeMigration(
            true, 1, true, true, false, ["209901010000_Unknown"], KnownMigrations).Should().BeTrue();

        // Empty history is not accepted as a trusted existing schema.
        SqlTestCatalogPolicy.ShouldBlockBeforeMigration(
            true, 1, true, true, false, [], KnownMigrations).Should().BeTrue();

        // If catalog inspection fails, the harness blocks before migration.
        SqlTestCatalogPolicy.ShouldBlockBeforeMigration(
            false, 0, false, false, false, [], KnownMigrations).Should().BeTrue();

        // A recognized dbo history is allowed to continue to pending migrations.
        SqlTestCatalogPolicy.ShouldBlockBeforeMigration(
            true, 6, true, true, false, ["202610150001_InitialClinicSchema"], KnownMigrations).Should().BeFalse();
    }
}
