using Microsoft.Extensions.DependencyInjection;
using TreatmentService.Application.Abstractions;
using TreatmentService.Application.Reminders.Commands.AddReminder;
using TreatmentService.Application.Reminders.Commands.DeleteReminder;
using TreatmentService.Application.Reminders.Commands.UpdateReminder;
using TreatmentService.Application.Reminders.Models;
using TreatmentService.Application.Reminders.Queries.GetAllReminders;
using TreatmentService.Application.Reminders.Queries.GetById;

namespace TreatmentService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<
            IQueryHandler<GetAllRemindersQueryParams, IEnumerable<ReminderDto>>,
            GetAllRemindersQueryHandler>();

        services.AddScoped<
            IQueryHandler<GetReminderByIdQueryParams, ReminderDto>,
            GetReminderByIdQueryHandler>();

        services.AddScoped<
            ICommandHandler<AddReminderCommandParams, ReminderDto>,
            AddReminderCommandHandler>();

        services.AddScoped<
            ICommandHandler<UpdateReminderCommandParams, ReminderDto>,
            UpdateReminderCommandHandler>();

        services.AddScoped<
            ICommandHandler<DeleteReminderCommandParams>,
            DeleteReminderCommandHandler>();

        return services;
    }
}
