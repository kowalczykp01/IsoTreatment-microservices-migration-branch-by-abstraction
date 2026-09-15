using TreatmentService.Application.Abstractions;
using TreatmentService.Application.Reminders.Models;

namespace TreatmentService.Application.Reminders.Commands.UpdateReminder;

public sealed record UpdateReminderCommandParams(
    int Id,
    int UserId,
    TimeOnly Time) : ICommand<ReminderDto>;
