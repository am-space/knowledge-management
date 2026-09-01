using System.ComponentModel.DataAnnotations;

namespace Knowledge.Server.Workspaces.Infrastructure;

public sealed class LocalWorkspaceOptions
{
    public const string SectionName = "LocalWorkspace";

    [Required]
    [MaxLength(200)]
    public string OwnerDisplayName { get; init; } = "Local Owner";

    [Required]
    [MaxLength(200)]
    public string WorkspaceName { get; init; } = "Personal Knowledge";
}
