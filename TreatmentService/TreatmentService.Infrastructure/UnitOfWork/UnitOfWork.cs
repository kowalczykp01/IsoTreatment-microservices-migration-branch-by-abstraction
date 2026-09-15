using TreatmentService.Domain.Repositories;
using TreatmentService.Domain.UnitOfWork;
using TreatmentService.Infrastructure.Persistence;

namespace TreatmentService.Infrastructure.UnitOfWork;

internal sealed class UnitOfWork : IUnitOfWork
{
    private readonly TreatmentDbContext _dbContext;

    public UnitOfWork(TreatmentDbContext dbContext, IReminderRepository reminderRepository)
    {
        _dbContext = dbContext;
        ReminderRepository = reminderRepository;
    }

    public IReminderRepository ReminderRepository { get; }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);

    public void Dispose() => _dbContext.Dispose();
}
