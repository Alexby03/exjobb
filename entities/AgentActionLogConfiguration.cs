using App.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Data.Configurations;

public class AgentActionLogConfiguration : IEntityTypeConfiguration<AgentActionLog>
{
    public void Configure(EntityTypeBuilder<AgentActionLog> builder)
    {
        builder.ToTable("AgentActionLog");

        // Composite primary key
        builder.HasKey(a => new { a.UserPromptId, a.ToolIndex });

        // Columns
        builder.Property(a => a.UserPromptId)
               .HasColumnType("bigint")
               .IsRequired();

        builder.Property(a => a.ToolIndex)
               .HasColumnType("int")
               .IsRequired();

        builder.Property(a => a.ToolName)
               .HasColumnType("varchar(64)")
               .IsRequired();

        builder.Property(a => a.Client)
               .HasColumnType("varchar(36)")
               .IsRequired();

        // TINYINT(1) — EF Core maps bool to tinyint(1) on MySQL automatically
        builder.Property(a => a.IsNL)
               .HasColumnType("tinyint(1)")
               .IsRequired();

        builder.Property(a => a.UserPrompt)
               .HasColumnType("text")
               .IsRequired();

        builder.Property(a => a.IsBad)
               .HasColumnType("tinyint(1)")
               .IsRequired();

        builder.Property(a => a.ReasonLog)
               .HasColumnType("text")
               .IsRequired();

        // Indexes
        builder.HasIndex(a => a.UserPromptId)
               .HasDatabaseName("IDX_AgentActionLog_UserPromptId");

        builder.HasIndex(a => a.Client)
               .HasDatabaseName("IDX_AgentActionLog_Client");
    }
}
