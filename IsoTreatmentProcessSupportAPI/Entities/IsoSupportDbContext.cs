using IsoTreatmentProcessSupportAPI.Converters;
using Microsoft.EntityFrameworkCore;

namespace IsoTreatmentProcessSupportAPI.Entities
{
    public class IsoSupportDbContext : DbContext
    {
        public IsoSupportDbContext(DbContextOptions<IsoSupportDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Entry> Entries { get; set; }
        public DbSet<Reminder> Reminders { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>()
                .Property(u => u.FirstName)
                .IsRequired()
                .HasMaxLength(50);

            modelBuilder.Entity<User>()
                .Property(u => u.LastName)
                .IsRequired()
                .HasMaxLength(60);

            modelBuilder.Entity<User>()
                .Property(u => u.Email)
                .IsRequired();
        }

        protected override void ConfigureConventions(ModelConfigurationBuilder builder)
        {
            base.ConfigureConventions(builder);
            builder.Properties<TimeOnly>()
                .HaveConversion<TimeOnlyConverter>();
        }
    }
}
