using App.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Data.Configurations;

public class CalendarEventParticipantConfiguration : IEntityTypeConfiguration<CalendarEventParticipant>
{
    public void Configure(EntityTypeBuilder<CalendarEventParticipant> builder)
    {
        builder.ToTable("CalendarEventParticipants");

        // Primary key
        builder.HasKey(p => p.ParticipantId)
               .HasName("PK_CalendarEventParticipants");

        // Columns
        builder.Property(p => p.ParticipantId)
               .HasColumnType("varchar(36)")
               .IsRequired();

        builder.Property(p => p.EventId)
               .HasColumnType("varchar(36)")
               .IsRequired();

        builder.Property(p => p.UserId)
               .HasColumnType("varchar(36)")
               .IsRequired();

        builder.Property(p => p.ResponseStatus)
               .HasColumnType("varchar(50)")
               .IsRequired()
               .HasDefaultValue("pending");

        // FK → CalendarEvents.EventId  (CASCADE)
        builder.HasOne(p => p.Event)
               .WithMany(e => e.Participants)
               .HasForeignKey(p => p.EventId)
               .HasConstraintName("FK_Participants_Event")
               .OnDelete(DeleteBehavior.Cascade);

        // FK → Users.UserId  (CASCADE)
        builder.HasOne(p => p.User)
               .WithMany(u => u.EventParticipations)
               .HasForeignKey(p => p.UserId)
               .HasConstraintName("FK_Participants_User")
               .OnDelete(DeleteBehavior.Cascade);

        // Unique constraint on (EventId, UserId)
        builder.HasIndex(p => new { p.EventId, p.UserId })
               .IsUnique()
               .HasDatabaseName("UQ_Participants_EventUser");

        // Covering indexes
        builder.HasIndex(p => p.EventId)
               .HasDatabaseName("IDX_Participants_Event");

        builder.HasIndex(p => p.UserId)
               .HasDatabaseName("IDX_Participants_User");
    }
}
