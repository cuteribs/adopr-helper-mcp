# Azure DevOps PR Helper for AI Assistants

[![npm version](https://badge.fury.io/js/@cuteribs%2Fadopr-helper-mcp.svg)](https://www.npmjs.com/package/@cuteribs/adopr-helper-mcp)
[![npm downloads](https://img.shields.io/npm/dm/@cuteribs/adopr-helper-mcp.svg)](https://www.npmjs.com/package/@cuteribs/adopr-helper-mcp)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

Give your AI assistant (like GitHub Copilot or Claude) the ability to analyze and review Azure DevOps pull requests! This extension lets AI help you with code reviews by reading PR changes and posting intelligent comments.

## What Can This Do?

Once set up, you can ask your AI assistant to:

- 📖 **Review a pull request** - "Review this PR: https://dev.azure.com/myorg/myproject/_git/myrepo/pullrequest/123"
- 🔍 **Analyze code changes** - "What files changed in PR #456?"
- � **Suggest improvements** - "Find potential issues in this PR and comment on them"
- 🤖 **Automate code reviews** - Let AI be your first reviewer before human review

## Quick Setup

### What You Need

- Node.js 18 or newer ([download here](https://nodejs.org/))
- An Azure DevOps account
- One of these AI assistants:
  - GitHub Copilot in VS Code
  - Claude Desktop
  - Any other MCP-compatible AI client

### Step 1: Get Your Azure DevOps Access Token

1. Go to your Azure DevOps organization
2. Click your profile icon → **Personal access tokens**
3. Click **New Token**
4. Give it a name (e.g., "AI PR Helper")
5. Select these permissions:
   - **Code (Read)** - to read PR changes
   - **Code (Status)** - to post comments
6. Click **Create** and **copy the token** (you won't see it again!)

### Step 2: Configure Your AI Assistant

#### Option A: For GitHub Copilot in VS Code

1. Install the [GitHub Copilot Chat MCP extension](https://marketplace.visualstudio.com/items?itemName=GitHub.copilot-mcp)

2. Open VS Code Settings (File → Preferences → Settings)

3. Search for "mcp.servers"

4. Click "Edit in settings.json"

5. Add this configuration (replace `your-token-here` with your actual token from Step 1):

```json
{
  "mcp.servers": {
    "adopr-helper": {
      "command": "npx",
      "args": [
        "@cuteribs/adopr-helper-mcp",
        "--authentication",
        "pat"
      ],
      "env": {
        "AZURE_DEVOPS_PAT": "your-token-here"
      }
    }
  }
}
```

6. Restart VS Code

7. Ask Copilot: "Can you see the Azure DevOps PR Helper tools?" to verify it's working

#### Option B: For Claude Desktop

1. Find your Claude config file:
   - **Windows**: `%APPDATA%\Claude\claude_desktop_config.json`
   - **Mac**: `~/Library/Application Support/Claude/claude_desktop_config.json`

2. Open it in a text editor and add this configuration (replace `your-token-here` with your actual token):

```json
{
  "mcpServers": {
    "adopr-helper": {
      "command": "npx",
      "args": [
        "@cuteribs/adopr-helper-mcp",
        "--authentication",
        "pat"
      ],
      "env": {
        "AZURE_DEVOPS_PAT": "your-token-here"
      }
    }
  }
}
```

3. Restart Claude Desktop

4. Ask Claude: "What tools do you have available?" to verify it's working

### Step 3: Start Using It!

Now you can ask your AI assistant to help with pull requests:

**Example prompts:**

- "Review this PR and tell me if there are any issues: https://dev.azure.com/myorg/myproject/_git/myrepo/pullrequest/123"

- "Look at PR #456 and suggest code improvements"

- "Analyze the changes in this PR and post comments on any potential bugs"

- "What files were changed in this PR?"

- "Check this PR for security issues and code quality problems"

## How to Use

### Reviewing a Pull Request

Just paste the PR URL and ask your AI to review it:

```
Review this PR: https://dev.azure.com/myorg/myproject/_git/myrepo/pullrequest/123
```

The AI will:
1. Fetch all code changes
2. Analyze the diff for each file
3. Provide feedback on code quality, potential bugs, and improvements

### Posting Comments

You can ask the AI to post comments directly to the PR:

```
Look at PR #456 and if you find any issues, comment on them in the PR
```

The AI will automatically comment on specific lines where it finds problems.

### Analyzing Changes

Get a summary of what changed:

```
What changes were made in this PR? https://dev.azure.com/myorg/myproject/_git/myrepo/pullrequest/789
```

## Troubleshooting

### "Authentication failed" or "PAT not found"
- Make sure you copied the token correctly in your config
- Check that your token hasn't expired
- Verify the token has `Code (Read)` and `Code (Status)` permissions

### "Cannot access PR" or "PR not found"
- Verify you have access to the Azure DevOps project/repository
- Check that the PR URL is complete and correct
- Make sure the PR exists and hasn't been deleted

### AI says it doesn't have the tools
- Restart your AI assistant (VS Code or Claude Desktop)
- Check your configuration syntax (make sure the JSON is valid)
- Verify Node.js 18+ is installed: run `node --version` in terminal

### Still having issues?
- Check the [GitHub issues](https://github.com/cuteribs/adopr-helper-mcp/issues)
- Look at the [MCP documentation](https://modelcontextprotocol.io/)

## Advanced Options

### Using Interactive OAuth Instead of PAT

If you prefer browser-based login instead of a Personal Access Token:

**For GitHub Copilot:**
```json
{
  "mcp.servers": {
    "adopr-helper": {
      "command": "npx",
      "args": [
        "@cuteribs/adopr-helper-mcp",
        "--authentication",
        "interactive"
      ]
    }
  }
}
```

**For Claude Desktop:**
```json
{
  "mcpServers": {
    "adopr-helper": {
      "command": "npx",
      "args": [
        "@cuteribs/adopr-helper-mcp",
        "--authentication",
        "interactive"
      ]
    }
  }
}
```

You'll be prompted to log in via your browser when the AI assistant starts.

## For Developers

Interested in contributing or building your own MCP server? Check out:

- [Development documentation](https://github.com/cuteribs/adopr-helper-mcp/wiki) for building from source
- [MCP Protocol Documentation](https://modelcontextprotocol.io/) for creating MCP servers
- [Azure DevOps REST API](https://learn.microsoft.com/en-us/rest/api/azure/devops/) reference

## Privacy & Security

- Your Personal Access Token is stored only in your local configuration
- It's never sent anywhere except to Azure DevOps APIs
- All communication is encrypted (HTTPS)
- **Tip**: Create a token with minimal permissions needed (just `Code (Read)` and `Code (Status)`)
- Rotate your tokens regularly for best security

## Need Help?

- 📝 [Report an issue](https://github.com/cuteribs/adopr-helper-mcp/issues)
- 💬 [Ask a question](https://github.com/cuteribs/adopr-helper-mcp/discussions)
- 📖 [Read the docs](https://github.com/cuteribs/adopr-helper-mcp)

## License

MIT - Free to use and modify!

---

Made with ❤️ for developers who want AI-powered code reviews
