using IsoTreatmentProcessSupportAPI.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace IsoTreatmentProcessSupportAPI.CharacterizationTests;

public sealed class TestDatabase
{
    public const string RequiredDatabaseNameSuffix = "_CharacterizationTests";

    private readonly DbContextOptions<IsoSupportDbContext> _options;

    public TestDatabase(string connectionString)
    {
        EnsureDatabaseIsDisposable(connectionString);

        _options = new DbContextOptionsBuilder<IsoSupportDbContext>()
            .UseSqlServer(connectionString)
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
        await using var dbContext = new IsoSupportDbContext(_options);
        await dbContext.Database.ExecuteSqlRawAsync(
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
        await using var dbContext = new IsoSupportDbContext(_options);
        var reminder = new Reminder { UserId = userId, Time = time };
        dbContext.Reminders.Add(reminder);
        await dbContext.SaveChangesAsync();
        return reminder.Id;
    }

    public async Task<List<Reminder>> GetRemindersAsync()
    {
        await using var dbContext = new IsoSupportDbContext(_options);
        return await dbContext.Reminders.AsNoTracking().OrderBy(r => r.Id).ToListAsync();
    }
}
