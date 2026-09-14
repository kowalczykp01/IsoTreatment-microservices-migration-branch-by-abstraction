using FluentAssertions;

namespace IsoTreatmentProcessSupportAPI.CharacterizationTests;

public sealed class TestDatabaseGuardTests
{
    [Theory]
    [InlineData("Server=localhost;Database=IsoTreatmentProcessSupport;User Id=sa;Password=x;")]
    [InlineData("Server=127.0.0.1,61111;Database=master;User Id=sa;Password=x;")]
    [InlineData("Server=prod.example.com;Database=IsoTreatmentProcessSupport_Production;User Id=sa;Password=x;")]
    public void Constructor_Throws_WhenDatabaseIsNotAThrowawayTestDatabase(string connectionString)
    {
        var act = () => new TestDatabase(connectionString);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{TestDatabase.RequiredDatabaseNameSuffix}*");
    }

    [Fact]
    public void Constructor_Succeeds_WhenDatabaseNameCarriesTheRequiredSuffix()
    {
        var connectionString =
            "Server=127.0.0.1,61111;"
            + $"Database=IsoTreatmentProcessSupport{TestDatabase.RequiredDatabaseNameSuffix};"
            + "User Id=sa;Password=x;";

        var act = () => new TestDatabase(connectionString);

        act.Should().NotThrow();
    }
}
