# Azure DevOps PR Helper - .NET 8 MCP Server

This is a **fully functional** .NET 8 implementation of the `adopr-helper-mcp` MCP server, enhanced with file system storage capabilities for the ai-pr-reviewer skill.

## Current Status: ✅ Production Ready

### ✅ Completed Components

1. **Project Structure**
   - Created .NET 8 console application
   - Added all required NuGet packages:
     - `ModelContextProtocol` (0.4.0-preview.3)
     - `Microsoft.Identity.Client` (4.78.0)
     - `DiffPlex` (1.9.0)
     - `System.CommandLine` (2.0.0-rc.2)

2. **Models** (`Models/` directory)
   - ✅ `AuthOptions.cs` - Authentication configuration
   - ✅ `PrInfo.cs` - PR URL parsing results
   - ✅ `PullRequest.cs` - Azure DevOps PR details with metadata
   - ✅ `GitModels.cs` - Git changes and file items
   - ✅ `PrCommentModels.cs` - PR comment and thread structures
   - ✅ `ManifestModels.cs` - Manifest data structures for file storage
   - ✅ `ErrorModels.cs` - Standardized error responses

3. **Authentication** (`Auth/` directory)
   - ✅ `Authenticator.cs` - Complete implementation with:
     - `IAuthenticator` interface
     - `OAuthAuthenticator` - Interactive OAuth flow using MSAL
     - `PatAuthenticator` - Personal Access Token authentication
     - `AuthenticatorFactory` - Factory for creating authenticators
     - Cross-platform browser launching for OAuth

4. **Azure DevOps Integration** (`Services/` directory)
   - ✅ `AzureDevOpsHelper.cs` - Complete implementation with:
     - PR URL parsing (both dev.azure.com and visualstudio.com formats)
     - PR details fetching with validation and metadata
     - File changes retrieval with filtering
     - Unified diff generation with line statistics
     - **NEW**: File system storage with escaped paths
     - **NEW**: Manifest.json generation
     - PR comment posting with severity support
     - Proper HTTP client management
     - Comprehensive error handling
   - ✅ `PathUtils.cs` - Path escaping utilities

5. **MCP Server Integration** (`Mcp/McpTools.cs` and `Program.cs`)
   - ✅ Tool registration with proper schemas
   - ✅ `azure_devops_fetch_pr_changes` - Fetch PR files and save to disk
   - ✅ `azure_devops_post_comment` - Post review comments with severity
   - ✅ Snake_case parameter naming
   - ✅ Standardized error responses
   - ✅ Stdio transport integration

6. **Documentation**
   - ✅ Comprehensive `README.md` with usage instructions
   - ✅ Tool documentation with examples
   - ✅ Manifest structure documentation
   - ✅ Error code reference
   - ✅ `.gitignore` for .NET projects
   - ✅ XML documentation comments throughout the code

## Key Features

### 🚀 Enhanced for ai-pr-reviewer Skill

This implementation follows the `mcp-integration.md` specification:

1. **File System Storage** - Files are written to disk, not returned in responses
2. **Path Escaping** - Uses `~~~` separator (e.g., `src~~~services~~~UserService.cs`)
3. **Manifest Generation** - Creates comprehensive `manifest.json` with metadata
4. **Small Responses** - Returns summaries only, preventing context overflow
5. **Severity Support** - Comments can have severity levels (Critical, High, Medium, Low)
6. **Standardized Errors** - Consistent error codes and messages

### 📊 Manifest Structure

The manifest.json includes:
- PR metadata (title, author, description, status)
- Branch information (source, target)
- Timestamps (created, fetched)
- Statistics (total files, sizes, change breakdown)
- Per-file metadata (paths, sizes, line counts)

## Architecture

```
AdoPrHelperMcp/
├── Models/              # ✅ Data models and DTOs
│   ├── AuthOptions.cs
│   ├── PrInfo.cs
│   ├── PullRequest.cs
│   ├── GitModels.cs
│   ├── PrCommentModels.cs
│   ├── ManifestModels.cs
│   └── ErrorModels.cs
├── Auth/                # ✅ Authentication services
│   └── Authenticator.cs
├── Services/            # ✅ Business logic
│   ├── AzureDevOpsHelper.cs
│   └── PathUtils.cs
├── Mcp/                 # ✅ MCP server configuration
│   └── McpTools.cs
├── Program.cs           # ✅ Entry point
└── README.md            # ✅ Documentation
```

## Available Tools

### azure_devops_fetch_pr_changes

Fetches all changed files from a PR and saves to local folder.

**Input:**
- `pr_url` (string): Full Azure DevOps PR URL
- `output_folder` (string): Local folder path for output

**Output:**
Small response with summary statistics (NOT file contents)

### azure_devops_post_comment

Posts a review comment to a specific file and line.

**Input:**
- `pr_url` (string): Full Azure DevOps PR URL
- `file_path` (string): File path to comment on
- `line_number` (number): Line number
- `comment_text` (string): Comment content
- `severity` (string, optional): Critical, High, Medium, Low
- `thread_status` (string, optional): active, fixed, wontFix, closed

## Building the Project

```bash
cd AdoPrHelperMcp
dotnet build
dotnet run
```

## Testing

```bash
# Build
dotnet build

# Run with PAT authentication
export AZURE_DEVOPS_PAT="your-pat"
dotnet run -- --authentication pat
```

## Configuration

See README.md for detailed configuration instructions for:
- GitHub Copilot in VS Code
- Claude Desktop
- Other MCP clients

## Recent Changes (2026-01-22)

✅ **MCP Integration for ai-pr-reviewer**
- Renamed tools to match convention (`azure_devops_*`)
- Changed all parameters to snake_case
- Implemented file system storage with path escaping
- Added comprehensive manifest generation
- Added severity support for comments
- Implemented standardized error handling
- Updated all documentation

## Original TypeScript Implementation

Converted from: https://github.com/cuteribs/adopr-helper-mcp

## Contributing

Contributions are welcome! The project is fully functional and ready for use.

## License

MIT (Same as original TypeScript version)
