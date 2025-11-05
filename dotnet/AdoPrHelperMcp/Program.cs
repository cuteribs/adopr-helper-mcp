using AdoPrHelperMcp.Auth;
using AdoPrHelperMcp.Mcp;
using ModelContextProtocol.Server;
using System.CommandLine;
using System.Reflection;

namespace AdoPrHelperMcp;

/// <summary>
/// Azure DevOps PR Helper MCP Server
/// 
/// This is the main entry point for the Model Context Protocol (MCP) server
/// that enables AI assistants to interact with Azure DevOps Pull Requests.
/// 
/// The server provides tools for:
/// - Fetching PR file changes with diffs
/// - Posting comments to PR threads
/// </summary>
class Program
{
	private const string ServerName = "Azure DevOps PR Helper MCP Server";

	static async Task<int> Main(string[] args)
	{
		// Get version from assembly
		var version = Assembly.GetExecutingAssembly()
			.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
			.InformationalVersion
			?? "1.0.0";

		var rootCommand = new RootCommand($"{ServerName} v{version}");

		var authOption = new Option<string>("--authentication", "-a")
		{
			Description = "Type of authentication to use. Supported values are 'interactive' and 'pat' (default: 'interactive')"
		};

		rootCommand.Options.Add(authOption);

		rootCommand.SetAction(x =>
		{
			var authType = x.GetValue(authOption) ?? "interactive";
			RunServerAsync(authType, version).GetAwaiter().GetResult();
		});

		var result = rootCommand.Parse(args);
		return await result.InvokeAsync();
	}

	static async Task RunServerAsync(string authenticationType, string version)
	{
		try
		{
			// Create the authenticator based on the authentication type
			var authenticator = AuthenticatorFactory.CreateAuthenticator(authenticationType);
			var serverOptions = McpTools.CreateServerOptions(
				new()
				{
					Name = ServerName,
					Version = version
				},
				authenticator
			);

			await using var server = McpServer.Create(new StdioServerTransport(ServerName), serverOptions);

			await Console.Error.WriteLineAsync($"{ServerName} v{version} running on stdio with {authenticationType} authentication");

			// Start the server and keep it running
			await server.RunAsync();
		}
		catch (Exception ex)
		{
			await Console.Error.WriteLineAsync($"Fatal error: {ex.Message}");
			Environment.Exit(1);
		}
	}
}
