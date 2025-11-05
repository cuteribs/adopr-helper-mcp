#!/usr/bin/env node

/**
 * Azure DevOps PR Helper MCP Server
 * 
 * This is the main entry point for the Model Context Protocol (MCP) server
 * that enables AI assistants to interact with Azure DevOps Pull Requests.
 * 
 * The server provides tools for:
 * - Fetching PR file changes with diffs
 * - Posting comments to PR threads
 */

import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { StdioServerTransport } from "@modelcontextprotocol/sdk/server/stdio.js";
import { configureAllTools } from "./tools.js";
import yargs from "yargs";
import { hideBin } from "yargs/helpers";
import { createAuthenticator } from './auth.js';
import { createRequire } from 'module';

const require = createRequire(import.meta.url);
const packageJson = require('../package.json');
const packageVersion = packageJson.version;
const mcpServerName = `Azure DevOps PR Helper MCP Server v${packageVersion}`;

const argv = yargs(hideBin(process.argv))
  .scriptName("mcp-server-adopr-helper")
  .usage("Usage: $0 [options]")
  .version(packageVersion)
  .command("$0 [options]", mcpServerName)
  .option("authentication", {
    alias: "a",
    describe: "Type of authentication to use. Supported values are 'interactive' and 'pat' (default: 'interactive')",
    type: "string",
    choices: ["interactive", "pat"],
    default: "interactive",
  })
  .help()
  .parseSync();

/**
 * Initialize and start the MCP server
 * 
 * Creates an MCP server instance, configures all available tools,
 * and connects it to the stdio transport for communication.
 */
async function main() {
  // Create the MCP server instance with metadata
  const server = new McpServer({
    name: mcpServerName,
    version: packageVersion,
    icons: [
      {
        src: "https://cdn.vsassets.io/content/icons/favicon.ico",
      },
    ],
  });

  const authenticator = createAuthenticator(argv.authentication);

  // Register all available tools (get_pr_changes, post_pr_comment)
  configureAllTools(server, authenticator);

  // Connect using stdio transport for communication with MCP clients
  const transport = new StdioServerTransport();
  await server.connect(transport);

  console.error(`${mcpServerName} running on stdio with ${argv.authentication} authentication`);
}

// Start the server and handle any fatal errors
main().catch((error) => {
  console.error("Fatal error in main():", error);
  process.exit(1);
});
