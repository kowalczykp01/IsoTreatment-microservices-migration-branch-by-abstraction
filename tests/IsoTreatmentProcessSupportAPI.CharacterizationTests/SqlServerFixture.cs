using IsoTreatmentProcessSupportAPI.Entities;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;
using TreatmentService.Infrastructure.Persistence;
using TreatmentServiceEntryPoint = TreatmentService.Api.Controllers.ReminderController;

namespace IsoTreatmentProcessSupportAPI.CharacterizationTests;

public sealed class SqlServerFixture : IAsyncLifetime
{
    private const string TestDatabaseName =
        "IsoTreatmentProcessSupport" + TestDatabase.RequiredDatabaseNameSuffix;

    private const string TreatmentTestDatabaseName =
        "Treatment" + TestDatabase.RequiredDatabaseNameSuffix;

    private readonly MsSqlContainer _container = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    public string ConnectionString { get; private set; } = string.Empty;

    public string TreatmentConnectionString { get; private set; } = string.Empty;

    // An in-memory instance of the real Treatment service, for runs with the flag on.
    public WebApplicationFactory<TreatmentServiceEntryPoint> TreatmentService { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        ConnectionString = WithDatabase(TestDatabaseName);
        TreatmentConnectionString = WithDatabase(TreatmentTestDatabaseName);

        Environment.SetEnvironmentVariable("ConnectionStrings__IsoSupportDb", ConnectionString);
        Environment.SetEnvironmentVariable("ConnectionStrings__TreatmentDb", TreatmentConnectionString);
        Environment.SetEnvironmentVariable("Authentication__Issuer", MonolithApplicationFactory.Issuer);
        Environment.SetEnvironmentVariable("Authentication__Audience", MonolithApplicationFactory.Audience);
        Environment.SetEnvironmentVariable("Authentication__SigningKey", MonolithApplicationFactory.SigningKey);
        Environment.SetEnvironmentVariable("Authentication__Expiry", "00.01:00:00");
        Environment.SetEnvironmentVariable("OTEL_SDK_DISABLED", "true");

        var options = new DbContextOptionsBuilder<IsoSupportDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        await using (var dbContext = new IsoSupportDbContext(options))
        {
            await dbContext.Database.MigrateAsync();
        }

        var treatmentOptions = new DbContextOptionsBuilder<TreatmentDbContext>()
            .UseSqlServer(TreatmentConnectionString)
            .Options;

        await using (var treatmentDbContext = new TreatmentDbContext(treatmentOptions))
        {
            await treatmentDbContext.Database.MigrateAsync();
        }

        TreatmentService = new WebApplicationFactory<TreatmentServiceEntryPoint>();
    }

    public async Task DisposeAsync()
    {
        await TreatmentService.DisposeAsync();
        await _container.DisposeAsync();
    }

    private string WithDatabase(string databaseName) =>
        new SqlConnectionStringBuilder(_container.GetConnectionString()) { InitialCatalog = databaseName }.ConnectionString;
}

[CollectionDefinition(Name)]
public sealed class SqlServerCollection : ICollectionFixture<SqlServerFixture>
{
    public const string Name = "SqlServer";
}
