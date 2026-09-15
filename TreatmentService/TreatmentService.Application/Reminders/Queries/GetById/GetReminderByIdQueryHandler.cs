using TreatmentService.Application.Abstractions;
using TreatmentService.Application.Exceptions;
using TreatmentService.Application.Reminders.Mappers;
using TreatmentService.Application.Reminders.Models;
using TreatmentService.Domain.UnitOfWork;

namespace TreatmentService.Application.Reminders.Queries.GetById;

public sealed class GetReminderByIdQueryHandler(IUnitOfWork unitOfWork)
    : IQueryHandler<GetReminderByIdQueryParams, ReminderDto>
{
    public async Task<ReminderDto> HandleAsync(
        GetReminderByIdQueryParams query,
        CancellationToken cancellationToken)
    {
        var reminder = await unitOfWork.ReminderRepository
            .GetByIdForUserAsync(query.Id, query.UserId, cancellationToken)
            ?? throw new ReminderNotFoundException();

        return reminder.MapReminderToReminderDto();
    }
}
