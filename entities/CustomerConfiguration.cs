using App.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Data.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");

        // Primary key
        builder.HasKey(c => c.CustomerId)
               .HasName("PK_Customers");

        // Columns
        builder.Property(c => c.CustomerId)
               .HasColumnType("varchar(36)")
               .IsRequired();

        builder.Property(c => c.ManagedByUserId)
               .HasColumnType("varchar(36)");

        builder.Property(c => c.CustomerName)
               .HasColumnType("varchar(255)")
               .IsRequired();

        builder.Property(c => c.Email)
               .HasColumnType("varchar(255)");

        builder.Property(c => c.Phone)
               .HasColumnType("varchar(50)");

        builder.Property(c => c.AccountType)
               .HasColumnType("varchar(50)")
               .IsRequired();

        builder.Property(c => c.SubscriptionStatus)
               .HasColumnType("varchar(50)")
               .IsRequired()
               .HasDefaultValue("active");

        builder.Property(c => c.CreatedAt)
               .IsRequired()
               .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(c => c.UpdatedAt)
               .IsRequired()
               .HasDefaultValueSql("CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP");

        // FK → Users.UserId  (ManagedBy, SET NULL)
        builder.HasOne(c => c.ManagedBy)
               .WithMany(u => u.ManagedCustomers)
               .HasForeignKey(c => c.ManagedByUserId)
               .HasConstraintName("FK_Customers_ManagedBy")
               .OnDelete(DeleteBehavior.SetNull);

        // Indexes
        builder.HasIndex(c => c.ManagedByUserId)
               .HasDatabaseName("IDX_Customers_Manager");

        builder.HasIndex(c => c.AccountType)
               .HasDatabaseName("IDX_Customers_AccountType");
    }
}
