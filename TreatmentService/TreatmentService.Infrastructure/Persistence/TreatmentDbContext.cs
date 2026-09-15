using Microsoft.EntityFrameworkCore;
using TreatmentService.Domain.Entities;
using TreatmentService.Infrastructure.Persistence.Configurations;
using TreatmentService.Infrastructure.Persistence.Converters;

namespace TreatmentService.Infrastructure.Persistence;

public sealed class TreatmentDbContext : DbContext
{
    public TreatmentDbContext(DbContextOptions<TreatmentDbContext> options)
        : base(options)
    {
    }

    public DbSet<Reminder> Reminders => Set<Reminder>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new ReminderConfiguration());
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder builder)
    {
        base.ConfigureConventions(builder);

        builder.Properties<TimeOnly>()
            .HaveConversion<TimeOnlyConverter>();
    }
}
