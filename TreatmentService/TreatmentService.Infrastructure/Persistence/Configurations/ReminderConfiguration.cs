using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TreatmentService.Domain.Entities;

namespace TreatmentService.Infrastructure.Persistence.Configurations;

internal sealed class ReminderConfiguration : IEntityTypeConfiguration<Reminder>
{
    public void Configure(EntityTypeBuilder<Reminder> builder)
    {
        builder.ToTable("Reminders");

        builder.HasKey(reminder => reminder.Id);

        builder.Property(reminder => reminder.Id)
            .ValueGeneratedOnAdd();

        builder.Property(reminder => reminder.Time)
            .HasColumnType("time");

        builder.Property(reminder => reminder.UserId);

        builder.HasIndex(reminder => reminder.UserId);
    }
}
