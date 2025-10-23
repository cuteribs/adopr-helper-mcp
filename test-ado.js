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

const TEST_PR_URL = "https://dev.azure.com/dnvgl-one/Veracity%20Data%20Platform/_git/Connector/pullrequest/258662";
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
      "prUrl": "https://dev.azure.com/dnvgl-one/Engineering%20China/_git/dapr-shop/pullrequest/356025",
      "filePath": "/src/product/Repository.cs",
      "rightFileStartLine": 42,
      "rightFileStartOffset": 1,
      "rightFileEndLine": 43,
      "rightFileEndOffset": 1,
      "comment": "**Severity: High**\n\n```csharp\ncommand.CommandText = \"SELECT * FROM Item WHERE Id = \" + id;\n//command.Parameters.AddWithValue(\"@Id\", id.ToString());\n```\n\n**Issue:** Critical SQL injection vulnerability. The `id` parameter is directly concatenated into the SQL query string, allowing an attacker to inject arbitrary SQL commands.\n\n**Impact:** An attacker could:\n- Extract sensitive data from the database\n- Modify or delete data\n- Potentially gain control of the database server\n\n**Suggestion:** Use parameterized queries to prevent SQL injection. Uncomment and use the parameterized version:\n\n```suggestion\n\tpublic async Task<ProductItem?> Get(Guid id)\n\t{\n\t\tusing var connection = await this.OpenConnection();\n\t\tvar command = connection.CreateCommand();\n\t\tcommand.CommandText = \"SELECT * FROM Item WHERE Id = @Id\";\n\t\tcommand.Parameters.AddWithValue(\"@Id\", id.ToString());\n\n\t\tusing var reader = await command.ExecuteReaderAsync();\n```"
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
