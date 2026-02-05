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
		var fetchPrChangesTool = new Tool
		{
			Name = "azure_devops_fetch_pr_changes",
			Description = "Fetches all changed files from a pull request and saves them directly to a local folder with escaped paths and manifest",
			InputSchema = JsonSerializer.SerializeToElement(
				new
				{
					type = "object",
					properties = new
					{
						pr_url = new
						{
							type = "string",
							description = "The full URL of the Azure DevOps pull request"
						},
						output_folder = new
						{
							type = "string",
							description = "The local folder path where files will be saved"
						}
					},
					required = new[] { "pr_url", "output_folder" }
				},
				JsonOptions
			)
		};
		var postPrCommentTool = new Tool
		{
			Name = "azure_devops_post_comment",
			Description = "Posts a review comment to a specific file and line in an Azure DevOps pull request",
			InputSchema = JsonSerializer.SerializeToElement(
				new
				{
					type = "object",
					properties = new
					{
						pr_url = new
						{
							type = "string",
							description = "The full URL of the Azure DevOps pull request"
						},
						file_path = new
						{
							type = "string",
							description = "File path to attach comment to"
						},
						line_number = new
						{
							type = "number",
							description = "Line number in file to attach comment to"
						},
						comment_text = new
						{
							type = "string",
							description = "The comment text to post"
						},
						severity = new
						{
							type = "string",
							description = "Severity level (Critical, High, Medium, Low)",
							@enum = new[] { "Critical", "High", "Medium", "Low" }
						},
						thread_status = new
						{
							type = "string",
							description = "Thread status (active, fixed, wontFix, closed)",
							@enum = new[] { "active", "fixed", "wontFix", "closed" }
						}
					},
					required = new[] { "pr_url", "file_path", "line_number", "comment_text" }
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
					Tools = [fetchPrChangesTool, postPrCommentTool]
				}),
				CallToolHandler = async (request, _) =>
				{
					var toolName = request.Params?.Name;
					var args = request.Params?.Arguments;

					if (toolName != null && args != null)
					{
						if (toolName == "azure_devops_fetch_pr_changes")
						{
							try
							{
								var prUrl = GetStringValue(args, "pr_url");
								var outputFolder = GetStringValue(args, "output_folder");
								var adoHelper = new AzureDevOpsHelper(prUrl, authenticator);
								var result = await adoHelper.FetchPrChangesAsync(outputFolder);

								var text = JsonSerializer.Serialize(result, JsonOptions);
								return CreateCallToolResult(text);
							}
							catch (Exception ex)
							{
								return HandleToolError(ex, "Error fetching PR changes");
							}
						}
						else if (toolName == "azure_devops_post_comment")
						{
							try
							{
								var options = new PrCommentOptions
								{
									PrUrl = GetStringValue(args, "pr_url"),
									FilePath = GetStringValue(args, "file_path"),
									LineNumber = GetIntValue(args, "line_number"),
									CommentText = GetStringValue(args, "comment_text"),
									Severity = GetOptionalStringValue(args, "severity"),
									ThreadStatus = GetOptionalStringValue(args, "thread_status")
								};

								var adoHelper = new AzureDevOpsHelper(options.PrUrl, authenticator);
								await adoHelper.PostPrCommentAsync(options);

								var result = new { Success = true };
								var text = JsonSerializer.Serialize(result, JsonOptions);
								return CreateCallToolResult(text);
							}
							catch (Exception ex)
							{
								return HandleToolError(ex, "Error posting comment");
							}
						}
					}

					return CreateCallToolResult($"Unknown tool", true);
				}
			}
		};
	}

	/// <summary>
	/// Handles tool errors and returns appropriate error responses.
	/// </summary>
	private static CallToolResult HandleToolError(Exception ex, string defaultErrorMessage)
	{
		string code;
		string message;

		switch (ex)
		{
			case ArgumentException argEx:
				code = ErrorCodes.InvalidInput;
				message = argEx.Message;
				break;
			case InvalidOperationException invOpEx when invOpEx.Message.Contains("not found") || invOpEx.Message.Contains("access denied"):
				code = ErrorCodes.PrNotFound;
				message = invOpEx.Message;
				break;
			case HttpRequestException httpEx when httpEx.StatusCode == System.Net.HttpStatusCode.Unauthorized:
				code = ErrorCodes.AuthFailed;
				message = "Authentication failed";
				break;
			case HttpRequestException httpEx when httpEx.StatusCode == System.Net.HttpStatusCode.TooManyRequests:
				code = ErrorCodes.RateLimited;
				message = "Too many requests";
				break;
			case HttpRequestException httpEx when httpEx.Message.Contains("thread"):
				code = ErrorCodes.CommentFailed;
				message = httpEx.Message;
				break;
			default:
				code = ErrorCodes.Unknown;
				message = $"{defaultErrorMessage}: {ex.Message}";
				break;
		}

		var error = new ErrorResponse
		{
			Success = false,
			Error = new() { Code = code, Message = message }
		};
		return CreateCallToolResult(JsonSerializer.Serialize(error, JsonOptions), true);
	}

	/// <summary>
	/// Gets a string value from the arguments dictionary.
	/// </summary>
	private static string GetStringValue(IReadOnlyDictionary<string, JsonElement> args, string key)
	{
		if (args?.TryGetValue(key, out var value) == true)
		{
			return value.ToString();
		}

		throw new ArgumentException($"{key} is required");
	}

	/// <summary>
	/// Gets an optional string value from the arguments dictionary.
	/// </summary>
	private static string? GetOptionalStringValue(IReadOnlyDictionary<string, JsonElement> args, string key)
	{
		if (args?.TryGetValue(key, out var value) == true && value.ValueKind != JsonValueKind.Null)
		{
			return value.ToString();
		}

		return null;
	}

	/// <summary>
	/// Gets an integer value from the arguments dictionary.
	/// </summary>
	private static int GetIntValue(IReadOnlyDictionary<string, JsonElement> args, string key)
	{
		if (args?.TryGetValue(key, out var value) == true)
		{
			if (value.ValueKind == JsonValueKind.Number)
			{
				return value.GetInt32();
			}
			if (value.ValueKind == JsonValueKind.String && int.TryParse(value.ToString(), out var intValue))
			{
				return intValue;
			}
		}

		throw new ArgumentException($"{key} must be a valid integer");
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
