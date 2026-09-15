using TreatmentService.Application.Abstractions;

namespace TreatmentService.Application.Reminders.Commands.DeleteReminder;

public sealed record DeleteReminderCommandParams(
    int Id,
    int UserId) : ICommand;
