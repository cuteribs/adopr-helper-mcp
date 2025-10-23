import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { z } from "zod";
import { getPrFileChanges, postPrComment } from "./azureDevOps.js";

export function configureAllTools(server: McpServer): void {
  // Tool 1: Get PR Changes
  server.tool(
    "get_pr_changes",
    "Fetches all file changes in an Azure DevOps pull request with diffs",
    {
      prUrl: z.string().describe("The full URL of the Azure DevOps pull request"),
    },
    async (input) => {
      try {
        const changes = await getPrFileChanges(input.prUrl);

        return {
          content: [
            {
              type: "text",
              text: JSON.stringify(
                {
                  success: true,
                  changesCount: changes.length,
                  changes: changes,
                },
                null,
                2
              ),
            },
          ],
        };
      } catch (error) {
        const errorMessage = error instanceof Error ? error.message : String(error);
        return {
          content: [
            {
              type: "text",
              text: `Error fetching PR changes: ${errorMessage}`,
            },
          ],
          isError: true,
        };
      }
    }
  );

  // Tool 2: Post PR Comment
  server.tool(
    "post_pr_comment",
    "Posts a comment to an Azure DevOps pull request thread",
    {
      prUrl: z.string().describe("The full URL of the Azure DevOps pull request"),
      comment: z.string().describe("The comment text to post"),
      filePath: z.string().describe("File path to attach comment to (creates new thread)"),
      rightFileStartLine: z.number().describe("Start line number in file to attach comment to"),
      rightFileStartOffset: z.number().describe("Offset in start line to attach comment to"),
      rightFileEndLine: z.number().describe("End line number in file to attach comment to"),
      rightFileEndOffset: z.number().describe("Offset in end line to attach comment to"),
    },
    async (input) => {
      try {
        await postPrComment({
          prUrl: input.prUrl,
          comment: input.comment,
          filePath: input.filePath,
          rightFileStartLine: input.rightFileStartLine,
          rightFileStartOffset: input.rightFileStartOffset,
          rightFileEndLine: input.rightFileEndLine,
          rightFileEndOffset: input.rightFileEndOffset,
        });

        return {
          content: [
            {
              type: "text",
              text: JSON.stringify({ success: true }, null, 2),
            },
          ],
        };
      } catch (error) {
        const errorMessage = error instanceof Error ? error.message : String(error);
        return {
          content: [
            {
              type: "text",
              text: `Error posting comment: ${errorMessage}`,
            },
          ],
          isError: true,
        };
      }
    }
  );
}
