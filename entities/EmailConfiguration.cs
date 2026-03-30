using App.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Data.Configurations;

public class EmailConfiguration : IEntityTypeConfiguration<Email>
{
    public void Configure(EntityTypeBuilder<Email> builder)
    {
        builder.ToTable("Emails");

        // Primary key
        builder.HasKey(e => e.EmailId)
               .HasName("PK_Emails");

        // Columns
        builder.Property(e => e.EmailId)
               .HasColumnType("varchar(36)")
               .IsRequired();

        builder.Property(e => e.InboxOwnerId)
               .HasColumnType("varchar(36)")
               .IsRequired();

        builder.Property(e => e.FromUserId)
               .HasColumnType("varchar(36)");

        builder.Property(e => e.ToUserId)
               .HasColumnType("varchar(36)");

        builder.Property(e => e.FromAddress)
               .HasColumnType("varchar(255)")
               .IsRequired();

        builder.Property(e => e.ToAddress)
               .HasColumnType("varchar(255)")
               .IsRequired();

        builder.Property(e => e.Subject)
               .HasColumnType("varchar(500)");

        builder.Property(e => e.Body)
               .HasColumnType("text");

        builder.Property(e => e.SentAt)
               .IsRequired()
               .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(e => e.IsRead)
               .IsRequired()
               .HasDefaultValue(false);

        builder.Property(e => e.IsDeleted)
               .IsRequired()
               .HasDefaultValue(false);

        // FK → Users.UserId  (InboxOwner, CASCADE)
        builder.HasOne(e => e.InboxOwner)
               .WithMany(u => u.InboxEmails)
               .HasForeignKey(e => e.InboxOwnerId)
               .HasConstraintName("FK_Emails_InboxOwner")
               .OnDelete(DeleteBehavior.Cascade);

        // FK → Users.UserId  (FromUser, SET NULL)
        builder.HasOne(e => e.FromUser)
               .WithMany(u => u.SentEmails)
               .HasForeignKey(e => e.FromUserId)
               .HasConstraintName("FK_Emails_FromUser")
               .OnDelete(DeleteBehavior.SetNull);

        // FK → Users.UserId  (ToUser, SET NULL)
        builder.HasOne(e => e.ToUser)
               .WithMany(u => u.ReceivedEmails)
               .HasForeignKey(e => e.ToUserId)
               .HasConstraintName("FK_Emails_ToUser")
               .OnDelete(DeleteBehavior.SetNull);

        // Indexes
        builder.HasIndex(e => e.InboxOwnerId)
               .HasDatabaseName("IDX_Emails_InboxOwner");

        builder.HasIndex(e => e.FromUserId)
               .HasDatabaseName("IDX_Emails_FromUser");

        builder.HasIndex(e => e.ToUserId)
               .HasDatabaseName("IDX_Emails_ToUser");
    }
}
