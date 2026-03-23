namespace AdoPrHelperMcp.Models;

/// <summary>
/// Pull request details from Azure DevOps API
/// </summary>
public record PullRequest
{
    /// <summary>
    /// PR ID
    /// </summary>
    public required int PullRequestId { get; init; }

    /// <summary>
    /// PR title
    /// </summary>
    public string? Title { get; init; }

    /// <summary>
    /// PR description
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// PR status (active, completed, abandoned)
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// Merge status (succeeded, conflicts, etc.)
    /// </summary>
    public required string MergeStatus { get; init; }

    /// <summary>
    /// Source branch ref (e.g., refs/heads/feature)
    /// </summary>
    public required string SourceRefName { get; init; }

    /// <summary>
    /// Target branch ref (e.g., refs/heads/main)
    /// </summary>
    public required string TargetRefName { get; init; }

    /// <summary>
    /// PR creation date
    /// </summary>
    public string? CreationDate { get; init; }

    /// <summary>
    /// PR creator information
    /// </summary>
    public PullRequestCreator? CreatedBy { get; init; }
}

/// <summary>
/// Pull request creator information
/// </summary>
public record PullRequestCreator
{
    /// <summary>
    /// Display name
    /// </summary>
    public string? DisplayName { get; init; }

    /// <summary>
    /// Unique name (email)
    /// </summary>
    public string? UniqueName { get; init; }
}
