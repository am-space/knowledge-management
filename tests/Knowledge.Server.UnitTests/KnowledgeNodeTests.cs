using Knowledge.Server.Knowledge.Domain;

namespace Knowledge.Server.UnitTests;

public sealed class KnowledgeNodeTests
{
    [Fact]
    public void CreateArticle_CreatesStableNodeAndInitialRevision()
    {
        var nodeId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var revisionId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var timestamp = DateTimeOffset.UtcNow;

        var node = KnowledgeNode.CreateArticle(
            nodeId,
            workspaceId,
            revisionId,
            " Article ",
            "# Article\n",
            actorId,
            timestamp);

        Assert.Equal(nodeId, node.Id);
        Assert.Equal(workspaceId, node.WorkspaceId);
        Assert.Equal(KnowledgeNodeType.Article, node.Type);
        Assert.Equal(KnowledgeNodeStatus.Active, node.Status);
        Assert.Equal(revisionId, node.CurrentRevisionId);
        var revision = Assert.Single(node.Revisions);
        Assert.Same(revision, node.CurrentRevision);
        Assert.Equal(1, revision.Version);
        Assert.Equal("Article", revision.Title);
        Assert.Equal("# Article\n", revision.ContentMarkdown);
    }

    [Fact]
    public void AddRevision_AdvancesVersionWithoutChangingPreviousRevision()
    {
        var node = CreateArticle();
        var initialRevision = Assert.Single(node.Revisions);

        var revision = node.AddRevision(
            Guid.NewGuid(),
            expectedVersion: 1,
            "Updated",
            "Updated content",
            Guid.NewGuid(),
            DateTimeOffset.UtcNow);

        Assert.Equal(2, revision.Version);
        Assert.Equal(revision.Id, node.CurrentRevisionId);
        Assert.Same(revision, node.CurrentRevision);
        Assert.Equal(2, node.Revisions.Count);
        Assert.Equal(1, initialRevision.Version);
        Assert.Equal("Initial", initialRevision.Title);
        Assert.Equal("Initial content", initialRevision.ContentMarkdown);
    }

    [Fact]
    public void AddRevision_RejectsStaleExpectedVersionWithoutPartialRevision()
    {
        var node = CreateArticle();

        var exception = Assert.Throws<RevisionConflictException>(() => node.AddRevision(
            Guid.NewGuid(),
            expectedVersion: 2,
            "Updated",
            "Updated content",
            Guid.NewGuid(),
            DateTimeOffset.UtcNow));

        Assert.Equal(2, exception.ExpectedVersion);
        Assert.Equal(1, exception.CurrentVersion);
        Assert.Single(node.Revisions);
        Assert.Equal(1, node.CurrentRevision?.Version);
    }

    [Theory]
    [InlineData("", "content")]
    [InlineData("   ", "content")]
    public void CreateArticle_RejectsInvalidTitle(string title, string content)
    {
        Assert.Throws<ArgumentException>(() => KnowledgeNode.CreateArticle(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            title,
            content,
            Guid.NewGuid(),
            DateTimeOffset.UtcNow));
    }

    [Fact]
    public void CreateArticle_RejectsMissingWorkspace()
    {
        Assert.Throws<ArgumentException>(() => KnowledgeNode.CreateArticle(
            Guid.NewGuid(),
            Guid.Empty,
            Guid.NewGuid(),
            "Initial",
            "Initial content",
            Guid.NewGuid(),
            DateTimeOffset.UtcNow));
    }

    [Fact]
    public void AddRevision_RejectsMissingRevisionIdentityWithoutPartialRevision()
    {
        var node = CreateArticle();

        Assert.Throws<ArgumentException>(() => node.AddRevision(
            Guid.Empty,
            expectedVersion: 1,
            "Updated",
            "Updated content",
            Guid.NewGuid(),
            DateTimeOffset.UtcNow));
        Assert.Single(node.Revisions);
        Assert.Equal(1, node.CurrentRevision?.Version);
    }

    private static KnowledgeNode CreateArticle() => KnowledgeNode.CreateArticle(
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid(),
        "Initial",
        "Initial content",
        Guid.NewGuid(),
        DateTimeOffset.UtcNow);
}
