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
}
