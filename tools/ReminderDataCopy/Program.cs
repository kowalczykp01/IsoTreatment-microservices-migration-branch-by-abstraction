using System.Data;
using Microsoft.Data.SqlClient;

// Copies the Reminders table from the monolith's database into the Treatment service's
// database, preserving ids and the identity seed, and verifies the result before committing.
// Run before the flag is turned on (Phase 5) and again immediately before the switch (Phase 8).

var replace = args.Contains("--replace");

var sourceConnectionString = ReadConnectionString("IsoSupportDb");
var targetConnectionString = ReadConnectionString("TreatmentDb");

var snapshot = await ReadSourceAsync(sourceConnectionString);

Console.WriteLine($"Source    {Describe(snapshot.Rows)}, identity {snapshot.IdentityCurrent}");

await using var target = new SqlConnection(targetConnectionString);
await target.OpenAsync();
await using var transaction = (SqlTransaction)await target.BeginTransactionAsync(IsolationLevel.Serializable);

var existingRows = await ScalarAsync<int>(target, transaction, "SELECT COUNT(*) FROM [Reminders]");

if (existingRows > 0 && !replace)
{
    Console.Error.WriteLine(
        $"The target already holds {existingRows} reminders. Run with --replace to overwrite them. "
        + "Never do that after the switch: from then on the target is the only copy of the data.");
    return 2;
}

await ExecuteAsync(target, transaction, "DELETE FROM [Reminders]");

using (var bulkCopy = new SqlBulkCopy(target, SqlBulkCopyOptions.KeepIdentity, transaction))
{
    bulkCopy.DestinationTableName = "[Reminders]";
    bulkCopy.ColumnMappings.Add(nameof(ReminderRow.Id), "Id");
    bulkCopy.ColumnMappings.Add(nameof(ReminderRow.UserId), "UserId");
    bulkCopy.ColumnMappings.Add(nameof(ReminderRow.Time), "Time");
    await bulkCopy.WriteToServerAsync(ToDataTable(snapshot.Rows));
}

// Continue from the monolith's identity, not from MAX(Id): an id the monolith ever handed out,
// even for a reminder deleted since, must never be issued again by the Treatment service.
await ExecuteAsync(target, transaction, $"DBCC CHECKIDENT ('[Reminders]', RESEED, {snapshot.IdentityCurrent}) WITH NO_INFOMSGS");

var copiedRows = await ReadRowsAsync(target, transaction);
var targetIdentity = await ScalarAsync<decimal>(target, transaction, "SELECT IDENT_CURRENT('Reminders')");

Console.WriteLine($"Target    {Describe(copiedRows)}, identity {targetIdentity}");

var problems = new List<string>();

if (copiedRows.Count != snapshot.Rows.Count)
{
    problems.Add($"row count differs: source {snapshot.Rows.Count}, target {copiedRows.Count}");
}

if (!copiedRows.SequenceEqual(snapshot.Rows))
{
    problems.Add("row contents differ");
}

if (targetIdentity != snapshot.IdentityCurrent)
{
    problems.Add($"identity differs: source {snapshot.IdentityCurrent}, target {targetIdentity}");
}

if (problems.Count > 0)
{
    await transaction.RollbackAsync();
    Console.Error.WriteLine("Verification failed, nothing was written: " + string.Join("; ", problems));
    return 1;
}

await transaction.CommitAsync();

var sourceRowsNow = await CountSourceAsync(sourceConnectionString);
if (sourceRowsNow != snapshot.Rows.Count)
{
    Console.WriteLine(
        $"Warning: the source changed while copying ({snapshot.Rows.Count} -> {sourceRowsNow} rows). "
        + "The copy matches the snapshot it was taken from; run it again to catch up.");
}

Console.WriteLine($"Copied and verified {copiedRows.Count} reminders.");
return 0;

static string ReadConnectionString(string name) =>
    Environment.GetEnvironmentVariable($"ConnectionStrings__{name}")
    ?? throw new InvalidOperationException(
        $"Missing connection string (environment variable: ConnectionStrings__{name}).");

static async Task<(List<ReminderRow> Rows, decimal IdentityCurrent)> ReadSourceAsync(string connectionString)
{
    await using var connection = new SqlConnection(connectionString);
    await connection.OpenAsync();

    // Serializable keeps writers out for the duration of the read, so the rows and the
    // identity value describe the same moment.
    await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(IsolationLevel.Serializable);

    var rows = await ReadRowsAsync(connection, transaction);
    var identity = await ScalarAsync<decimal>(connection, transaction, "SELECT IDENT_CURRENT('Reminders')");

    await transaction.CommitAsync();
    return (rows, identity);
}

static async Task<int> CountSourceAsync(string connectionString)
{
    await using var connection = new SqlConnection(connectionString);
    await connection.OpenAsync();
    return await ScalarAsync<int>(connection, null, "SELECT COUNT(*) FROM [Reminders]");
}

static async Task<List<ReminderRow>> ReadRowsAsync(SqlConnection connection, SqlTransaction? transaction)
{
    await using var command = new SqlCommand(
        "SELECT [Id], [UserId], [Time] FROM [Reminders] ORDER BY [Id]", connection, transaction);
    await using var reader = await command.ExecuteReaderAsync();

    var rows = new List<ReminderRow>();
    while (await reader.ReadAsync())
    {
        rows.Add(new ReminderRow(reader.GetInt32(0), reader.GetInt32(1), reader.GetTimeSpan(2)));
    }

    return rows;
}

static async Task<T> ScalarAsync<T>(SqlConnection connection, SqlTransaction? transaction, string sql)
{
    await using var command = new SqlCommand(sql, connection, transaction);
    return (T)Convert.ChangeType((await command.ExecuteScalarAsync())!, typeof(T));
}

static async Task ExecuteAsync(SqlConnection connection, SqlTransaction transaction, string sql)
{
    await using var command = new SqlCommand(sql, connection, transaction);
    await command.ExecuteNonQueryAsync();
}

static DataTable ToDataTable(IEnumerable<ReminderRow> rows)
{
    var table = new DataTable();
    table.Columns.Add(nameof(ReminderRow.Id), typeof(int));
    table.Columns.Add(nameof(ReminderRow.UserId), typeof(int));
    table.Columns.Add(nameof(ReminderRow.Time), typeof(TimeSpan));

    foreach (var row in rows)
    {
        table.Rows.Add(row.Id, row.UserId, row.Time);
    }

    return table;
}

static string Describe(IReadOnlyCollection<ReminderRow> rows) =>
    rows.Count == 0
        ? "0 reminders"
        : $"{rows.Count} reminders, ids {rows.Min(r => r.Id)}..{rows.Max(r => r.Id)}, "
          + $"{rows.Select(r => r.UserId).Distinct().Count()} users";

internal sealed record ReminderRow(int Id, int UserId, TimeSpan Time);
