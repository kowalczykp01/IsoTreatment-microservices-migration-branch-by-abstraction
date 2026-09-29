using TreatmentService.Application.Abstractions;
using TreatmentService.Application.Reminders.Models;

namespace TreatmentService.Application.Reminders.Queries.GetAllReminders;

public sealed record GetAllRemindersQueryParams(int UserId) : IQuery<IEnumerable<ReminderDto>>;
