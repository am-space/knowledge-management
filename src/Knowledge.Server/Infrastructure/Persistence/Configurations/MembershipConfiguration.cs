using Knowledge.Server.Workspaces.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Knowledge.Server.Infrastructure.Persistence.Configurations;

public sealed class MembershipConfiguration : IEntityTypeConfiguration<Membership>
{
    public void Configure(EntityTypeBuilder<Membership> builder)
    {
        builder.ToTable("Memberships");
        builder.HasKey(membership => new { membership.WorkspaceId, membership.UserId });
        builder.Property(membership => membership.Role)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(membership => membership.JoinedAt).IsRequired();
        builder.HasOne<Workspace>()
            .WithMany()
            .HasForeignKey(membership => membership.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(membership => membership.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(membership => membership.UserId);
    }
}
