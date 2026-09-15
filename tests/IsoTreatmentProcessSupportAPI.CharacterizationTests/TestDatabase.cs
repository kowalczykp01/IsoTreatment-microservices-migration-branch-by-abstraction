using IsoTreatmentProcessSupportAPI.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace IsoTreatmentProcessSupportAPI.CharacterizationTests;

public sealed record StoredReminder(int Id, int UserId, TimeOnly Time);

public sealed class TestDatabase
{
    public const string RequiredDatabaseNameSuffix = "_CharacterizationTests";

    private readonly DbContextOptions<IsoSupportDbContext> _options;
    private readonly string _monolithConnectionString;
    private readonly string _remindersConnectionString;

    // Users always live in the monolith's database. Reminders live in whichever database owns
    // them for the run: the monolith's with the flag off, the Treatment service's with it on.
    // Both Reminders tables have the same columns, so reminders are handled in plain SQL.
    public TestDatabase(string monolithConnectionString, string? remindersConnectionString = null)
    {
        _monolithConnectionString = monolithConnectionString;
        _remindersConnectionString = remindersConnectionString ?? monolithConnectionString;

        EnsureDatabaseIsDisposable(_monolithConnectionString);
        EnsureDatabaseIsDisposable(_remindersConnectionString);

        _options = new DbContextOptionsBuilder<IsoSupportDbContext>()
            .UseSqlServer(monolithConnectionString)
            .Options;
    }

    private static void EnsureDatabaseIsDisposable(string connectionString)
    {
        var databaseName = new SqlConnectionStringBuilder(connectionString).InitialCatalog;

        if (!databaseName.EndsWith(RequiredDatabaseNameSuffix, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"TestDatabase deletes every row in Users, Entries and Reminders, so it refuses "
                + $"to run against '{databaseName}'. Point it at a throwaway database whose name "
                + $"ends with '{RequiredDatabaseNameSuffix}'.");
        }
    }

    public async Task ResetAsync()
    {
        if (_remindersConnectionString != _monolithConnectionString)
        {
            await ExecuteAsync(_remindersConnectionString,
                """
                DELETE FROM [Reminders];
                DBCC CHECKIDENT ('[Reminders]', RESEED, 0);
                """);
        }

        await ExecuteAsync(_monolithConnectionString,
            """
            DELETE FROM [Entries];
            DELETE FROM [Reminders];
            DELETE FROM [Users];
            DBCC CHECKIDENT ('[Entries]', RESEED, 0);
            DBCC CHECKIDENT ('[Reminders]', RESEED, 0);
            DBCC CHECKIDENT ('[Users]', RESEED, 0);
            """);
    }

    public async Task<int> SeedUserAsync(string email = "patient@example.com")
    {
        await using var dbContext = new IsoSupportDbContext(_options);
        var user = new User
        {
            FirstName = "Jan",
            LastName = "Kowalski",
            Email = email,
            PasswordHash = "irrelevant-for-reminder-tests",
            EmailConfirmed = true,
            Weight = 70,
            ClimaxDoseInMiligramsPerKilogramOfBodyWeight = 120,
            DailyDose = 40,
            MedicationStartDate = new DateTime(2024, 1, 1),
        };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        return user.Id;
    }

    public async Task<int> SeedReminderAsync(int userId, TimeOnly time)
    {
        await using var connection = new SqlConnection(_remindersConnectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(
            "INSERT INTO [Reminders] ([UserId], [Time]) OUTPUT INSERTED.[Id] VALUES (@userId, @time)", connection);
        command.Parameters.AddWithValue("@userId", userId);
        command.Parameters.AddWithValue("@time", time.ToTimeSpan());
        return (int)(await command.ExecuteScalarAsync())!;
    }

    public async Task<List<StoredReminder>> GetRemindersAsync()
    {
        await using var connection = new SqlConnection(_remindersConnectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(
            "SELECT [Id], [UserId], [Time] FROM [Reminders] ORDER BY [Id]", connection);
        await using var reader = await command.ExecuteReaderAsync();

        var reminders = new List<StoredReminder>();
        while (await reader.ReadAsync())
        {
            reminders.Add(new StoredReminder(
                reader.GetInt32(0), reader.GetInt32(1), TimeOnly.FromTimeSpan(reader.GetTimeSpan(2))));
        }

        return reminders;
    }

    private static async Task ExecuteAsync(string connectionString, string sql)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }
}
