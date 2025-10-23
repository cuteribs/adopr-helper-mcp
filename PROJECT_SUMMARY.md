# Project Summary: Azure DevOps PR Helper MCP Server

## What We Built

A Model Context Protocol (MCP) server that enables AI assistants (like Claude) to interact with Azure DevOps Pull Requests. The server provides two main capabilities:

1. **Fetch PR Changes**: Get all file changes with diffs from any Azure DevOps PR
2. **Post Comments**: Add AI-generated review comments to PR threads

## Project Structure

```
adopr-helper-mcp/
├── src/
│   ├── index.ts           # Main server entry point
│   ├── tools.ts           # MCP tool definitions
│   └── azureDevOps.ts     # Azure DevOps API integration
├── dist/                  # Compiled JavaScript (generated)
├── package.json           # Dependencies and scripts
├── tsconfig.json          # TypeScript configuration
├── README.md             # Project overview
├── USAGE_GUIDE.md        # Comprehensive usage instructions
├── CONFIG_EXAMPLE.md     # Configuration examples
├── test-ado.js           # Test script
└── .gitignore            # Git ignore rules
```

## Key Technologies

- **TypeScript**: Type-safe development
- **MCP SDK**: Model Context Protocol integration
- **Azure DevOps Node API**: Official Azure DevOps REST API client
- **diff**: Unified diff generation
- **zod**: Runtime type validation

## Features Implemented

### 1. PR Change Retrieval
- Parses Azure DevOps PR URLs (both dev.azure.com and visualstudio.com formats)
- Authenticates using Personal Access Token
- Fetches all commits in the PR
- Generates unified diffs for each changed file
- Filters out binary files automatically
- Returns structured JSON with file paths, change types, and diffs

### 2. Comment Posting
- Posts comments to PR threads
- Supports creating new threads
- Supports replying to existing threads
- Can attach comments to specific files
- Can attach comments to specific line numbers
- Returns comment ID and content confirmation

## How It Works

### Architecture

```
┌─────────────────┐
│   VS Code       │  (Testing with test-interactive.js)
│   OR            │  OR
│   AI Assistant  │  (GitHub Copilot/Claude Desktop)
│   (MCP Client)  │
└────────┬────────┘
         │ MCP Protocol (stdio)
         │
┌────────▼────────┐
│   MCP Server    │  (adopr-helper-mcp)
│                 │
│  ├─ index.ts    │  Initializes server
│  ├─ tools.ts    │  Registers tools
│  └─ azureDevOps │  API integration
└────────┬────────┘
         │ HTTPS REST API
         │
┌────────▼────────┐
│  Azure DevOps   │
│                 │
│  ├─ Projects    │
│  ├─ Repos       │
│  └─ Pull Req.   │
└─────────────────┘
```

### Data Flow

**Getting PR Changes:**
1. Client (test script or AI) sends request with PR URL
2. MCP server parses URL → extracts org/project/repo/PR ID
3. Server authenticates with Azure DevOps using PAT
4. Fetches PR details and commit list
5. For each commit, retrieves file changes
6. Generates diffs by comparing old/new file contents
7. Returns structured data to client

**Posting Comments:**
1. Client sends request with PR URL, comment text, optional file/line/thread
2. MCP server parses URL and authenticates
3. If threadId provided → replies to existing thread
4. If filePath provided → creates new thread on that file
5. Otherwise → creates general PR comment
6. Returns confirmation with comment ID

## Quick Start

### 1. Build
In VS Code terminal:
```bash
cd c:/git/dnv/vscode/adopr-helper-mcp
npm install
npm run build
```
Or press `Ctrl+Shift+B` in VS Code.

### 2. Get Azure DevOps PAT
- Go to Azure DevOps → Personal Access Tokens
- Create token with: Code (Read & Write), PR Threads (Read & Write)

### 3. Test in VS Code
```bash
# In VS Code terminal
export AZURE_DEVOPS_PAT="your-pat-here"
node test-interactive.js
```
Choose option 1, enter a PR URL, and see the results!

### 4. Configure for Production (Optional)
**GitHub Copilot Chat MCP**: Add to VS Code settings (see CONFIG_EXAMPLE.md)

**Claude Desktop**: Add to `%APPDATA%\Claude\claude_desktop_config.json`

### 5. Use with AI!
```
Can you review this PR and post feedback?
https://dev.azure.com/myorg/myproject/_git/myrepo/pullrequest/123
```

## Example Workflows

### Testing in VS Code
```bash
# Interactive test
export AZURE_DEVOPS_PAT="your-pat"
node test-interactive.js
```

### Code Review (with AI)
```
Review this PR for:
- Code quality issues
- Potential bugs
- Best practices
Then post a summary comment with your findings.
```

### Security Audit (with AI)
```
Analyze this PR for security vulnerabilities:
- SQL injection
- XSS risks
- Authentication issues
Post a comment if you find any concerns.
```

### Documentation Check (with AI)
```
Check if this PR includes:
- JSDoc comments for new functions
- Updated README
- CHANGELOG entry
Post a reminder if anything is missing.
```

## Development Commands

```bash
npm run build      # Compile TypeScript
npm run dev        # Watch mode (recompile on changes)
npm start          # Run the server
npm run lint       # Check code style
npm run lint:fix   # Fix code style issues
npm run clean      # Remove dist folder
```

**VS Code shortcuts:**
- `Ctrl+Shift+B` - Build
- `F5` - Debug
- `Ctrl+Shift+P` → "Tasks: Run Task" - Run tasks

## Testing

### Interactive Test (Recommended)
```bash
# In VS Code terminal
export AZURE_DEVOPS_PAT=your-pat-here
node test-interactive.js
```

### Simple Test
```bash
# Edit test-ado.js with your PR URL first
export AZURE_DEVOPS_PAT=your-pat-here
node test-ado.js
```

### Debug in VS Code
- Press `F5`
- Choose "Debug Test Script"
- Set breakpoints and step through code

See **VSCODE_TESTING.md** for comprehensive testing guide.

## Security Considerations

1. **PAT Security**: 
   - Never commit PAT to git
   - Use environment variables
   - Rotate regularly
   - Use minimum required scopes

2. **Input Validation**:
   - All inputs validated with Zod schemas
   - PR URLs parsed and validated
   - Error handling for all API calls

3. **Binary File Filtering**:
   - Automatically skips binary files
   - Prevents large content downloads
   - Improves performance

## Limitations

1. **Large PRs**: Very large PRs (100+ files) may be slow
2. **Binary Files**: Cannot diff binary files (intentionally filtered)
3. **Line Numbers**: Line number comments only work for unchanged lines
4. **Rate Limiting**: Subject to Azure DevOps API rate limits

## Future Enhancements

Potential improvements:
- [ ] Cache PR data for repeated queries
- [ ] Support for PR approval/rejection
- [ ] Work item integration
- [ ] Build status checking
- [ ] Support for draft comments
- [ ] Batch comment posting
- [ ] Comment templates
- [ ] Metrics and analytics

## Files Created

1. **package.json** - Dependencies and scripts
2. **tsconfig.json** - TypeScript configuration
3. **src/index.ts** - Server entry point
4. **src/tools.ts** - MCP tool definitions
5. **src/azureDevOps.ts** - Azure DevOps API functions
6. **README.md** - Project overview
7. **USAGE_GUIDE.md** - Comprehensive usage guide (this file)
8. **CONFIG_EXAMPLE.md** - Configuration examples
9. **test-ado.js** - Test script
10. **.gitignore** - Git ignore rules

## Troubleshooting

See USAGE_GUIDE.md for detailed troubleshooting steps.

Common issues:
- PAT not set → Check environment variable
- Invalid URL → Use full https:// URL
- 401 error → Verify PAT scopes
- Server not appearing → Check config file syntax

## Resources

- [MCP Documentation](https://modelcontextprotocol.io/)
- [Azure DevOps REST API](https://docs.microsoft.com/en-us/rest/api/azure/devops/)
- [azure-devops-node-api](https://github.com/microsoft/azure-devops-node-api)

## Success! 🎉

Your MCP server is ready to use. Start by:
1. Testing with a simple PR
2. Trying the example workflows
3. Creating custom review prompts
4. Integrating into your development workflow

Enjoy AI-powered PR reviews!
