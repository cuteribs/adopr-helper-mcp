namespace AdoPrHelperMcp.Models;

/// <summary>
/// Authentication options for Azure DevOps API
/// </summary>
public record AuthOptions
{
    /// <summary>
    /// Type of authentication: 'interactive' or 'pat'
    /// </summary>
    public required string Type { get; init; }

    /// <summary>
    /// Token or credentials based on auth type
    /// </summary>
    public required string Token { get; init; }
}
