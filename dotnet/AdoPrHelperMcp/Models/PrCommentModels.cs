namespace AdoPrHelperMcp.Models;

/// <summary>
/// Pull request thread structure for posting comments
/// </summary>
public record PrThread
{
    /// <summary>
    /// Array of comments in the thread
    /// </summary>
    public required PrComment[] Comments { get; init; }

    /// <summary>
    /// Context for where to place the thread
    /// </summary>
    public required ThreadContext ThreadContext { get; init; }
}

/// <summary>
/// Thread context for positioning comments
/// </summary>
public record ThreadContext
{
    /// <summary>
    /// File path
    /// </summary>
    public required string FilePath { get; init; }

    /// <summary>
    /// Start position
    /// </summary>
    public FilePosition? RightFileStart { get; init; }

    /// <summary>
    /// End position
    /// </summary>
    public FilePosition? RightFileEnd { get; init; }
}

/// <summary>
/// File position (line and offset)
/// </summary>
public record FilePosition
{
    /// <summary>
    /// Line number
    /// </summary>
    public required int Line { get; init; }

    /// <summary>
    /// Offset in line
    /// </summary>
    public required int Offset { get; init; }
}

/// <summary>
/// A single comment in a PR thread
/// </summary>
public record PrComment
{
    /// <summary>
    /// Comment text (supports Markdown)
    /// </summary>
    public required string Content { get; init; }
}

/// <summary>
/// Options for posting a PR comment
/// </summary>
public record PrCommentOptions
{
    /// <summary>
    /// Full PR URL
    /// </summary>
    public required string PrUrl { get; init; }

    /// <summary>
    /// Comment text to post
    /// </summary>
    public required string Comment { get; init; }

    /// <summary>
    /// File to attach comment to
    /// </summary>
    public required string FilePath { get; init; }

    /// <summary>
    /// Start line number
    /// </summary>
    public required int RightFileStartLine { get; init; }

    /// <summary>
    /// Offset in start line
    /// </summary>
    public required int RightFileStartOffset { get; init; }

    /// <summary>
    /// End line number
    /// </summary>
    public required int RightFileEndLine { get; init; }

    /// <summary>
    /// Offset in end line
    /// </summary>
    public required int RightFileEndOffset { get; init; }
}
