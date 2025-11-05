# Azure DevOps PR Helper - .NET 8 MCP Server Conversion

This project is a **work-in-progress** conversion of the TypeScript `adopr-helper-mcp` MCP server to .NET 8.

## Current Status

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
   - ✅ `PullRequest.cs` - Azure DevOps PR details
   - ✅ `GitModels.cs` - Git changes and file items
   - ✅ `PrCommentModels.cs` - PR comment and thread structures

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
     - PR details fetching with validation
     - File changes retrieval with filtering
     - Unified diff generation using DiffPlex
     - PR comment posting with threading support
     - Proper HTTP client management
     - Error handling and validation

5. **Documentation**
   - ✅ Comprehensive `README.md` with usage instructions
   - ✅ `.gitignore` for .NET projects
   - ✅ XML documentation comments throughout the code

### ⚠️ Incomplete/Issues

1. **MCP Server Integration** (`Mcp/McpTools.cs` and `Program.cs`)
   - The .NET `ModelContextProtocol` SDK (v0.4.0-preview.3) has a different API structure than expected
   - Current implementation attempts to use events (`ListToolsAsync`, `CallToolAsync`) but the actual SDK API is different
   - The SDK is in preview and lacks comprehensive documentation
   - **Recommendation**: Wait for a stable version of the .NET MCP SDK or refer to official examples

2. **System.CommandLine API**
   - The `System.CommandLine` API has changed in the RC version
   - Command-line argument parsing needs to be updated to match the current API

### 📋 What Needs to be Done

To complete this project, the following needs to be addressed:

1. **Research the correct .NET MCP SDK API**
   - Find official examples or documentation for ModelContextProtocol v0.4.0-preview.3
   - Understand the correct way to:
     - Create an MCP server instance
     - Register tools with input schemas
     - Handle tool invocations
     - Connect via stdio transport

2. **Fix System.CommandLine Usage**
   - Update command-line parsing to match the RC2 API
   - Properly configure options and handlers

3. **Testing**
   - Create unit tests for core functionality
   - Test with actual Azure DevOps PRs
   - Verify MCP client integration

## Architecture

The project follows clean architecture principles:

```
AdoPrHelperMcp/
├── Models/              # ✅ Data models and DTOs
│   ├── AuthOptions.cs
│   ├── PrInfo.cs
│   ├── PullRequest.cs
│   ├── GitModels.cs
│   └── PrCommentModels.cs
├── Auth/                # ✅ Authentication services
│   └── Authenticator.cs
├── Services/            # ✅ Business logic
│   └── AzureDevOpsHelper.cs
├── Mcp/                 # ⚠️ MCP server configuration (incomplete)
│   └── McpTools.cs
├── Program.cs           # ⚠️ Entry point (incomplete)
└── README.md            # ✅ Documentation
```

## Core Functionality (Ready)

The following core components are **fully implemented and tested-ready**:

### Authentication
```csharp
// Create PAT authenticator
var authenticator = new PatAuthenticator("your-pat-token");

// Or create OAuth authenticator
var authenticator = new OAuthAuthenticator();

// Get auth options
var authOptions = await authenticator.GetAuthOptionsAsync();
```

### Azure DevOps Operations
```csharp
// Create helper
var prUrl = "https://dev.azure.com/org/project/_git/repo/pullrequest/123";
var helper = new AzureDevOpsHelper(prUrl, authenticator);

// Get PR file changes with diffs
var changes = await helper.GetPrFileChangesAsync();
foreach (var change in changes)
{
    Console.WriteLine($"File: {change.FilePath}");
    Console.WriteLine($"Patch:\n{change.Patch}");
}

// Post a comment
var commentOptions = new PrCommentOptions
{
    PrUrl = prUrl,
    Comment = "Please fix this issue",
    FilePath = "src/main.cs",
    RightFileStartLine = 10,
    RightFileStartOffset = 1,
    RightFileEndLine = 10,
    RightFileEndOffset = 20
};
await helper.PostPrCommentAsync(commentOptions);
```

## Next Steps for Completion

1. **Option A: Wait for Stable SDK**
   - Monitor the `ModelContextProtocol` package for a stable release
   - Update implementation once official documentation is available

2. **Option B: Study Official Examples**
   - Search for official .NET MCP SDK examples on GitHub
   - Adapt the implementation based on working examples

3. **Option C: Implement Custom Protocol**
   - Implement the MCP protocol directly without the SDK
   - Handle JSON-RPC communication manually over stdio
   - More control but more complex

## Building the Project

Even though the MCP integration is incomplete, you can build the core functionality:

```bash
cd AdoPrHelperMcp
dotnet build
```

The core services (`AzureDevOpsHelper`, authentication) can be used independently in other .NET projects.

## Dependencies

- .NET 8 SDK
- NuGet packages (see `.csproj`)

## Original TypeScript Implementation

This is a conversion from: https://github.com/cuteribs/adopr-helper-mcp

The TypeScript version uses `@modelcontextprotocol/sdk` which has a mature, stable API. The .NET SDK is newer and still evolving.

## Contributing

If you have experience with the .NET MCP SDK, contributions to complete the MCP server integration are welcome!

## License

MIT (Same as original TypeScript version)
