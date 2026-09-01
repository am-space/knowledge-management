using Knowledge.Server.Workspaces.Domain;

namespace Knowledge.Server.UnitTests;

public sealed class WorkspaceDomainTests
{
    [Fact]
    public void Constructors_RequireIdentityAndNames()
    {
        Assert.Throws<ArgumentException>(() =>
            new User(Guid.Empty, "Owner", DateTimeOffset.UtcNow));
        Assert.Throws<ArgumentException>(() =>
            new User(Guid.NewGuid(), " ", DateTimeOffset.UtcNow));
        Assert.Throws<ArgumentException>(() =>
            new Workspace(Guid.NewGuid(), " ", Guid.NewGuid(), DateTimeOffset.UtcNow));
        Assert.Throws<ArgumentException>(() =>
            new Membership(Guid.Empty, Guid.NewGuid(), MembershipRole.Owner, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Membership_RejectsUnknownRole()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Membership(
            Guid.NewGuid(),
            Guid.NewGuid(),
            (MembershipRole)999,
            DateTimeOffset.UtcNow));
    }

    [Fact]
    public void User_RejectsDisplayNameOverMaximumLength()
    {
        Assert.Throws<ArgumentException>(() => new User(
            Guid.NewGuid(),
            new string('a', User.MaxDisplayNameLength + 1),
            DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Workspace_RejectsNameOverMaximumLength()
    {
        Assert.Throws<ArgumentException>(() => new Workspace(
            Guid.NewGuid(),
            new string('a', Workspace.MaxNameLength + 1),
            Guid.NewGuid(),
            DateTimeOffset.UtcNow));
    }
}
