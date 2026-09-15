using TreatmentService.Application.Abstractions;
using TreatmentService.Application.Reminders.Models;

namespace TreatmentService.Application.Reminders.Commands.AddReminder;

public sealed record AddReminderCommandParams(
    int UserId,
    TimeOnly Time) : ICommand<ReminderDto>;