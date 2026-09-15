using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AutoMapper;
using IsoTreatmentProcessSupportAPI;
using IsoTreatmentProcessSupportAPI.Entities;
using IsoTreatmentProcessSupportAPI.Gateways;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Testcontainers.MsSql;
using TreatmentService.Infrastructure.Persistence;
using TreatmentServiceEntryPoint = TreatmentService.Api.Controllers.ReminderController;

namespace IsoTreatment.GatewayEquivalenceTests;

public sealed class GatewayTestFixture : IAsyncLifetime
{
    private const string Issuer = "isotreatment-users-issuer";
    private const string Audience = "isotreatment-users-audience";
    private const string SigningKey = "Kj9pL2mQ8rT5vW3nY6bC4dF1gH0jA9eZ2xU7yVqW3eR5tY6uI8oP9aS0dF==";

    private readonly MsSqlContainer _container = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    private readonly List<IDisposable> _disposables = new();

    private string _monolithConnectionString = string.Empty;
    private string _treatmentConnectionString = string.Empty;
    private WebApplicationFactory<TreatmentServiceEntryPoint> _treatmentService = null!;
    private IMapper _mapper = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        // One throwaway server, two databases. In the running system they live on separate
        // instances; for the gateways the difference is invisible.
        _monolithConnectionString = WithDatabase("IsoTreatmentProcessSupport_EquivalenceTests");
        _treatmentConnectionString = WithDatabase("Treatment_EquivalenceTests");

        // The Treatment service reads these before WebApplicationFactory can override its
        // configuration, so they go in as environment variables.
        Environment.SetEnvironmentVariable("ConnectionStrings__TreatmentDb", _treatmentConnectionString);
        Environment.SetEnvironmentVariable("Authentication__Issuer", Issuer);
        Environment.SetEnvironmentVariable("Authentication__Audience", Audience);
        Environment.SetEnvironmentVariable("Authentication__SigningKey", SigningKey);
        Environment.SetEnvironmentVariable("OTEL_SDK_DISABLED", "true");

        await using (var monolithDb = CreateMonolithDbContext())
        {
            await monolithDb.Database.MigrateAsync();
        }

        _treatmentService = new WebApplicationFactory<TreatmentServiceEntryPoint>();

        await using (var scope = _treatmentService.Services.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<TreatmentDbContext>().Database.MigrateAsync();
        }

        _mapper = new MapperConfiguration(config => config.AddProfile<MappingProfile>()).CreateMapper();
    }

    public async Task DisposeAsync()
    {
        _disposables.ForEach(disposable => disposable.Dispose());
        await _treatmentService.DisposeAsync();
        await _container.DisposeAsync();
    }

    // A gateway acting on behalf of one user. The Treatment service learns who the user is
    // from the forwarded token only, so its gateway is bound to a token for that user.
    public IReminderGateway CreateGateway(string implementation, int userId)
    {
        switch (implementation)
        {
            case GatewayImplementation.EntityFramework:
                var dbContext = CreateMonolithDbContext();
                _disposables.Add(dbContext);
                return new EfReminderGateway(dbContext, _mapper);

            case GatewayImplementation.TreatmentService:
                var httpClient = _treatmentService.CreateClient();
                _disposables.Add(httpClient);
                return new TreatmentServiceReminderGateway(
                    httpClient, new ForwardedTokenHttpContextAccessor(CreateToken(userId)));

            default:
                throw new ArgumentOutOfRangeException(nameof(implementation), implementation, null);
        }
    }

    public async Task ResetAsync()
    {
        await ExecuteAsync(_monolithConnectionString,
            $"""
            DELETE FROM [Entries];
            DELETE FROM [Reminders];
            DELETE FROM [Users];

            SET IDENTITY_INSERT [Users] ON;
            INSERT INTO [Users]
                ([Id], [FirstName], [LastName], [Email], [PasswordHash], [EmailConfirmed],
                 [Weight], [ClimaxDoseInMiligramsPerKilogramOfBodyWeight], [DailyDose], [MedicationStartDate])
            VALUES
                ({TestData.OwnerId}, 'Jan', 'Kowalski', 'owner@example.com', 'x', 1, 70, 120, 40, '2024-01-01'),
                ({TestData.OtherUserId}, 'Anna', 'Nowak', 'other@example.com', 'x', 1, 58, 120, 30, '2024-01-01'),
                ({TestData.UserWithoutRemindersId}, 'Piotr', 'Wiśniewski', 'empty@example.com', 'x', 1, 82, 150, 40, '2024-01-01');
            SET IDENTITY_INSERT [Users] OFF;
            {RemindersSeedSql}
            """);

        await ExecuteAsync(_treatmentConnectionString,
            $"""
            DELETE FROM [Reminders];
            {RemindersSeedSql}
            """);
    }

    public async Task<IReadOnlyList<(int Id, int UserId, TimeSpan Time)>> StoredRemindersAsync(string implementation)
    {
        var connectionString = implementation == GatewayImplementation.EntityFramework
            ? _monolithConnectionString
            : _treatmentConnectionString;

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand("SELECT [Id], [UserId], [Time] FROM [Reminders] ORDER BY [Id]", connection);
        await using var reader = await command.ExecuteReaderAsync();

        var rows = new List<(int, int, TimeSpan)>();
        while (await reader.ReadAsync())
        {
            rows.Add((reader.GetInt32(0), reader.GetInt32(1), reader.GetTimeSpan(2)));
        }

        return rows;
    }

    private static string RemindersSeedSql =>
        $"""
        SET IDENTITY_INSERT [Reminders] ON;
        INSERT INTO [Reminders] ([Id], [UserId], [Time])
        VALUES
            ({TestData.OwnersMorningReminderId}, {TestData.OwnerId}, '{TestData.OwnersMorningTime:HH\:mm}'),
            ({TestData.OwnersEveningReminderId}, {TestData.OwnerId}, '{TestData.OwnersEveningTime:HH\:mm}'),
            ({TestData.OtherUsersReminderId}, {TestData.OtherUserId}, '{TestData.OtherUsersTime:HH\:mm}');
        SET IDENTITY_INSERT [Reminders] OFF;
        DBCC CHECKIDENT ('[Reminders]', RESEED, {TestData.IdentitySeed}) WITH NO_INFOMSGS;
        """;

    private IsoSupportDbContext CreateMonolithDbContext() =>
        new(new DbContextOptionsBuilder<IsoSupportDbContext>().UseSqlServer(_monolithConnectionString).Options);

    private string WithDatabase(string databaseName) =>
        new SqlConnectionStringBuilder(_container.GetConnectionString()) { InitialCatalog = databaseName }.ConnectionString;

    private static async Task ExecuteAsync(string connectionString, string sql)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private static string CreateToken(int userId)
    {
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey)), SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) },
            expires: DateTime.Now.AddHours(1),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

[CollectionDefinition(Name)]
public sealed class GatewayCollection : ICollectionFixture<GatewayTestFixture>
{
    public const string Name = "Gateways";
}
