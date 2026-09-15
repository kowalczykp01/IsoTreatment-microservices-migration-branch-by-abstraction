using TreatmentService.Application.Reminders.Models;
using TreatmentService.Domain.Entities;

namespace TreatmentService.Application.Reminders.Mappers;

public static class ReminderMapper
{
    public static IEnumerable<ReminderDto> MapRemindersToReminderDtos(
        this IEnumerable<Reminder> reminders)
        => reminders.Select(x => new ReminderDto(x.Id, x.Time));

    public static ReminderDto MapReminderToReminderDto(
        this Reminder reminder)
        => new(reminder.Id, reminder.Time);
}
