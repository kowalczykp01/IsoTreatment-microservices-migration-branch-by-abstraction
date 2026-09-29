using TreatmentService.Application.Abstractions;
using TreatmentService.Application.Exceptions;
using TreatmentService.Application.Reminders.Mappers;
using TreatmentService.Application.Reminders.Models;
using TreatmentService.Domain.UnitOfWork;

namespace TreatmentService.Application.Reminders.Commands.UpdateReminder;

public sealed class UpdateReminderCommandHandler(IUnitOfWork unitOfWork)
    : ICommandHandler<UpdateReminderCommandParams, ReminderDto>
{
    public async Task<ReminderDto> HandleAsync(
        UpdateReminderCommandParams command,
        CancellationToken cancellationToken)
    {
        var reminder = await unitOfWork.ReminderRepository
            .GetByIdForUserAsync(command.Id, command.UserId, cancellationToken)
            ?? throw new ReminderNotFoundException();

        reminder.ChangeTime(command.Time);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return reminder.MapReminderToReminderDto();
    }
}
