using App.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Data.Configurations;

public class CalendarEventConfiguration : IEntityTypeConfiguration<CalendarEvent>
{
    public void Configure(EntityTypeBuilder<CalendarEvent> builder)
    {
        builder.ToTable("CalendarEvents");

        // Primary key
        builder.HasKey(e => e.EventId)
               .HasName("PK_CalendarEvents");

        // Columns
        builder.Property(e => e.EventId)
               .HasColumnType("varchar(36)")
               .IsRequired();

        builder.Property(e => e.CalendarOwnerId)
               .HasColumnType("varchar(36)")
               .IsRequired();

        builder.Property(e => e.OrganizerUserId)
               .HasColumnType("varchar(36)");

        builder.Property(e => e.Title)
               .HasColumnType("varchar(255)")
               .IsRequired();

        builder.Property(e => e.Description)
               .HasColumnType("text");

        builder.Property(e => e.StartTime)
               .IsRequired();

        builder.Property(e => e.EndTime)
               .IsRequired();

        builder.Property(e => e.Location)
               .HasColumnType("varchar(255)");

        builder.Property(e => e.CreatedAt)
               .IsRequired()
               .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(e => e.IsDeleted)
               .IsRequired()
               .HasDefaultValue(false);

        // FK → Users.UserId  (CalendarOwner, CASCADE)
        builder.HasOne(e => e.CalendarOwner)
               .WithMany(u => u.OwnedCalendarEvents)
               .HasForeignKey(e => e.CalendarOwnerId)
               .HasConstraintName("FK_CalendarEvents_Owner")
               .OnDelete(DeleteBehavior.Cascade);

        // FK → Users.UserId  (Organizer, SET NULL)
        builder.HasOne(e => e.Organizer)
               .WithMany(u => u.OrganizedCalendarEvents)
               .HasForeignKey(e => e.OrganizerUserId)
               .HasConstraintName("FK_CalendarEvents_Organizer")
               .OnDelete(DeleteBehavior.SetNull);

        // Indexes
        builder.HasIndex(e => e.CalendarOwnerId)
               .HasDatabaseName("IDX_CalendarEvents_Owner");

        builder.HasIndex(e => e.StartTime)
               .HasDatabaseName("IDX_CalendarEvents_Start");
    }
}
