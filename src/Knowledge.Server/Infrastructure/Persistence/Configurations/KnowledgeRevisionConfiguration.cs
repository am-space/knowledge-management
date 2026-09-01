using Knowledge.Server.Knowledge.Domain;
using Knowledge.Server.Workspaces.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Knowledge.Server.Infrastructure.Persistence.Configurations;

public sealed class KnowledgeRevisionConfiguration : IEntityTypeConfiguration<KnowledgeRevision>
{
    public void Configure(EntityTypeBuilder<KnowledgeRevision> builder)
    {
        builder.ToTable("KnowledgeRevisions", table => table.HasCheckConstraint(
            "CK_KnowledgeRevisions_VersionPositive",
            "\"Version\" > 0"));
        builder.HasKey(revision => revision.Id);
        builder.HasAlternateKey(revision => new
        {
            revision.WorkspaceId,
            revision.NodeId,
            revision.Id,
        });
        builder.HasIndex(revision => new { revision.NodeId, revision.Version }).IsUnique();
        builder.Property(revision => revision.Version).IsRequired();
        builder.Property(revision => revision.Title)
            .HasMaxLength(KnowledgeRevision.MaxTitleLength)
            .IsRequired();
        builder.Property(revision => revision.ContentMarkdown).IsRequired();
        builder.Property(revision => revision.CreatedAt).IsRequired();
        builder.Property(revision => revision.CreatedBy).IsRequired();
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(revision => revision.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
