using System.Net;
using System.Text;
using FluentAssertions;

namespace IsoTreatmentProcessSupportAPI.CharacterizationTests;

[Collection(SqlServerCollection.Name)]
public sealed class ReminderApiCharacterizationTests : IAsyncLifetime
{
    private readonly TestDatabase _database;
    private readonly MonolithApplicationFactory _factory;

    public ReminderApiCharacterizationTests(SqlServerFixture fixture)
    {
        _database = new TestDatabase(fixture.ConnectionString);
        _factory = new MonolithApplicationFactory();
    }

    public Task InitializeAsync() => _database.ResetAsync();

    public Task DisposeAsync()
    {
        _factory.Dispose();
        return Task.CompletedTask;
    }

    private static StringContent JsonBody(string json) =>
        new(json, Encoding.UTF8, "application/json");

    private HttpClient ClientFor(int userId) =>
        _factory.CreateClientWithTokenCookie(MonolithApplicationFactory.CreateToken(userId));

    [Fact]
    public async Task GetAll_ReturnsRemindersWithTimeFormattedAsHoursAndMinutes()
    {
        var userId = await _database.SeedUserAsync();
        var firstId = await _database.SeedReminderAsync(userId, new TimeOnly(8, 0));
        var secondId = await _database.SeedReminderAsync(userId, new TimeOnly(20, 30));

        var response = await ClientFor(userId).GetAsync("/api/reminder");
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.Should().Be(
            $$"""[{"id":{{firstId}},"time":"08:00"},{"id":{{secondId}},"time":"20:30"}]""");
    }

    [Fact]
    public async Task GetAll_ReturnsEmptyArray_WhenUserHasNoReminders()
    {
        var userId = await _database.SeedUserAsync();

        var response = await ClientFor(userId).GetAsync("/api/reminder");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().Be("[]");
    }

    [Fact]
    public async Task GetAll_ReturnsNotFoundWithUserNotFoundMessage_WhenUserRowIsMissing()
    {
        var response = await ClientFor(999).GetAsync("/api/reminder");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await response.Content.ReadAsStringAsync()).Should().Be("User not found");
    }

    [Fact]
    public async Task GetAll_DoesNotReturnRemindersOwnedByAnotherUser()
    {
        var ownerId = await _database.SeedUserAsync("owner@example.com");
        var otherId = await _database.SeedUserAsync("other@example.com");
        await _database.SeedReminderAsync(ownerId, new TimeOnly(8, 0));

        var response = await ClientFor(otherId).GetAsync("/api/reminder");

        (await response.Content.ReadAsStringAsync()).Should().Be("[]");
    }

    [Fact]
    public async Task GetById_ReturnsSingleReminder()
    {
        var userId = await _database.SeedUserAsync();
        var reminderId = await _database.SeedReminderAsync(userId, new TimeOnly(8, 0));

        var response = await ClientFor(userId).GetAsync($"/api/reminder/{reminderId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync())
            .Should().Be($$"""{"id":{{reminderId}},"time":"08:00"}""");
    }

    [Fact]
    public async Task GetById_ReturnsNotFoundWithReminderNotFoundMessage_WhenReminderDoesNotExist()
    {
        var userId = await _database.SeedUserAsync();

        var response = await ClientFor(userId).GetAsync("/api/reminder/12345");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await response.Content.ReadAsStringAsync()).Should().Be("Reminder not found");
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenReminderIsOwnedByAnotherUser()
    {
        var ownerId = await _database.SeedUserAsync("owner@example.com");
        var otherId = await _database.SeedUserAsync("other@example.com");
        var reminderId = await _database.SeedReminderAsync(ownerId, new TimeOnly(8, 0));

        var response = await ClientFor(otherId).GetAsync($"/api/reminder/{reminderId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await response.Content.ReadAsStringAsync()).Should().Be("Reminder not found");
    }

    [Fact]
    public async Task Add_ReturnsOkNotCreated_AndPersistsReminder()
    {
        var userId = await _database.SeedUserAsync();

        var response = await ClientFor(userId).PostAsync("/api/reminder", JsonBody("""{"time":"08:00"}"""));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Location.Should().BeNull();

        var stored = await _database.GetRemindersAsync();
        stored.Should().ContainSingle();
        stored[0].Time.Should().Be(new TimeOnly(8, 0));
        stored[0].UserId.Should().Be(userId);

        (await response.Content.ReadAsStringAsync())
            .Should().Be($$"""{"id":{{stored[0].Id}},"time":"08:00"}""");
    }

    [Fact]
    public async Task Add_ReturnsNotFound_WhenUserRowIsMissing()
    {
        var response = await ClientFor(999).PostAsync("/api/reminder", JsonBody("""{"time":"08:00"}"""));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await response.Content.ReadAsStringAsync()).Should().Be("User not found");
    }

    [Fact]
    public async Task Update_ChangesTimeAndReturnsOk()
    {
        var userId = await _database.SeedUserAsync();
        var reminderId = await _database.SeedReminderAsync(userId, new TimeOnly(8, 0));

        var response = await ClientFor(userId)
            .PutAsync($"/api/reminder/{reminderId}", JsonBody("""{"time":"21:15"}"""));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync())
            .Should().Be($$"""{"id":{{reminderId}},"time":"21:15"}""");

        (await _database.GetRemindersAsync())[0].Time.Should().Be(new TimeOnly(21, 15));
    }

    [Fact]
    public async Task Update_ReturnsNotFoundAndLeavesDataUnchanged_WhenReminderIsOwnedByAnotherUser()
    {
        var ownerId = await _database.SeedUserAsync("owner@example.com");
        var otherId = await _database.SeedUserAsync("other@example.com");
        var reminderId = await _database.SeedReminderAsync(ownerId, new TimeOnly(8, 0));

        var response = await ClientFor(otherId)
            .PutAsync($"/api/reminder/{reminderId}", JsonBody("""{"time":"21:15"}"""));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await _database.GetRemindersAsync())[0].Time.Should().Be(new TimeOnly(8, 0));
    }

    [Fact]
    public async Task Delete_ReturnsNoContentAndRemovesRecord()
    {
        var userId = await _database.SeedUserAsync();
        var reminderId = await _database.SeedReminderAsync(userId, new TimeOnly(8, 0));

        var response = await ClientFor(userId).DeleteAsync($"/api/reminder/{reminderId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await response.Content.ReadAsStringAsync()).Should().BeEmpty();
        (await _database.GetRemindersAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task Delete_ReturnsNotFound_WhenReminderDoesNotExist()
    {
        var userId = await _database.SeedUserAsync();

        var response = await ClientFor(userId).DeleteAsync("/api/reminder/12345");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await response.Content.ReadAsStringAsync()).Should().Be("Reminder not found");
    }

    [Theory]
    [InlineData("GET", "/api/reminder")]
    [InlineData("GET", "/api/reminder/1")]
    [InlineData("POST", "/api/reminder")]
    [InlineData("PUT", "/api/reminder/1")]
    [InlineData("DELETE", "/api/reminder/1")]
    public async Task Endpoints_ReturnUnauthorized_WhenTokenCookieIsMissing(string method, string path)
    {
        var request = new HttpRequestMessage(new HttpMethod(method), path);
        if (method is "POST" or "PUT")
        {
            request.Content = JsonBody("""{"time":"08:00"}""");
        }

        var response = await _factory.CreateClient().SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAll_ReturnsUnauthorized_WhenTokenIsSignedWithDifferentKey()
    {
        var userId = await _database.SeedUserAsync();
        var foreignToken = MonolithApplicationFactory.CreateToken(
            userId, signingKey: "ACompletelyDifferentHmacKey-0123456789-0123456789==");

        var response = await _factory.CreateClientWithTokenCookie(foreignToken).GetAsync("/api/reminder");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAll_ReturnsUnauthorized_WhenTokenHasDifferentIssuer()
    {
        var userId = await _database.SeedUserAsync();
        var foreignToken = MonolithApplicationFactory.CreateToken(userId, issuer: "different-issuer");

        var response = await _factory.CreateClientWithTokenCookie(foreignToken).GetAsync("/api/reminder");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SuccessResponse_HasJsonContentType()
    {
        var userId = await _database.SeedUserAsync();

        var response = await ClientFor(userId).GetAsync("/api/reminder");

        response.Content.Headers.ContentType!.ToString().Should().Be("application/json; charset=utf-8");
    }

    [Fact]
    public async Task NotFoundResponse_HasNoContentTypeHeader()
    {
        var response = await ClientFor(999).GetAsync("/api/reminder");

        response.Content.Headers.ContentType.Should().BeNull();
        (await response.Content.ReadAsStringAsync()).Should().Be("User not found");
    }
}
