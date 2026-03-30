using App.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Data.Configurations;

public class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.ToTable("Documents");

        // Primary key
        builder.HasKey(d => d.DocumentId)
               .HasName("PK_Documents");

        // Columns
        builder.Property(d => d.DocumentId)
               .HasColumnType("varchar(36)")
               .IsRequired();

        builder.Property(d => d.OwnerId)
               .HasColumnType("varchar(36)")
               .IsRequired();

        builder.Property(d => d.Title)
               .HasColumnType("varchar(255)")
               .IsRequired();

        builder.Property(d => d.FilePath)
               .HasColumnType("varchar(500)");

        builder.Property(d => d.Content)
               .HasColumnType("text");

        builder.Property(d => d.FolderPath)
               .HasColumnType("varchar(500)");

        builder.Property(d => d.CreatedAt)
               .IsRequired()
               .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(d => d.UpdatedAt)
               .IsRequired()
               .HasDefaultValueSql("CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP");

        builder.Property(d => d.IsDeleted)
               .IsRequired()
               .HasDefaultValue(false);

        // Foreign key → Users.UserId (CASCADE)
        builder.HasOne(d => d.Owner)
               .WithMany(u => u.Documents)
               .HasForeignKey(d => d.OwnerId)
               .HasConstraintName("FK_Documents_Owner")
               .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(d => d.OwnerId)
               .HasDatabaseName("IDX_Documents_Owner");

        // FolderPath index with prefix length – expressed as a raw index annotation
        // (Pomelo MySql provider supports HasPrefixLength)
        builder.HasIndex(d => d.FolderPath)
               .HasDatabaseName("IDX_Documents_Folder");
    }
}
