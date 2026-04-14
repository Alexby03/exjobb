using Microsoft.EntityFrameworkCore;

namespace App.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all IEntityTypeConfiguration<T> classes from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // ------------------------------------------------------------------
        // If you prefer to apply each configuration explicitly instead of
        // scanning the assembly, use the lines below:
        // ------------------------------------------------------------------
        // modelBuilder.ApplyConfiguration(new UserConfiguration());
        // modelBuilder.ApplyConfiguration(new DocumentConfiguration());
        // modelBuilder.ApplyConfiguration(new EmailConfiguration());
        // modelBuilder.ApplyConfiguration(new CustomerConfiguration());
        // modelBuilder.ApplyConfiguration(new CalendarEventConfiguration());
        // modelBuilder.ApplyConfiguration(new CalendarEventParticipantConfiguration());
        // modelBuilder.ApplyConfiguration(new AgentActionLogConfiguration());
    }
}
