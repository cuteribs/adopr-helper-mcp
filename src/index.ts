#!/usr/bin/env node

import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { StdioServerTransport } from "@modelcontextprotocol/sdk/server/stdio.js";
import { configureAllTools } from "./tools.js";

const packageVersion = "0.1.0";

async function main(): Promise<void> {
  const server = new McpServer({
    name: "Azure DevOps PR Helper MCP Server",
    version: packageVersion,
  });

  // Configure all tools
  configureAllTools(server);

  // Connect using stdio transport
  const transport = new StdioServerTransport();
  await server.connect(transport);
}

main().catch((error) => {
  console.error("Fatal error in main():", error);
  process.exit(1);
});
