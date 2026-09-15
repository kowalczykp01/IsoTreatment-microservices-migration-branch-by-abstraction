using FluentAssertions;
using IsoTreatmentProcessSupportAPI.Models;
using static IsoTreatment.GatewayEquivalenceTests.TestData;

namespace IsoTreatment.GatewayEquivalenceTests;

// Every test runs once per IReminderGateway implementation with the same assertions. A test
// that passes for one implementation and fails for the other is a difference in behaviour.
[Collection(GatewayCollection.Name)]
public sealed class ReminderGatewayEquivalenceTests : IAsyncLifetime
{
    private readonly GatewayTestFixture _fixture;

    public ReminderGatewayEquivalenceTests(GatewayTestFixture fixture) => _fixture = fixture;

    public Task InitializeAsync() => _fixture.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Theory]
    [InlineData(GatewayImplementation.EntityFramework)]
    [InlineData(GatewayImplementation.TreatmentService)]
    public async Task GetAllForUser_ReturnsOnlyThatUsersReminders(string implementation)
    {
        var gateway = _fixture.CreateGateway(implementation, OwnerId);

        var reminders = await gateway.GetAllForUserAsync(OwnerId);

        reminders.Should().BeEquivalentTo(
            new[]
            {
                new ReminderDto { Id = OwnersMorningReminderId, Time = OwnersMorningTime },
                new ReminderDto { Id = OwnersEveningReminderId, Time = OwnersEveningTime },
            },
            options => options.WithoutStrictOrdering());
    }

    [Theory]
    [InlineData(GatewayImplementation.EntityFramework)]
    [InlineData(GatewayImplementation.TreatmentService)]
    public async Task GetAllForUser_ReturnsEmptyList_WhenUserHasNoReminders(string implementation)
    {
        var gateway = _fixture.CreateGateway(implementation, UserWithoutRemindersId);

        var reminders = await gateway.GetAllForUserAsync(UserWithoutRemindersId);

        reminders.Should().BeEmpty();
    }

    [Theory]
    [InlineData(GatewayImplementation.EntityFramework)]
    [InlineData(GatewayImplementation.TreatmentService)]
    public async Task GetByIdForUser_ReturnsReminder_WhenUserOwnsIt(string implementation)
    {
        var gateway = _fixture.CreateGateway(implementation, OwnerId);

        var reminder = await gateway.GetByIdForUserAsync(OwnersEveningReminderId, OwnerId);

        reminder.Should().BeEquivalentTo(new ReminderDto { Id = OwnersEveningReminderId, Time = OwnersEveningTime });
    }

    [Theory]
    [InlineData(GatewayImplementation.EntityFramework)]
    [InlineData(GatewayImplementation.TreatmentService)]
    public async Task GetByIdForUser_ReturnsNull_WhenReminderDoesNotExist(string implementation)
    {
        var gateway = _fixture.CreateGateway(implementation, OwnerId);

        var reminder = await gateway.GetByIdForUserAsync(MissingReminderId, OwnerId);

        reminder.Should().BeNull();
    }

    [Theory]
    [InlineData(GatewayImplementation.EntityFramework)]
    [InlineData(GatewayImplementation.TreatmentService)]
    public async Task GetByIdForUser_ReturnsNull_WhenReminderBelongsToAnotherUser(string implementation)
    {
        var gateway = _fixture.CreateGateway(implementation, OwnerId);

        var reminder = await gateway.GetByIdForUserAsync(OtherUsersReminderId, OwnerId);

        reminder.Should().BeNull();
    }

    [Theory]
    [InlineData(GatewayImplementation.EntityFramework)]
    [InlineData(GatewayImplementation.TreatmentService)]
    public async Task Add_ReturnsReminderWithNextId_AndStoresItForTheUser(string implementation)
    {
        var gateway = _fixture.CreateGateway(implementation, OwnerId);

        var added = await gateway.AddAsync(OwnerId, new TimeOnly(12, 45));

        added.Should().BeEquivalentTo(new ReminderDto { Id = NextReminderId, Time = new TimeOnly(12, 45) });
        (await _fixture.StoredRemindersAsync(implementation))
            .Should().ContainSingle(row => row.Id == NextReminderId)
            .Which.Should().Be((NextReminderId, OwnerId, new TimeSpan(12, 45, 0)));
    }

    [Theory]
    [InlineData(GatewayImplementation.EntityFramework)]
    [InlineData(GatewayImplementation.TreatmentService)]
    public async Task Add_StoresTimeWithFullPrecision(string implementation)
    {
        var gateway = _fixture.CreateGateway(implementation, OwnerId);
        var time = new TimeOnly(7, 5, 30);

        var added = await gateway.AddAsync(OwnerId, time);

        (await gateway.GetByIdForUserAsync(added.Id, OwnerId))!.Time.Should().Be(time);
    }

    [Theory]
    [InlineData(GatewayImplementation.EntityFramework)]
    [InlineData(GatewayImplementation.TreatmentService)]
    public async Task Update_ChangesTime_WhenUserOwnsTheReminder(string implementation)
    {
        var gateway = _fixture.CreateGateway(implementation, OwnerId);

        var updated = await gateway.UpdateAsync(OwnersMorningReminderId, OwnerId, new TimeOnly(9, 15));

        updated.Should().BeEquivalentTo(new ReminderDto { Id = OwnersMorningReminderId, Time = new TimeOnly(9, 15) });
        (await gateway.GetByIdForUserAsync(OwnersMorningReminderId, OwnerId))!.Time.Should().Be(new TimeOnly(9, 15));
    }

    [Theory]
    [InlineData(GatewayImplementation.EntityFramework)]
    [InlineData(GatewayImplementation.TreatmentService)]
    public async Task Update_StoresTimeWithFullPrecision(string implementation)
    {
        var gateway = _fixture.CreateGateway(implementation, OwnerId);
        var time = new TimeOnly(22, 59, 45);

        var updated = await gateway.UpdateAsync(OwnersEveningReminderId, OwnerId, time);

        updated!.Time.Should().Be(time);
        (await _fixture.StoredRemindersAsync(implementation))
            .Should().Contain((OwnersEveningReminderId, OwnerId, time.ToTimeSpan()));
    }

    [Theory]
    [InlineData(GatewayImplementation.EntityFramework)]
    [InlineData(GatewayImplementation.TreatmentService)]
    public async Task Update_ReturnsNull_WhenReminderDoesNotExist(string implementation)
    {
        var gateway = _fixture.CreateGateway(implementation, OwnerId);

        var updated = await gateway.UpdateAsync(MissingReminderId, OwnerId, new TimeOnly(9, 15));

        updated.Should().BeNull();
    }

    [Theory]
    [InlineData(GatewayImplementation.EntityFramework)]
    [InlineData(GatewayImplementation.TreatmentService)]
    public async Task Update_ReturnsNullAndLeavesReminderUnchanged_WhenReminderBelongsToAnotherUser(string implementation)
    {
        var gateway = _fixture.CreateGateway(implementation, OwnerId);

        var updated = await gateway.UpdateAsync(OtherUsersReminderId, OwnerId, new TimeOnly(9, 15));

        updated.Should().BeNull();
        (await _fixture.StoredRemindersAsync(implementation))
            .Should().Contain((OtherUsersReminderId, OtherUserId, OtherUsersTime.ToTimeSpan()));
    }

    [Theory]
    [InlineData(GatewayImplementation.EntityFramework)]
    [InlineData(GatewayImplementation.TreatmentService)]
    public async Task Delete_ReturnsTrueAndRemovesReminder_WhenUserOwnsIt(string implementation)
    {
        var gateway = _fixture.CreateGateway(implementation, OwnerId);

        var deleted = await gateway.DeleteAsync(OwnersMorningReminderId, OwnerId);

        deleted.Should().BeTrue();
        (await gateway.GetByIdForUserAsync(OwnersMorningReminderId, OwnerId)).Should().BeNull();
    }

    [Theory]
    [InlineData(GatewayImplementation.EntityFramework)]
    [InlineData(GatewayImplementation.TreatmentService)]
    public async Task Delete_ReturnsFalse_WhenReminderDoesNotExist(string implementation)
    {
        var gateway = _fixture.CreateGateway(implementation, OwnerId);

        var deleted = await gateway.DeleteAsync(MissingReminderId, OwnerId);

        deleted.Should().BeFalse();
    }

    [Theory]
    [InlineData(GatewayImplementation.EntityFramework)]
    [InlineData(GatewayImplementation.TreatmentService)]
    public async Task Delete_ReturnsFalseAndKeepsReminder_WhenReminderBelongsToAnotherUser(string implementation)
    {
        var gateway = _fixture.CreateGateway(implementation, OwnerId);

        var deleted = await gateway.DeleteAsync(OtherUsersReminderId, OwnerId);

        deleted.Should().BeFalse();
        (await _fixture.StoredRemindersAsync(implementation))
            .Should().Contain(row => row.Id == OtherUsersReminderId);
    }
}
