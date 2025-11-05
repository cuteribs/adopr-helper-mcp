# Azure DevOps PR Helper MCP Server (.NET 8)

A Model Context Protocol (MCP) server for Azure DevOps Pull Request operations, implemented in .NET 8.

## Features

- **Fetch PR Changes**: Get all file changes in a pull request with unified diffs
- **Post Comments**: Post comments to PR threads at specific file locations
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

### get_pr_changes

Fetches all file changes in an Azure DevOps pull request with unified diffs.

**Parameters:**
- `prUrl` (string, required): The full URL of the Azure DevOps pull request

**Example:**
```
Get changes from https://dev.azure.com/myorg/myproject/_git/myrepo/pullrequest/123
```

### post_pr_comment

Posts a comment to an Azure DevOps pull request thread.

**Parameters:**
- `prUrl` (string, required): The full URL of the Azure DevOps pull request
- `comment` (string, required): The comment text to post
- `filePath` (string, required): File path to attach comment to
- `rightFileStartLine` (number, required): Start line number
- `rightFileStartOffset` (number, required): Offset in start line
- `rightFileEndLine` (number, required): End line number
- `rightFileEndOffset` (number, required): Offset in end line

**Example:**
```
Post a comment "Fix this issue" to PR 123 at line 10 of file src/main.cs
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
│   └── PrCommentModels.cs
├── Auth/                # Authentication services
│   └── Authenticator.cs
├── Services/            # Business logic
│   └── AzureDevOpsHelper.cs
├── Mcp/                 # MCP server configuration
│   └── McpTools.cs
└── Program.cs           # Application entry point
```

## Project Structure

- **Models**: Record types for Azure DevOps API models
- **Auth**: OAuth and PAT authentication implementations using MSAL
- **Services**: Azure DevOps REST API integration
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

## Support

For issues and questions:
- Create an issue on the GitHub repository
- Check Azure DevOps API documentation: https://learn.microsoft.com/en-us/rest/api/azure/devops/

---

Converted from TypeScript MCP server at: https://github.com/cuteribs/adopr-helper-mcp
