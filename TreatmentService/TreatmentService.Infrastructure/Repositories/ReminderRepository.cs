using Microsoft.EntityFrameworkCore;
using TreatmentService.Domain.Entities;
using TreatmentService.Domain.Repositories;
using TreatmentService.Infrastructure.Persistence;

namespace TreatmentService.Infrastructure.Repositories;

internal sealed class ReminderRepository : IReminderRepository
{
    private readonly TreatmentDbContext _dbContext;

    public ReminderRepository(TreatmentDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Reminder>> GetAllForUserAsync(
        int userId,
        CancellationToken cancellationToken = default) =>
        await _dbContext.Reminders
            .Where(reminder => reminder.UserId == userId)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Reminder reminder, CancellationToken cancellationToken = default) =>
        await _dbContext.Reminders.AddAsync(reminder, cancellationToken);

    public void Remove(Reminder reminder) =>
        _dbContext.Reminders.Remove(reminder);

    public async Task<Reminder?> GetByIdForUserAsync(int id, int userId, CancellationToken cancellationToken = default)
        => await _dbContext.Reminders
            .SingleOrDefaultAsync(reminder => reminder.Id == id && reminder.UserId == userId, cancellationToken);
}
