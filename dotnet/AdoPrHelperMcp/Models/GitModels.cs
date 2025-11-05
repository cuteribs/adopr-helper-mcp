namespace AdoPrHelperMcp.Models;

/// <summary>
/// Response from Azure DevOps diffs/commits API
/// </summary>
public record CommitDiffs
{
    /// <summary>
    /// Array of file changes
    /// </summary>
    public required GitChange[] Changes { get; init; }
}

/// <summary>
/// A single file change in a commit
/// </summary>
public record GitChange
{
    /// <summary>
    /// The changed file item
    /// </summary>
    public required GitItem Item { get; init; }

    /// <summary>
    /// Type of change (add, edit, delete, rename, move)
    /// </summary>
    public required string ChangeType { get; init; }
}

/// <summary>
/// Git item (file or folder) metadata
/// </summary>
public record GitItem
{
    /// <summary>
    /// New version SHA (if exists)
    /// </summary>
    public string? ObjectId { get; init; }

    /// <summary>
    /// Original version SHA (if exists)
    /// </summary>
    public string? OriginalObjectId { get; init; }

    /// <summary>
    /// Type: blob (file) or tree (folder)
    /// </summary>
    public required string GitObjectType { get; init; }

    /// <summary>
    /// Commit SHA that includes this change
    /// </summary>
    public required string CommitId { get; init; }

    /// <summary>
    /// File path relative to repo root
    /// </summary>
    public required string Path { get; init; }

    /// <summary>
    /// True if this is a folder
    /// </summary>
    public bool? IsFolder { get; init; }

    /// <summary>
    /// API URL to fetch this item
    /// </summary>
    public required string Url { get; init; }
}

/// <summary>
/// File patch with unified diff
/// </summary>
public record FilePatch
{
    /// <summary>
    /// Path to the file
    /// </summary>
    public required string FilePath { get; init; }

    /// <summary>
    /// Original file content (optional)
    /// </summary>
    public string? SourceContent { get; init; }

    /// <summary>
    /// Unified diff patch
    /// </summary>
    public required string Patch { get; init; }
}
