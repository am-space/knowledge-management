using Knowledge.Server.Workspaces.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Knowledge.Server.Infrastructure.Persistence.Configurations;

public sealed class WorkspaceConfiguration : IEntityTypeConfiguration<Workspace>
{
    public void Configure(EntityTypeBuilder<Workspace> builder)
    {
        builder.ToTable("Workspaces");
        builder.HasKey(workspace => workspace.Id);
        builder.Property(workspace => workspace.Name)
            .HasMaxLength(Workspace.MaxNameLength)
            .IsRequired();
        builder.Property(workspace => workspace.CreatedAt).IsRequired();
        builder.Property(workspace => workspace.CreatedBy).IsRequired();
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(workspace => workspace.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
