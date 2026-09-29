using TreatmentService.Domain.Repositories;

namespace TreatmentService.Domain.UnitOfWork;

public interface IUnitOfWork : IDisposable
{
    IReminderRepository ReminderRepository { get; }

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
