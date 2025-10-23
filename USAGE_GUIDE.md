# Azure DevOps PR Helper MCP Server - Step-by-Step Guide

## Overview

This MCP server provides two main tools:
1. **get_pr_changes** - Fetch all file changes in a PR with diffs
2. **post_pr_comment** - Post AI-generated comments to PR threads

## Step-by-Step Setup

### Step 1: Build the Project

In VS Code terminal (`Ctrl+\``):
```bash
cd c:/git/dnv/vscode/adopr-helper-mcp
npm install
npm run build
```

Or press `Ctrl+Shift+B` to build.

### Step 2: Get Azure DevOps Personal Access Token (PAT)

1. Navigate to Azure DevOps: https://dev.azure.com
2. Click your profile icon (top right) → **Personal access tokens**
3. Click **+ New Token**
4. Configure the token:
   - **Name**: "MCP Server Access"
   - **Organization**: Select your organization
   - **Expiration**: Choose duration
   - **Scopes**: Select **Custom defined**
     - ✓ **Code** → Read & Write
     - ✓ **Pull Request Threads** → Read & Write
5. Click **Create**
6. **Copy the token immediately** (you won't be able to see it again!)

### Step 3: Test in VS Code

In VS Code terminal:
```bash
# Set your PAT
export AZURE_DEVOPS_PAT="your-pat-here"

# Run interactive test
node test-interactive.js
```

See **VSCODE_TESTING.md** for comprehensive VS Code testing options.

### Step 4: Configure for Production Use (Optional)

**Option A: GitHub Copilot Chat MCP Extension**

If you have GitHub Copilot, install the MCP extension and add to VS Code settings:

```json
{
  "mcp.servers": {
    "adopr-helper": {
      "command": "node",
      "args": ["C:\\git\\dnv\\vscode\\adopr-helper-mcp\\dist\\index.js"],
      "env": {
        "AZURE_DEVOPS_PAT": "your-pat-here"
      }
    }
  }
}
```

**Option B: Claude Desktop**

Configure `%APPDATA%\Claude\claude_desktop_config.json` (see CONFIG_EXAMPLE.md)

### Step 5: Verify It Works

Run the interactive test and test with a real PR:
```bash
export AZURE_DEVOPS_PAT="your-pat"
node test-interactive.js
# Choose option 1, enter a PR URL
```

## Usage Examples

### Example 1: Get All PR Changes

**Using the test script:**
```bash
export AZURE_DEVOPS_PAT="your-pat"
node test-interactive.js
# Choose option 1
# Enter: https://dev.azure.com/myorg/myproject/_git/myrepo/pullrequest/123
```

**Or with an AI assistant (GitHub Copilot/Claude):**
```
Can you get all the file changes from this PR?
https://dev.azure.com/myorg/myproject/_git/myrepo/pullrequest/123
```

**What happens:**
- MCP server connects to Azure DevOps
- Fetches PR details and commits
- Retrieves file contents from source and target branches
- Generates unified diffs for each changed file
- Returns structured JSON with all changes

**Response format:**
```json
{
  "success": true,
  "changesCount": 5,
  "changes": [
    {
      "path": "/src/index.ts",
      "changeType": "Edit",
      "diff": "--- /src/index.ts\n+++ /src/index.ts\n@@ -1,4 +1,5 @@\n..."
    }
  ]
}
```

### Example 2: Review Code and Post Comments

**Using the test script:**
```bash
node test-interactive.js
# Choose option 2
# Enter PR URL
# Enter your review comment
```

**Or with an AI assistant (GitHub Copilot/Claude):**
```
Please review the code changes in this PR and post a comment with your findings:
https://dev.azure.com/myorg/myproject/_git/myrepo/pullrequest/123

Focus on:
- Code quality issues
- Potential bugs
- Performance concerns
```

**What happens:**
1. AI uses `get_pr_changes` to fetch all diffs
2. Analyzes the code changes
3. Generates review comments
4. Uses `post_pr_comment` to post the comments to the PR

### Example 3: Post Comment on Specific File

**With an AI assistant:**
```
Post a comment on line 42 of src/utils.ts in this PR:
https://dev.azure.com/myorg/myproject/_git/myrepo/pullrequest/123

Comment: "Consider using async/await instead of promises for better readability"
```

**MCP Call:**
```json
{
  "prUrl": "https://dev.azure.com/myorg/myproject/_git/myrepo/pullrequest/123",
  "comment": "Consider using async/await instead of promises for better readability",
  "filePath": "/src/utils.ts",
  "lineNumber": 42
}
```

### Example 4: Reply to Existing Thread

**Prompt to Claude:**
```
Reply to thread #12345 in this PR with: "Fixed in the latest commit"
https://dev.azure.com/myorg/myproject/_git/myrepo/pullrequest/123
```

**MCP Call:**
```json
{
  "prUrl": "https://dev.azure.com/myorg/myproject/_git/myrepo/pullrequest/123",
  "comment": "Fixed in the latest commit",
  "threadId": 12345
}
```

## Advanced Workflows

### Automated Code Review Workflow

1. **Get changes**: Fetch all file diffs using the test script or AI
2. **Analyze**: AI examines the code (or manual review)
3. **Generate feedback**: Create detailed review comments
4. **Post comments**: Submit comments to specific files/lines

### Security Audit Workflow

**With AI assistant:**
```
Analyze this PR for security vulnerabilities and post a summary comment:
https://dev.azure.com/myorg/myproject/_git/myrepo/pullrequest/123

Check for:
- SQL injection risks
- XSS vulnerabilities
- Authentication issues
- Sensitive data exposure
```

### Documentation Check Workflow

```
Review this PR and check if:
1. All new functions have JSDoc comments
2. README is updated if needed
3. CHANGELOG includes this change

Post a comment with your findings.
```

## Troubleshooting

### Error: "No PAT provided"

**Solutions**:
- **In VS Code terminal**: Set environment variable before running
  ```bash
  export AZURE_DEVOPS_PAT="your-pat"  # bash
  $env:AZURE_DEVOPS_PAT="your-pat"    # PowerShell
  ```
- **For MCP clients**: Ensure PAT is in config file (see CONFIG_EXAMPLE.md)

### Error: "Invalid PR URL format"

**Solution**: Use full PR URL format:
- ✓ `https://dev.azure.com/org/project/_git/repo/pullrequest/123`
- ✗ `dev.azure.com/org/project/...` (missing https://)

### Error: "HTTP 401: Unauthorized"

**Solutions**:
1. Verify your PAT is correct
2. Check PAT hasn't expired
3. Ensure PAT has required scopes (Code: Read/Write, PR Threads: Read/Write)

### Error: "Pull request not found"

**Solutions**:
1. Verify PR ID is correct
2. Ensure you have access to the repository
3. Check if PR has been completed/abandoned

### Module Not Found Errors

**Solutions**:
1. Run `npm install` to install dependencies
2. Run `npm run build` to compile TypeScript
3. Check that `dist/` folder exists with .js files

### Server Not Working with MCP Client

**Solutions**:
1. Test with `test-interactive.js` first to verify server works
2. Check config file syntax is valid JSON
3. Verify absolute paths in config
4. For Copilot: Reload VS Code window
5. For Claude: Restart Claude Desktop and check logs

## Best Practices

### 1. Start with Testing
- Test with `test-interactive.js` before using with AI
- Verify server works with small PRs first
- Use VS Code debugger (`F5`) when developing

### 2. Use Specific Prompts (when using AI)
- ✓ "Review src/auth.ts for security issues"
- ✗ "Review this PR"

### 3. Batch Operations
Instead of multiple small comments:
- Generate a comprehensive review
- Post a summary comment
- Group related issues

### 4. Comment Etiquette
- Be constructive and specific
- Include file paths and line numbers
- Provide code suggestions when possible

### 5. Security
- Never commit your PAT to version control
- Use environment variables or secure config
- Rotate PATs regularly
- Use minimum required scopes

## API Reference

### get_pr_changes

**Parameters:**
- `prUrl` (string, required): Full Azure DevOps PR URL
- `pat` (string, optional): Personal Access Token (overrides env var)

**Returns:**
```typescript
{
  success: boolean;
  changesCount: number;
  changes: Array<{
    path: string;
    changeType: "Add" | "Edit" | "Delete" | "Rename";
    diff: string; // Unified diff format
  }>;
}
```

### post_pr_comment

**Parameters:**
- `prUrl` (string, required): Full Azure DevOps PR URL
- `comment` (string, required): Comment text (supports Markdown)
- `filePath` (string, optional): File path to attach comment
- `lineNumber` (number, optional): Line number in file
- `threadId` (number, optional): Existing thread ID to reply to
- `pat` (string, optional): Personal Access Token (overrides env var)

**Returns:**
```typescript
{
  success: boolean;
  commentId: number;
  content: string;
}
```

## Next Steps

1. ✅ **Test in VS Code**: Run `test-interactive.js` with a real PR
2. � **Debug**: Use F5 to debug and understand the code
3. 📝 **Customize**: Modify for your team's workflow
4. 🤖 **Integrate**: Connect with Copilot or Claude
5. 📊 **Monitor**: Track usage and iterate

## Support

For issues or questions:
1. **Check VSCODE_TESTING.md** for VS Code-specific help
2. Check troubleshooting section above
3. Review Azure DevOps API documentation
4. Check MCP SDK documentation
5. Use VS Code debugger to inspect issues
