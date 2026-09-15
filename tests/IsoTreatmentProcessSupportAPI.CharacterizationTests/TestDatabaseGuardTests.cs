using FluentAssertions;

namespace IsoTreatmentProcessSupportAPI.CharacterizationTests;

public sealed class TestDatabaseGuardTests
{
    private const string ThrowawayConnectionString =
        "Server=127.0.0.1,61111;"
        + $"Database=IsoTreatmentProcessSupport{TestDatabase.RequiredDatabaseNameSuffix};"
        + "User Id=sa;Password=x;";

    [Theory]
    [InlineData("Server=localhost;Database=IsoTreatmentProcessSupport;User Id=sa;Password=x;")]
    [InlineData("Server=127.0.0.1,61111;Database=master;User Id=sa;Password=x;")]
    [InlineData("Server=prod.example.com;Database=IsoTreatmentProcessSupport_Production;User Id=sa;Password=x;")]
    public void Constructor_Throws_WhenDatabaseIsNotAThrowawayTestDatabase(string connectionString)
    {
        var monolith = () => new TestDatabase(connectionString, ThrowawayConnectionString);
        var reminders = () => new TestDatabase(ThrowawayConnectionString, connectionString);

        monolith.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{TestDatabase.RequiredDatabaseNameSuffix}*");
        reminders.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{TestDatabase.RequiredDatabaseNameSuffix}*");
    }

    [Fact]
    public void Constructor_Succeeds_WhenDatabaseNameCarriesTheRequiredSuffix()
    {
        var act = () => new TestDatabase(ThrowawayConnectionString, ThrowawayConnectionString);

        act.Should().NotThrow();
    }
}
