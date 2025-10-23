# MCP Server Configuration Examples

## Option 1: Testing in VS Code (Recommended)

The easiest way to test your MCP server is directly in VS Code using the provided test scripts.

### Quick Test
```bash
# In VS Code terminal (Ctrl+`)
export AZURE_DEVOPS_PAT="your-pat-here"
node test-interactive.js
```

### Using VS Code Tasks
- Press `Ctrl+Shift+P` → "Tasks: Run Task" → "Test MCP Server"
- Press `F5` to debug

See **VSCODE_TESTING.md** for complete guide.

---

## Option 2: VS Code with GitHub Copilot Chat MCP Extension

If you have GitHub Copilot, you can use the MCP extension:

1. Install: [GitHub Copilot Chat MCP](https://marketplace.visualstudio.com/items?itemName=GitHub.copilot-mcp)
2. Add to VS Code settings (`.vscode/settings.json` or User settings):

```json
{
  "mcp.servers": {
    "adopr-helper": {
      "command": "node",
      "args": ["C:\\git\\dnv\\vscode\\adopr-helper-mcp\\dist\\index.js"],
      "env": {
        "AZURE_DEVOPS_PAT": "your-personal-access-token-here"
      }
    }
  }
}
```

**For macOS/Linux:**
```json
{
  "mcp.servers": {
    "adopr-helper": {
      "command": "node",
      "args": ["/path/to/adopr-helper-mcp/dist/index.js"],
      "env": {
        "AZURE_DEVOPS_PAT": "your-personal-access-token-here"
      }
    }
  }
}
```

---

## Option 3: Claude Desktop (Alternative)

If you want to use this with Claude Desktop:

**Windows:** `%APPDATA%\Claude\claude_desktop_config.json`

```json
{
  "mcpServers": {
    "adopr-helper": {
      "command": "node",
      "args": ["C:\\git\\dnv\\vscode\\adopr-helper-mcp\\dist\\index.js"],
      "env": {
        "AZURE_DEVOPS_PAT": "your-personal-access-token-here"
      }
    }
  }
}
```

**macOS/Linux:** `~/Library/Application Support/Claude/claude_desktop_config.json`

```json
{
  "mcpServers": {
    "adopr-helper": {
      "command": "node",
      "args": ["/path/to/adopr-helper-mcp/dist/index.js"],
      "env": {
        "AZURE_DEVOPS_PAT": "your-personal-access-token-here"
      }
    }
  }
}
```

## Getting a Personal Access Token (PAT)

1. Go to Azure DevOps
2. Click on your profile icon (top right)
3. Select "Personal access tokens"
4. Click "New Token"
5. Set scopes:
   - **Code**: Read & Write (for reading PR changes and posting comments)
   - **Pull Request Threads**: Read & Write
6. Copy the generated token and use it in your config

## Testing the Server

### Interactive Test (Easiest)
```bash
cd c:/git/dnv/vscode/adopr-helper-mcp
export AZURE_DEVOPS_PAT="your-pat-here"
node test-interactive.js
```

### Direct Server Test
```bash
cd c:/git/dnv/vscode/adopr-helper-mcp
npm run build
export AZURE_DEVOPS_PAT="your-pat-here"
node dist/index.js
```
The server will wait for stdin input (MCP protocol messages).

### VS Code Debugging
- Press `F5` in VS Code
- Choose "Debug MCP Server" or "Debug Test Script"
- Set breakpoints and step through code

See **VSCODE_TESTING.md** for comprehensive testing options.
