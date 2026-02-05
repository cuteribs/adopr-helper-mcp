# Azure DevOps PR Helper MCP Server (.NET 8)

A Model Context Protocol (MCP) server for Azure DevOps Pull Request operations, implemented in .NET 8.

## Features

- **Fetch PR Changes**: Downloads all changed files from a PR and saves them to disk with escaped paths and a comprehensive manifest
- **Post Comments**: Post review comments to PR threads at specific file locations with severity levels
- **Authentication**: Support for both interactive OAuth and Personal Access Token (PAT) authentication

## Prerequisites

- .NET 8 SDK or later
- Azure DevOps account
- An MCP-compatible AI client (GitHub Copilot, Claude Desktop, etc.)

## Installation

### Build from Source

```bash
cd AdoPrHelperMcp
dotnet build
dotnet publish -c Release -o publish
```

## Configuration

### For Personal Access Token (PAT) Authentication

1. Create a PAT in Azure DevOps with `Code (Read)` and `Code (Status)` permissions
2. Set the environment variable:
   ```bash
   export AZURE_DEVOPS_PAT="your-pat-token"
   ```

### For Interactive OAuth Authentication

No additional configuration needed - the browser will open for authentication.

## Usage

### Command Line Options

```bash
# Run with interactive authentication (default)
dotnet run

# Run with PAT authentication
dotnet run -- --authentication pat
dotnet run -- -a pat

# Show help
dotnet run -- --help
```

## MCP Server Configuration

### For GitHub Copilot in VS Code

Add to your VS Code `settings.json`:

```json
{
  "github.copilot.chat.mcp.servers": {
    "adopr-helper-dotnet": {
      "command": "dotnet",
      "args": ["run", "--project", "path/to/AdoPrHelperMcp/AdoPrHelperMcp.csproj"],
      "env": {
        "AZURE_DEVOPS_PAT": "your-pat-here"  // Optional, for PAT auth
      }
    }
  }
}
```

### For Claude Desktop

Add to `claude_desktop_config.json`:

```json
{
  "mcpServers": {
    "adopr-helper-dotnet": {
      "command": "dotnet",
      "args": ["run", "--project", "path/to/AdoPrHelperMcp/AdoPrHelperMcp.csproj"],
      "env": {
        "AZURE_DEVOPS_PAT": "your-pat-here"  // Optional, for PAT auth
      }
    }
  }
}
```

## Available Tools

### azure_devops_fetch_pr_changes

Fetches all changed files from a pull request and **saves them directly to a local folder**. This prevents context overflow by keeping file contents out of the agent's context.

**Parameters:**
- `pr_url` (string, required): The full URL of the Azure DevOps pull request
- `output_folder` (string, required): Local folder path where files will be saved

**What it does:**
1. Fetches PR metadata (title, author, branches, timestamps)
2. Gets list of all changed files with diffs
3. For each file:
   - Escapes the file path using `~~~` separator (e.g., `src/services/UserService.cs` → `src~~~services~~~UserService.cs`)
   - Writes full file content to `{escaped_name}`
   - Writes diff to `{escaped_name}.diff`
4. Creates `manifest.json` with comprehensive metadata
5. Returns small summary (NOT file contents)

**Output Structure:**
```
output_folder/
├── manifest.json                             # Metadata
├── src~~~services~~~UserService.cs           # Full file
├── src~~~services~~~UserService.cs.diff      # Diff
└── ...
```

**Response (small, keeps context clean):**
```json
{
  "success": true,
  "manifestPath": "{output_folder}/manifest.json",
  "filesSaved": 15,
  "totalBytes": 52000,
  "summary": {
    "added": 3,
    "modified": 10,
    "deleted": 2,
    "renamed": 0
  }
}
```

**Example:**
```
Fetch changes from https://dev.azure.com/myorg/myproject/_git/myrepo/pullrequest/123 and save to ./pr-changes
```

### azure_devops_post_comment

Posts a review comment to a specific file and line in an Azure DevOps pull request.

**Parameters:**
- `pr_url` (string, required): The full URL of the Azure DevOps pull request
- `file_path` (string, required): File path to attach comment to
- `line_number` (number, required): Line number to attach comment to
- `comment_text` (string, required): The comment text to post
- `severity` (string, optional): Severity level - `Critical`, `High`, `Medium`, `Low`
- `thread_status` (string, optional): Thread status - `active`, `fixed`, `wontFix`, `closed`

**Severity Levels:**
- `Critical` - Security vulnerabilities, data loss risks
- `High` - Bugs, logic errors, potential crashes
- `Medium` - Code quality, performance issues
- `Low` - Best practice suggestions, minor improvements

**Example:**
```
Post comment "Potential SQL injection vulnerability" to line 42 of src/UserService.cs with severity "Critical"
```

**Recommended Comment Format:**
```
**[Critical]** SQL Injection Vulnerability

The query uses string interpolation which is vulnerable to SQL injection attacks.

**Suggestion:**
Use parameterized queries instead.

**Reference:** .NET Security Guidelines - SQL Injection
```

## Error Handling

All tools return standardized error responses:

```json
{
  "success": false,
  "error": {
    "code": "PR_NOT_FOUND",
    "message": "Pull request not found or access denied"
  }
}
```

**Error Codes:**
- `PR_NOT_FOUND` - PR doesn't exist or no access
- `AUTH_FAILED` - Authentication failed
- `RATE_LIMITED` - Too many requests
- `FILE_NOT_FOUND` - File doesn't exist in PR
- `COMMENT_FAILED` - Failed to post comment
- `INVALID_INPUT` - Invalid input parameters
- `UNKNOWN_ERROR` - Unexpected error

## Manifest Structure

The `manifest.json` file contains comprehensive metadata about the PR:

```json
{
  "prUrl": "https://dev.azure.com/org/project/_git/repo/pullrequest/123",
  "prId": 123,
  "prTitle": "Add user authentication",
  "prDescription": "Implements JWT-based auth...",
  "prAuthor": {
    "displayName": "John Doe",
    "email": "john.doe@example.com"
  },
  "prStatus": "active",
  "sourceBranch": "feature/auth",
  "targetBranch": "main",
  "createdDate": "2026-01-20T10:30:00Z",
  "fetchTimestamp": "2026-01-22T08:40:00Z",
  "statistics": {
    "totalFiles": 15,
    "totalSizeBytes": 52000,
    "changes": {
      "added": 3,
      "modified": 10,
      "deleted": 2,
      "renamed": 0
    }
  },
  "files": [
    {
      "originalPath": "src/services/UserService.cs",
      "escapedName": "src~~~services~~~UserService.cs",
      "diffName": "src~~~services~~~UserService.cs.diff",
      "changeType": "modified",
      "sizeBytes": 4523,
      "diffSizeBytes": 892,
      "linesAdded": 45,
      "linesDeleted": 12
    }
  ]
}
```

## Architecture

The project follows a clean architecture with the following structure:

```
AdoPrHelperMcp/
├── Models/              # Data models and DTOs
│   ├── AuthOptions.cs
│   ├── PrInfo.cs
│   ├── PullRequest.cs
│   ├── GitModels.cs
│   ├── PrCommentModels.cs
│   ├── ManifestModels.cs
│   └── ErrorModels.cs
├── Auth/                # Authentication services
│   └── Authenticator.cs
├── Services/            # Business logic
│   ├── AzureDevOpsHelper.cs
│   └── PathUtils.cs
├── Mcp/                 # MCP server configuration
│   └── McpTools.cs
└── Program.cs           # Application entry point
```

## Project Structure

- **Models**: Record types for Azure DevOps API models and MCP responses
- **Auth**: OAuth and PAT authentication implementations using MSAL
- **Services**: Azure DevOps REST API integration and utility functions
- **Mcp**: MCP server tool definitions and handlers

## Dependencies

- `ModelContextProtocol` (0.4.0-preview.3): MCP SDK for .NET
- `Microsoft.Identity.Client` (4.78.0): MSAL for OAuth authentication
- `DiffPlex` (1.9.0): Unified diff generation
- `System.CommandLine` (2.0.0-rc): Command-line argument parsing

## Development

### Running Tests

```bash
dotnet test
```

### Building for Production

```bash
dotnet publish -c Release -r win-x64 --self-contained
dotnet publish -c Release -r linux-x64 --self-contained
dotnet publish -c Release -r osx-x64 --self-contained
```

## License

MIT

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## Troubleshooting

### Authentication Issues

- **PAT**: Ensure `AZURE_DEVOPS_PAT` environment variable is set correctly
- **OAuth**: Check that browser can open and you can authenticate

### Connection Issues

- Verify Azure DevOps URL format is correct
- Check network connectivity to Azure DevOps
- Ensure PAT has required permissions

### File System Issues

- Ensure the `output_folder` path is writable
- Check disk space before fetching large PRs
- Path escaping uses `~~~` to avoid file system conflicts

## Support

For issues and questions:
- Create an issue on the GitHub repository
- Check Azure DevOps API documentation: https://learn.microsoft.com/en-us/rest/api/azure/devops/

---

Converted from TypeScript MCP server at: https://github.com/cuteribs/adopr-helper-mcp
