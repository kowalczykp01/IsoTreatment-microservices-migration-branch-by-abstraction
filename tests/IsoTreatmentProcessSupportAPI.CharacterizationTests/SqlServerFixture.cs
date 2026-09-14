using IsoTreatmentProcessSupportAPI.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;

namespace IsoTreatmentProcessSupportAPI.CharacterizationTests;

public sealed class SqlServerFixture : IAsyncLifetime
{
    private const string TestDatabaseName =
        "IsoTreatmentProcessSupport" + TestDatabase.RequiredDatabaseNameSuffix;

    private readonly MsSqlContainer _container = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        ConnectionString = new SqlConnectionStringBuilder(_container.GetConnectionString())
        {
            InitialCatalog = TestDatabaseName,
        }.ConnectionString;

        Environment.SetEnvironmentVariable("ConnectionStrings__IsoSupportDb", ConnectionString);
        Environment.SetEnvironmentVariable("Authentication__Issuer", MonolithApplicationFactory.Issuer);
        Environment.SetEnvironmentVariable("Authentication__Audience", MonolithApplicationFactory.Audience);
        Environment.SetEnvironmentVariable("Authentication__SigningKey", MonolithApplicationFactory.SigningKey);
        Environment.SetEnvironmentVariable("Authentication__Expiry", "00.01:00:00");

        var options = new DbContextOptionsBuilder<IsoSupportDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        await using var dbContext = new IsoSupportDbContext(options);
        await dbContext.Database.MigrateAsync();
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}

[CollectionDefinition(Name)]
public sealed class SqlServerCollection : ICollectionFixture<SqlServerFixture>
{
    public const string Name = "SqlServer";
}
