using TreatmentService.Domain.Entities;

namespace TreatmentService.Domain.Repositories;

public interface IReminderRepository
{
    Task<Reminder?> GetByIdForUserAsync(int id, int userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Reminder>> GetAllForUserAsync(int userId, CancellationToken cancellationToken = default);

    Task AddAsync(Reminder reminder, CancellationToken cancellationToken = default);

    void Remove(Reminder reminder);
}
