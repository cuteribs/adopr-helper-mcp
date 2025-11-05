namespace AdoPrHelperMcp.Models;

/// <summary>
/// Parsed pull request information from URL
/// </summary>
public record PrInfo
{
    /// <summary>
    /// Azure DevOps organization name
    /// </summary>
    public required string Organization { get; init; }

    /// <summary>
    /// Project name
    /// </summary>
    public required string Project { get; init; }

    /// <summary>
    /// Repository name
    /// </summary>
    public required string Repository { get; init; }

    /// <summary>
    /// PR ID number
    /// </summary>
    public required int PullRequestId { get; init; }
}
