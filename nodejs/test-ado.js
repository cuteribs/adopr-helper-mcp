/**
 * Test script to verify Azure DevOps API functions
 * Run with: node test-ado.js
 */

// INSTRUCTIONS:
// 1. Set AZURE_DEVOPS_PAT environment variable:
//    Bash:       export AZURE_DEVOPS_PAT="your-pat-here"
//    PowerShell: $env:AZURE_DEVOPS_PAT="your-pat-here"
//    CMD:        set AZURE_DEVOPS_PAT=your-pat-here
// 2. Replace TEST_PR_URL below with your actual PR URL
// 3. Run: node test-ado.js

const TEST_PR_URL = "https://dev.azure.com/{org}/{project}/_git/{repo}/pullrequest/{prnumber}";
const TEST_COMMENT = "This is a test comment from MCP server";
const { AzureDevOpsHelper } = await import('./dist/azureDevOps.js');

const adoHelper = new AzureDevOpsHelper(
  TEST_PR_URL,
  () => Promise.resolve({ type: 'pat', token: process.env.AZURE_DEVOPS_PAT })
);

async function testGetPrChanges() {
  console.log("Testing get_pr_changes...");

  try {
    const changes = await adoHelper.getPrFileChanges();
    console.log(`✓ Successfully fetched ${changes.length} file changes`);
    console.log("\nFirst 3 changes:");
    changes.slice(0, 3).forEach(x => {
      console.log(`  - ${x.filePath}`);
      console.log(`    Content lines: ${x.sourceContent?.split('\n').length || 0}`);
      console.log(`    Diff lines: ${x.patch.split('\n').length}`);
    });
  } catch (error) {
    console.error("✗ Error:", error.message);
  }
}

async function testPostPrComment() {
  console.log("\nTesting post_pr_comment...");

  // Import the function
  try {
    const options = {
      "filePath": "/src/Data/ADOProductRepository.cs",
      "rightFileStartLine": 42,
      "rightFileStartOffset": 1,
      "rightFileEndLine": 43,
      "rightFileEndOffset": 1,
      "comment": "blablabla"
    };
    await adoHelper.postPrComment(options);
    console.log('✓ Successfully posted comment');
  } catch (error) {
    console.error("✗ Error:", error.message);
  }
}

// Run tests
async function main() {
  console.log("Azure DevOps PR Helper MCP Server - Test Suite\n");
  console.log("=".repeat(50));

  if (!process.env.AZURE_DEVOPS_PAT) {
    console.error("\n⚠ Warning: AZURE_DEVOPS_PAT environment variable not set");
    console.error("Please set it before running tests:\n");
    console.error("  export AZURE_DEVOPS_PAT=your-pat-here  (macOS/Linux)");
    console.error("  set AZURE_DEVOPS_PAT=your-pat-here     (Windows)\n");
    return;
  }

  await testGetPrChanges();
  // Uncomment to test posting comments (be careful not to spam PRs!)
  // await testPostPrComment();

  console.log("\n" + "=".repeat(50));
  console.log("Tests completed!");
}

main().catch(console.error);
