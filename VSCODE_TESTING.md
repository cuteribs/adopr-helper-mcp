# Testing Your MCP Server in VS Code

## 🚀 Quick Start

You now have VS Code tasks and launch configurations set up!

## Method 1: Interactive Test Script (Recommended for First Test)

### Step 1: Set your PAT in terminal
In VS Code terminal (Ctrl+`):

**Bash:**
```bash
export AZURE_DEVOPS_PAT="your-pat-here"
```

**PowerShell:**
```powershell
$env:AZURE_DEVOPS_PAT="your-pat-here"
```

**CMD:**
```cmd
set AZURE_DEVOPS_PAT=your-pat-here
```

### Step 2: Run the interactive test
```bash
node test-interactive.js
```

This gives you a menu to:
- Test fetching PR changes
- Test posting comments
- See results in real-time

---

## Method 2: VS Code Tasks (Fastest)

### Press `Ctrl+Shift+B` (Build Task)
- Builds the TypeScript project

### Press `Ctrl+Shift+P` → "Tasks: Run Task"
- **Test MCP Server** - Runs the test suite
- **Run MCP Server (stdio)** - Starts the server for manual testing

When prompted, enter your Azure DevOps PAT.

---

## Method 3: Debug in VS Code (Best for Development)

### Press `F5` or go to Run & Debug panel

Choose one of:
1. **Debug MCP Server** - Debug the running server with breakpoints
2. **Debug Test Script** - Debug the test script

You can:
- ✅ Set breakpoints in your TypeScript source
- ✅ Step through code
- ✅ Inspect variables
- ✅ See call stacks

### Example: Debug a PR fetch
1. Open `src/azureDevOps.ts`
2. Set breakpoint on line in `getPrChanges` function
3. Press F5 → Select "Debug Test Script"
4. Enter your PAT when prompted
5. Edit `test-ado.js` to use your PR URL first
6. Watch the debugger stop at your breakpoint!

---

## Method 4: Simple Terminal Test

### One-liner test:
```bash
# Set PAT first!
export AZURE_DEVOPS_PAT="your-pat-here"

# Build and run test
npm run build && node test-ado.js
```

---

## Method 5: Test with MCP Inspector

### Install inspector:
```bash
npm install -g @modelcontextprotocol/inspector
```

### Run:
```bash
npx @modelcontextprotocol/inspector node dist/index.js
```

Opens a web UI at http://localhost:5173 where you can:
- See all available tools
- Test tool calls with a GUI
- View JSON responses
- Great for debugging MCP protocol issues

---

## Quick Test Checklist

Before testing, make sure:
- [ ] You have an Azure DevOps PAT with correct scopes
- [ ] You have a PR URL to test with
- [ ] Project is built: `npm run build`
- [ ] PAT is set in environment

### Minimum test:
1. Edit `test-ado.js` line 10 with your PR URL
2. Set PAT: `export AZURE_DEVOPS_PAT="your-pat"`
3. Run: `node test-ado.js`

### Interactive test:
1. Set PAT: `export AZURE_DEVOPS_PAT="your-pat"`
2. Run: `node test-interactive.js`
3. Follow the menu prompts

---

## Troubleshooting

### "AZURE_DEVOPS_PAT not set"
- Set it in the terminal before running tests
- Or use VS Code tasks (they'll prompt you)

### "Module not found"
- Run `npm run build` first
- Check that `dist/` folder exists

### "Invalid PR URL"
- Use full URL: `https://dev.azure.com/org/proj/_git/repo/pullrequest/123`
- Include `https://`

### Can't see breakpoints working
- Make sure source maps are enabled (already in tsconfig.json)
- Build with `npm run build`
- Use "Debug Test Script" launch config

---

## VS Code Extensions That Help

Consider installing:
- **REST Client** - Test Azure DevOps API directly
- **Thunder Client** - API testing in VS Code
- **Error Lens** - See errors inline

---

## Example Test Flow

```bash
# Terminal 1 - Watch mode (auto-rebuild on changes)
npm run dev

# Terminal 2 - Test
export AZURE_DEVOPS_PAT="your-pat"
node test-interactive.js
```

Now you can:
1. Make changes to TypeScript
2. They auto-compile
3. Re-run test immediately
4. Fast iteration!

---

## What's Next?

After basic testing works:
1. ✅ Test with Claude Desktop (see CONFIG_EXAMPLE.md)
2. ✅ Create custom test cases for your workflow
3. ✅ Add more tools/features
4. ✅ Set up CI/CD testing

Happy testing! 🎉
