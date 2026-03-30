using App.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        // Primary key
        builder.HasKey(u => u.UserId)
               .HasName("PK_Users");

        // Columns
        builder.Property(u => u.UserId)
               .HasColumnType("varchar(36)")
               .IsRequired();

        builder.Property(u => u.Username)
               .HasColumnType("varchar(100)")
               .IsRequired();

        builder.Property(u => u.Email)
               .HasColumnType("varchar(255)")
               .IsRequired();

        builder.Property(u => u.FullName)
               .HasColumnType("varchar(200)")
               .IsRequired();

        builder.Property(u => u.Department)
               .HasColumnType("varchar(100)");

        builder.Property(u => u.Role)
               .HasColumnType("varchar(100)");

        builder.Property(u => u.CreatedAt)
               .IsRequired()
               .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(u => u.IsActive)
               .IsRequired()
               .HasDefaultValue(true);

        // Unique constraints
        builder.HasIndex(u => u.Username)
               .IsUnique()
               .HasDatabaseName("UQ_Users_Username");

        builder.HasIndex(u => u.Email)
               .IsUnique()
               .HasDatabaseName("UQ_Users_Email");
    }
}
