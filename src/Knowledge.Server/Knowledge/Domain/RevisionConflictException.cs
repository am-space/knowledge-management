namespace Knowledge.Server.Knowledge.Domain;

public sealed class RevisionConflictException(int expectedVersion, int currentVersion)
    : InvalidOperationException(
        $"Expected revision version {expectedVersion}, but the current version is {currentVersion}.")
{
    public int ExpectedVersion { get; } = expectedVersion;

    public int CurrentVersion { get; } = currentVersion;
}
