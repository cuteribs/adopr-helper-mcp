using AdoPrHelperMcp.Auth;
using AdoPrHelperMcp.Models;
using AdoPrHelperMcp.Services;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System.Text.Json;

namespace AdoPrHelperMcp.Mcp;

/// <summary>
/// MCP Tool Definitions
/// 
/// This class configures all available tools for the MCP server.
/// Each tool is registered with the server with input schemas and handlers.
/// </summary>
public static class McpTools
{
	public static JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

	public static McpServerOptions CreateServerOptions(Implementation serverInfo, IAuthenticator authenticator)
	{
		var getPrChangesTool = new Tool
		{
			Name = "get_pr_changes",
			Description = "Fetches all file changes in an Azure DevOps pull request with diffs",
			InputSchema = JsonSerializer.SerializeToElement(
				new
				{
					type = "object",
					properties = new
					{
						prUrl = new
						{
							type = "string",
							description = "The full URL of the Azure DevOps pull request"
						}
					},
					required = new[] { "prUrl" }
				},
				JsonOptions
			)
		};
		var postPrComment = new Tool
		{
			Name = "post_pr_comment",
			Description = "Posts a comment to an Azure DevOps pull request thread",
			InputSchema = JsonSerializer.SerializeToElement(
				new
				{
					type = "object",
					properties = new
					{
						prUrl = new
						{
							type = "string",
							description = "The full URL of the Azure DevOps pull request"
						},
						comment = new
						{
							type = "string",
							description = "The comment text to post"
						},
						filePath = new
						{
							type = "string",
							description = "File path to attach comment to (creates new thread)"
						},
						rightFileStartLine = new
						{
							type = "number",
							description = "Start line number in file to attach comment to"
						},
						rightFileStartOffset = new
						{
							type = "number",
							description = "Offset in start line to attach comment to"
						},
						rightFileEndLine = new
						{
							type = "number",
							description = "End line number in file to attach comment to"
						},
						rightFileEndOffset = new
						{
							type = "number",
							description = "Offset in end line to attach comment to"
						}
					},
					required = new[] { "prUrl", "comment", "filePath", "rightFileStartLine", "rightFileStartOffset", "rightFileEndLine", "rightFileEndOffset" }
				},
				JsonOptions
			)
		};

		return new McpServerOptions
		{
			ServerInfo = serverInfo,
			Handlers = new()
			{
				ListToolsHandler = (_, _) => ValueTask.FromResult(new ListToolsResult
				{
					Tools = [getPrChangesTool, postPrComment]
				}),
				CallToolHandler = async (request, _) =>
				{
					var toolName = request.Params?.Name;
					var args = request.Params?.Arguments;

					if (toolName != null && args != null)
					{
						if (toolName == "get_pr_changes")
						{
							try
							{
								var prUrl = GetStringValue(args, "prUrl");
								var adoHelper = new AzureDevOpsHelper(prUrl, authenticator);
								var changes = await adoHelper.GetPrFileChangesAsync();

								var result = new
								{
									Success = true,
									ChangesCount = changes.Length,
									Changes = changes
								};

								var text = JsonSerializer.Serialize(result, JsonOptions);

								return CreateCallToolResult(text);
							}
							catch (Exception ex)
							{
								return CreateCallToolResult($"Error fetching PR changes: {ex.Message}", true);
							}
						}
						else if (toolName == "post_pr_comment")
						{
							try
							{
								var options = new PrCommentOptions
								{
									PrUrl = GetStringValue(args, "prUrl"),
									Comment = GetStringValue(args, "comment"),
									FilePath = GetStringValue(args, "filePath"),
									RightFileStartLine = Convert.ToInt32(GetStringValue(args, "rightFileStartLine")),
									RightFileStartOffset = Convert.ToInt32(GetStringValue(args, "rightFileStartOffset")),
									RightFileEndLine = Convert.ToInt32(GetStringValue(args, "rightFileEndLine")),
									RightFileEndOffset = Convert.ToInt32(GetStringValue(args, "rightFileEndOffset"))
								};

								var adoHelper = new AzureDevOpsHelper(options.PrUrl, authenticator);
								await adoHelper.PostPrCommentAsync(options);

								var result = new { Success = true };
								var text = JsonSerializer.Serialize(result, JsonOptions);
								return CreateCallToolResult(text);
							}
							catch (Exception ex)
							{
								return CreateCallToolResult($"Error posting comment: {ex.Message}", true);
							}
						}
					}

					return CreateCallToolResult($"Unknown tool", true);
				}
			}
		};
	}

	/// <summary>
	/// Gets a string value from the arguments dictionary.
	/// </summary>
	/// <param name="args"></param>
	/// <param name="key"></param>
	/// <returns></returns>
	/// <exception cref="ArgumentException"></exception>
	private static string GetStringValue(IReadOnlyDictionary<string, JsonElement> args, string key)
	{
		if (args?.TryGetValue(key, out var value) == true)
		{
			return value.ToString();
		}

		throw new ArgumentException($"{key} is required");
	}

	private static CallToolResult CreateCallToolResult(string text, bool isError = false)
	{
		return new()
		{
			Content = [new TextContentBlock { Text = text, Type = "text" }],
			IsError = isError
		};
	}
}
