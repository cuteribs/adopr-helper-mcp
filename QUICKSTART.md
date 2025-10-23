# Quick Start Checklist - VS Code

Follow these steps to get your Azure DevOps PR Helper MCP Server running in VS Code:

## ✅ Setup Checklist

### Phase 1: Build the Server
- [ ] Open project in VS Code: `c:/git/dnv/vscode/adopr-helper-mcp`
- [ ] Install dependencies: `npm install` (or use VS Code terminal)
- [ ] Build the project: `npm run build` (or press `Ctrl+Shift+B`)
- [ ] Verify `dist` folder was created with .js files

### Phase 2: Get Azure DevOps PAT
- [ ] Go to https://dev.azure.com
- [ ] Click profile icon → "Personal access tokens"
- [ ] Click "+ New Token"
- [ ] Set name: "MCP Server Access"
- [ ] Select scopes:
  - [ ] Code: Read & Write
  - [ ] Pull Request Threads: Read & Write
- [ ] Click "Create" and **copy the token immediately**

### Phase 3: Test in VS Code
- [ ] Open VS Code terminal (`Ctrl+\``)
- [ ] Set your PAT:
  ```bash
  export AZURE_DEVOPS_PAT="your-pat-here"  # Bash
  # OR
  $env:AZURE_DEVOPS_PAT="your-pat-here"    # PowerShell
  ```
- [ ] Run interactive test: `node test-interactive.js`
- [ ] Follow the menu prompts
- [ ] Test getting PR changes with a real PR URL

### Phase 4: Configure for Production Use (Optional)
Choose one:

**Option A: GitHub Copilot Chat MCP (if you have Copilot)**
- [ ] Install extension: GitHub Copilot Chat MCP
- [ ] Add to VS Code settings (see CONFIG_EXAMPLE.md)
- [ ] Reload VS Code
- [ ] Use in Copilot Chat

**Option B: Claude Desktop**
- [ ] Configure `%APPDATA%\Claude\claude_desktop_config.json`
- [ ] Restart Claude Desktop
- [ ] Test with Claude

### Phase 5: Verify It Works!
- [ ] Find a test PR in Azure DevOps
- [ ] Copy the PR URL
- [ ] In `test-interactive.js`, choose option 1
- [ ] Paste the PR URL
- [ ] Verify you see file changes and diffs

## 🎯 First Test Example

In VS Code terminal:

```bash
# Set your PAT
export AZURE_DEVOPS_PAT="your-pat-here"

# Run interactive test
node test-interactive.js

# Choose option 1: Get PR changes
# Enter your PR URL:
# https://dev.azure.com/yourorg/yourproject/_git/yourrepo/pullrequest/123
```

Expected behavior:
1. Script connects to Azure DevOps
2. You'll see a list of changed files
3. Each file will show its change type (Add/Edit/Delete)
4. You can view diffs interactively

## 🚨 Troubleshooting

If something doesn't work:

### Module not found errors?
1. Run `npm run build` first
2. Check that `dist/` folder exists
3. Verify all dependencies installed: `npm install`

### "No PAT provided" error?
1. Set in terminal: `export AZURE_DEVOPS_PAT="your-pat"`
2. Verify it's set: `echo $AZURE_DEVOPS_PAT` (bash) or `echo %AZURE_DEVOPS_PAT%` (cmd)
3. Run test in same terminal session

### "Invalid PR URL" error?
1. Use full URL: `https://dev.azure.com/org/project/_git/repo/pullrequest/123`
2. Include `https://`
3. Check URL is accessible in browser

### "401 Unauthorized" error?
1. Verify PAT hasn't expired
2. Check PAT has required scopes
3. Try generating a new PAT

## 📚 Next Steps

After successful setup:

1. **Read VSCODE_TESTING.md** for comprehensive VS Code testing guide
2. **Read USAGE_GUIDE.md** for detailed examples and workflows
3. **Try debugging** with `F5` - set breakpoints in your code
4. **Integrate with Copilot** (if you have it) or Claude Desktop

## 🎉 Success Criteria

You're ready when:
- ✅ Server builds without errors (`npm run build`)
- ✅ Interactive test runs successfully
- ✅ Can fetch PR changes from real PRs
- ✅ Can post test comments to PRs (if needed)

## 🔗 Important Files

- **VSCODE_TESTING.md** - Complete VS Code testing guide ⭐
- **README.md** - Project overview
- **CONFIG_EXAMPLE.md** - Configuration for VS Code, Copilot, Claude
- **USAGE_GUIDE.md** - Comprehensive examples and workflows
- **PROJECT_SUMMARY.md** - Technical details and architecture
- **test-interactive.js** - Interactive test script (recommended)
- **test-ado.js** - Simple test script

## 💡 Pro Tips for VS Code

1. **Use watch mode**: `npm run dev` in one terminal, test in another
2. **Debug everything**: Press `F5` and set breakpoints
3. **Use tasks**: `Ctrl+Shift+P` → "Tasks: Run Task"
4. **Terminal shortcuts**: `` Ctrl+` `` to toggle terminal
5. **View errors**: Problems panel (`Ctrl+Shift+M`)

## 🚀 Quick Commands

```bash
# Build
npm run build
# or press Ctrl+Shift+B

# Watch mode (auto-rebuild)
npm run dev

# Test
export AZURE_DEVOPS_PAT="your-pat"
node test-interactive.js

# Debug
# Press F5 in VS Code
```

---

**Need help?** Check **VSCODE_TESTING.md** for complete VS Code guide!

Good luck! 🚀
