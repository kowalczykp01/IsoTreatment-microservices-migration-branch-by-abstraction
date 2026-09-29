namespace TreatmentService.Domain.Entities;

public sealed class Reminder
{
    public int Id { get; }
    public TimeOnly Time { get; private set; }
    public int UserId { get; }

    private Reminder(TimeOnly time, int userId)
    {
        Time = time;
        UserId = userId;
    }

    public static Reminder Create(TimeOnly time, int userId)
    {
        var newReminder = new Reminder(time, userId);

        return newReminder;
    }

    public void ChangeTime(TimeOnly time)
    {
        this.Time = time;
    }
}