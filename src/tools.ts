/**
 * MCP Tool Definitions
 * 
 * This module configures all available tools for the MCP server.
 * Each tool is registered with the server with input schemas and handlers.
 */

import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { z } from "zod";
import { AzureDevOpsHelper } from "./azureDevOps.js";
import { AuthOptions } from './models.js';

/**
 * Configure all MCP tools on the server
 * 
 * Registers the following tools:
 * 1. get_pr_changes - Fetches all file changes in a PR with unified diffs
 * 2. post_pr_comment - Posts comments to PR threads
 * 
 * @param server - The MCP server instance to configure tools on
 */
export function configureAllTools(server: McpServer, tokenProvider: () => Promise<AuthOptions>) {
  // =============================================================================
  // Tool 1: Get PR Changes
  // =============================================================================
  // Fetches all file changes in a pull request, including unified diffs
  // for each modified file. Useful for code review and analysis.
  server.tool(
    "get_pr_changes",
    "Fetches all file changes in an Azure DevOps pull request with diffs",
    {
      prUrl: z.string().describe("The full URL of the Azure DevOps pull request"),
    },
    async (input) => {
      try {
        // Fetch all file changes with diffs from Azure DevOps
        const adoHelper = new AzureDevOpsHelper(input.prUrl, tokenProvider);
        const changes = await adoHelper.getPrFileChanges();

        // Return successful response with change details
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
        // Handle errors and return error response
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

  // =============================================================================
  // Tool 2: Post PR Comment
  // =============================================================================
  // Posts a comment to a specific location in a PR file.
  // Creates a new thread at the specified line and offset positions.
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
        // Post the comment to Azure DevOps PR thread
        const prCommentOptions = {
          prUrl: input.prUrl,
          comment: input.comment,
          filePath: input.filePath,
          rightFileStartLine: input.rightFileStartLine,
          rightFileStartOffset: input.rightFileStartOffset,
          rightFileEndLine: input.rightFileEndLine,
          rightFileEndOffset: input.rightFileEndOffset,
        };
        const adoHelper = new AzureDevOpsHelper(input.prUrl, tokenProvider);
        await adoHelper.postPrComment(prCommentOptions);

        // Return success response
        return {
          content: [
            {
              type: "text",
              text: JSON.stringify({ success: true }, null, 2),
            },
          ],
        };
      } catch (error) {
        // Handle errors and return error response
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
