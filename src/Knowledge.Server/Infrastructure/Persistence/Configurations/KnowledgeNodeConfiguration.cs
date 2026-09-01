using Knowledge.Server.Knowledge.Domain;
using Knowledge.Server.Workspaces.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Knowledge.Server.Infrastructure.Persistence.Configurations;

public sealed class KnowledgeNodeConfiguration : IEntityTypeConfiguration<KnowledgeNode>
{
    public void Configure(EntityTypeBuilder<KnowledgeNode> builder)
    {
        builder.ToTable("KnowledgeNodes", table => table.HasCheckConstraint(
            "CK_KnowledgeNodes_ParentIsNotSelf",
            "\"ParentId\" IS NULL OR \"ParentId\" <> \"Id\""));
        builder.HasKey(node => node.Id);
        builder.HasAlternateKey(node => new { node.WorkspaceId, node.Id });
        builder.Property(node => node.Type)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();
        builder.Property(node => node.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();
        builder.Property(node => node.CreatedAt).IsRequired();
        builder.Property(node => node.CreatedBy).IsRequired();

        builder.HasOne<Workspace>()
            .WithMany()
            .HasForeignKey(node => node.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(node => node.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<KnowledgeNode>()
            .WithMany()
            .HasForeignKey(node => new { node.WorkspaceId, node.ParentId })
            .HasPrincipalKey(node => new { node.WorkspaceId, node.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(node => node.Revisions)
            .WithOne()
            .HasForeignKey(revision => new { revision.WorkspaceId, revision.NodeId })
            .HasPrincipalKey(node => new { node.WorkspaceId, node.Id })
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(node => node.CurrentRevision)
            .WithMany()
            .HasForeignKey(node => new { node.WorkspaceId, node.Id, node.CurrentRevisionId })
            .HasPrincipalKey(revision => new { revision.WorkspaceId, revision.NodeId, revision.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(node => node.Revisions).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasIndex(node => new { node.WorkspaceId, node.ParentId });
        builder.HasIndex(node => new { node.WorkspaceId, node.Type, node.Status });
    }
}
