namespace AdoPrHelperMcp.Models;

/// <summary>
/// Error response structure for MCP tools
/// </summary>
public record ErrorResponse
{
    /// <summary>
    /// Success status (false for errors)
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Error details
    /// </summary>
    public required ErrorDetails Error { get; init; }
}

/// <summary>
/// Error details
/// </summary>
public record ErrorDetails
{
    /// <summary>
    /// Error code (e.g., PR_NOT_FOUND, AUTH_FAILED)
    /// </summary>
    public required string Code { get; init; }

    /// <summary>
    /// Human-readable error message
    /// </summary>
    public required string Message { get; init; }
}

/// <summary>
/// Known error codes
/// </summary>
public static class ErrorCodes
{
    public const string PrNotFound = "PR_NOT_FOUND";
    public const string AuthFailed = "AUTH_FAILED";
    public const string RateLimited = "RATE_LIMITED";
    public const string FileNotFound = "FILE_NOT_FOUND";
    public const string CommentFailed = "COMMENT_FAILED";
    public const string InvalidInput = "INVALID_INPUT";
    public const string Unknown = "UNKNOWN_ERROR";
}
