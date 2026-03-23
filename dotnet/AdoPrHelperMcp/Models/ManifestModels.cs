namespace AdoPrHelperMcp.Models;

/// <summary>
/// Complete manifest data structure saved to manifest.json
/// </summary>
public record ManifestData
{
    /// <summary>
    /// Full PR URL
    /// </summary>
    public required string PrUrl { get; init; }

    /// <summary>
    /// PR ID number
    /// </summary>
    public required int PrId { get; init; }

    /// <summary>
    /// PR title
    /// </summary>
    public required string PrTitle { get; init; }

    /// <summary>
    /// PR description
    /// </summary>
    public string? PrDescription { get; init; }

    /// <summary>
    /// PR author information
    /// </summary>
    public required PrAuthor PrAuthor { get; init; }

    /// <summary>
    /// PR status (active, completed, abandoned)
    /// </summary>
    public required string PrStatus { get; init; }

    /// <summary>
    /// Source branch name
    /// </summary>
    public required string SourceBranch { get; init; }

    /// <summary>
    /// Target branch name
    /// </summary>
    public required string TargetBranch { get; init; }

    /// <summary>
    /// When the PR was created
    /// </summary>
    public required string CreatedDate { get; init; }

    /// <summary>
    /// When the data was fetched
    /// </summary>
    public required string FetchTimestamp { get; init; }

    /// <summary>
    /// Statistics about the changes
    /// </summary>
    public required ChangeStatistics Statistics { get; init; }

    /// <summary>
    /// List of all changed files with metadata
    /// </summary>
    public required ManifestFile[] Files { get; init; }
}

/// <summary>
/// PR author information
/// </summary>
public record PrAuthor
{
    /// <summary>
    /// Display name
    /// </summary>
    public required string DisplayName { get; init; }

    /// <summary>
    /// Email address
    /// </summary>
    public required string Email { get; init; }
}

/// <summary>
/// Statistics about PR changes
/// </summary>
public record ChangeStatistics
{
    /// <summary>
    /// Total number of files changed
    /// </summary>
    public required int TotalFiles { get; init; }

    /// <summary>
    /// Total size in bytes
    /// </summary>
    public required long TotalSizeBytes { get; init; }

    /// <summary>
    /// Breakdown of changes by type
    /// </summary>
    public required ChangeBreakdown Changes { get; init; }
}

/// <summary>
/// Breakdown of changes by type
/// </summary>
public record ChangeBreakdown
{
    /// <summary>
    /// Number of added files
    /// </summary>
    public required int Added { get; init; }

    /// <summary>
    /// Number of modified files
    /// </summary>
    public required int Modified { get; init; }

    /// <summary>
    /// Number of deleted files
    /// </summary>
    public required int Deleted { get; init; }

    /// <summary>
    /// Number of renamed files
    /// </summary>
    public required int Renamed { get; init; }
}

/// <summary>
/// Metadata for a single file in the manifest
/// </summary>
public record ManifestFile
{
    /// <summary>
    /// Original file path (with / or \)
    /// </summary>
    public required string OriginalPath { get; init; }

    /// <summary>
    /// Escaped filename for storage (with ~~~)
    /// </summary>
    public required string EscapedName { get; init; }

    /// <summary>
    /// Diff filename (escaped_name.diff)
    /// </summary>
    public required string DiffName { get; init; }

    /// <summary>
    /// Change type (add, edit, delete, rename)
    /// </summary>
    public required string ChangeType { get; init; }

    /// <summary>
    /// File size in bytes
    /// </summary>
    public required long SizeBytes { get; init; }

    /// <summary>
    /// Diff size in bytes
    /// </summary>
    public required long DiffSizeBytes { get; init; }

    /// <summary>
    /// Number of lines added
    /// </summary>
    public required int LinesAdded { get; init; }

    /// <summary>
    /// Number of lines deleted
    /// </summary>
    public required int LinesDeleted { get; init; }
}

/// <summary>
/// Response from azure_devops_fetch_pr_changes tool
/// </summary>
public record FetchPrResponse
{
    /// <summary>
    /// Success status
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Path to manifest.json file
    /// </summary>
    public required string ManifestPath { get; init; }

    /// <summary>
    /// Number of files saved
    /// </summary>
    public required int FilesSaved { get; init; }

    /// <summary>
    /// Total bytes written
    /// </summary>
    public required long TotalBytes { get; init; }

    /// <summary>
    /// Summary of changes
    /// </summary>
    public required ChangeBreakdown Summary { get; init; }
}
