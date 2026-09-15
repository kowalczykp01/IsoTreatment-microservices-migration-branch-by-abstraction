using TreatmentService.Application.Abstractions;
using TreatmentService.Application.Reminders.Models;

namespace TreatmentService.Application.Reminders.Queries.GetById;

public sealed record GetReminderByIdQueryParams(int Id, int UserId) : IQuery<ReminderDto>;
