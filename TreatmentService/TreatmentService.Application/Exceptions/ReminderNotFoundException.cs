namespace TreatmentService.Application.Exceptions;

public sealed class ReminderNotFoundException : NotFoundException
{
    public ReminderNotFoundException() : base("Reminder not found")
    {
    }
}
