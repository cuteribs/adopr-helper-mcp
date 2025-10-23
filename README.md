# Azure DevOps PR Helper MCP Server

A Model Context Protocol (MCP) server that provides tools for interacting with Azure DevOps Pull Requests.

## Features

- **Get PR Changes**: Fetch all file changes in a pull request with diffs
- **Post PR Comments**: Add AI-generated comments to PR threads

## Installation

```bash
npm install
npm run build
```

## Configuration

Set the following environment variables:

- `AZURE_DEVOPS_PAT`: Your Azure DevOps Personal Access Token
- `AZURE_DEVOPS_ORG`: Your Azure DevOps organization name (optional, can be provided per request)

## Usage

### Testing in VS Code

See **VSCODE_TESTING.md** for comprehensive testing guide.

Quick test:
```bash
# In VS Code terminal
export AZURE_DEVOPS_PAT="your-pat-here"
node test-interactive.js
```

### As an MCP Server with VS Code Extension

Install the [GitHub Copilot Chat MCP extension](https://marketplace.visualstudio.com/items?itemName=GitHub.copilot-mcp) and configure in VS Code settings:

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

## Available Tools

### get_pr_changes

Fetches all file changes in a pull request.

**Parameters:**
- `prUrl` (string): The full URL of the Azure DevOps PR
- `pat` (string, optional): Personal Access Token (overrides env var)

**Returns:** Array of file changes with diffs

### post_pr_comment

Posts a comment to a PR thread.

**Parameters:**
- `prUrl` (string): The full URL of the Azure DevOps PR
- `comment` (string): The comment text to post
- `filePath` (string, optional): File path to create thread on
- `threadId` (number, optional): Existing thread ID to reply to
- `pat` (string, optional): Personal Access Token (overrides env var)

**Returns:** Created comment details

## Development

```bash
npm run dev    # Watch mode
npm run build  # Build
npm start      # Run the server
```

**VS Code Tasks:**
- Press `Ctrl+Shift+B` to build
- Press `F5` to debug
- See `.vscode/tasks.json` and `.vscode/launch.json`

## License

MIT
