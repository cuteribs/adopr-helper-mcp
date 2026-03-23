using AdoPrHelperMcp.Auth;
using AdoPrHelperMcp.Models;
using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AdoPrHelperMcp.Services;

/// <summary>
/// Azure DevOps API Integration
/// 
/// This class provides functions to interact with Azure DevOps REST API
/// for fetching pull request changes and posting comments.
/// </summary>
public partial class AzureDevOpsHelper
{
	private const string PathSeparator = "~~~";
	private const string ApiVersion = "7.1";
    private readonly string _prUrl;
    private readonly IAuthenticator _authenticator;
    private readonly HttpClient _httpClient;

    public AzureDevOpsHelper(string prUrl, IAuthenticator authenticator, HttpClient? httpClient = null)
    {
        _prUrl = prUrl;
        _authenticator = authenticator;
        _httpClient = httpClient ?? new HttpClient();
    }

    /// <summary>
    /// Fetch PR changes and save to disk with manifest
    /// 
    /// Process:
    /// 1. Parse PR URL and fetch PR metadata
    /// 2. Fetch all file changes with diffs
    /// 3. Save files to disk with escaped paths
    /// 4. Generate manifest.json with metadata
    /// 5. Return small response summary
    /// </summary>
    public async Task<FetchPrResponse> FetchPrChangesAsync(string outputFolder)
    {
        // Create output directory if it doesn't exist
        Directory.CreateDirectory(outputFolder);

        // Parse PR URL to extract components
        var prInfo = ParsePrUrl(_prUrl);
        var baseUrl = GetBaseUrl(prInfo.Organization, prInfo.Project, prInfo.Repository);

        // Get PR details including metadata
        var prDetailsUrl = GetPrDetailsUrl(baseUrl, prInfo.PullRequestId);
        var prDetails = await GetPrDetailsAsync(prDetailsUrl);

        // Get all file changes
        var filePatches = await GetPrFileChangesInternalAsync(prDetails, baseUrl);

        // Calculate statistics
        var changeBreakdown = CalculateChangeBreakdown(filePatches);
        long totalBytes = 0;
        var manifestFiles = new List<ManifestFile>();

        // Save each file and diff to disk
        foreach (var filePatch in filePatches)
        {
            var escapedName = EscapePath(filePatch.FilePath);
            var diffName = $"{escapedName}.diff";

            // Write full file content
            if (!string.IsNullOrEmpty(filePatch.NewContent))
            {
                var filePath = Path.Combine(outputFolder, escapedName);
                await File.WriteAllTextAsync(filePath, filePatch.NewContent);
                totalBytes += filePatch.NewContent.Length;
            }

            // Write diff
            var diffPath = Path.Combine(outputFolder, diffName);
            await File.WriteAllTextAsync(diffPath, filePatch.Patch);
            totalBytes += filePatch.Patch.Length;

            // Add to manifest
            manifestFiles.Add(new ManifestFile
            {
                OriginalPath = filePatch.FilePath,
                EscapedName = escapedName,
                DiffName = diffName,
                ChangeType = filePatch.ChangeType,
                SizeBytes = filePatch.NewContent?.Length ?? 0,
                DiffSizeBytes = filePatch.Patch.Length,
                LinesAdded = filePatch.LinesAdded,
                LinesDeleted = filePatch.LinesDeleted
            });
        }

        // Create manifest
        var manifest = new ManifestData
        {
            PrUrl = _prUrl,
            PrId = prDetails.PullRequestId,
            PrTitle = prDetails.Title ?? "",
            PrDescription = prDetails.Description,
            PrAuthor = new PrAuthor
            {
                DisplayName = prDetails.CreatedBy?.DisplayName ?? "Unknown",
                Email = prDetails.CreatedBy?.UniqueName ?? ""
            },
            PrStatus = prDetails.Status,
            SourceBranch = prDetails.SourceRefName.Replace("refs/heads/", ""),
            TargetBranch = prDetails.TargetRefName.Replace("refs/heads/", ""),
            CreatedDate = prDetails.CreationDate ?? DateTime.UtcNow.ToString("o"),
            FetchTimestamp = DateTime.UtcNow.ToString("o"),
            Statistics = new ChangeStatistics
            {
                TotalFiles = filePatches.Length,
                TotalSizeBytes = totalBytes,
                Changes = changeBreakdown
            },
            Files = manifestFiles.ToArray()
        };

        // Save manifest to disk
        var manifestPath = Path.Combine(outputFolder, "manifest.json");
        var manifestJson = JsonSerializer.Serialize(manifest, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });
        await File.WriteAllTextAsync(manifestPath, manifestJson);

        // Return small response
        return new FetchPrResponse
        {
            Success = true,
            ManifestPath = manifestPath,
            FilesSaved = filePatches.Length,
            TotalBytes = totalBytes,
            Summary = changeBreakdown
        };
    }

    /// <summary>
    /// Get all file changes in a pull request with unified diffs (internal)
    /// </summary>
    private async Task<FilePatch[]> GetPrFileChangesInternalAsync(PullRequest prDetails, string baseUrl)
    {
        // Parse PR URL to extract components
        var prInfo = ParsePrUrl(_prUrl);
        
        var sourceBranch = Uri.EscapeDataString(prDetails.SourceRefName.Replace("refs/heads/", ""));
        var targetBranch = Uri.EscapeDataString(prDetails.TargetRefName.Replace("refs/heads/", ""));

        if (string.IsNullOrEmpty(sourceBranch) || string.IsNullOrEmpty(targetBranch))
        {
            throw new InvalidOperationException("Could not determine source or target branch from PR details.");
        }

        // Fetch all changes between branches
        var diffsUrl = GetDiffsUrl(baseUrl, sourceBranch, targetBranch);
        var changes = await GetGitChangesAsync(diffsUrl);

        if (changes.Length == 0)
        {
            throw new InvalidOperationException("No changed files found in this PR.");
        }

        // Filter to only process supported file types (add/edit on blob files)
        var fileItems = changes
            .Where(IsSupportedChange)
            .ToArray();

        if (fileItems.Length == 0)
        {
            throw new InvalidOperationException("No supported code file found in this PR.");
        }

        // Download all files and generate diffs in parallel
        var getFileTasks = fileItems.Select(c => GetFilePatchAsync(c, baseUrl));
        var fileChanges = await Task.WhenAll(getFileTasks);
        
        return fileChanges;
    }

    /// <summary>
    /// Calculate change breakdown statistics
    /// </summary>
    private static ChangeBreakdown CalculateChangeBreakdown(FilePatch[] filePatches)
    {
        return new ChangeBreakdown
        {
            Added = filePatches.Count(f => f.ChangeType.Equals("add", StringComparison.OrdinalIgnoreCase)),
            Modified = filePatches.Count(f => f.ChangeType.Equals("edit", StringComparison.OrdinalIgnoreCase)),
            Deleted = filePatches.Count(f => f.ChangeType.Equals("delete", StringComparison.OrdinalIgnoreCase)),
            Renamed = filePatches.Count(f => f.ChangeType.Equals("rename", StringComparison.OrdinalIgnoreCase))
        };
    }

    /// <summary>
    /// Post a comment to a pull request thread
    /// 
    /// Creates a new thread on a specific file at the specified line.
    /// Supports severity levels and thread status.
    /// </summary>
    public async Task PostPrCommentAsync(PrCommentOptions options)
    {
        // Parse PR URL to extract components
        var prInfo = ParsePrUrl(options.PrUrl);
        var baseUrl = GetBaseUrl(prInfo.Organization, prInfo.Project, prInfo.Repository);
        var threadUrl = GetThreadUrl(baseUrl, prInfo.PullRequestId);

        // Format comment with severity if provided
        var commentContent = FormatCommentWithSeverity(options.CommentText, options.Severity);

        // Build thread object with comment and file position
        var thread = new PrThread
        {
            Comments = [new PrComment { Content = commentContent }],
            ThreadContext = new ThreadContext
            {
                FilePath = options.FilePath,
                RightFileStart = new FilePosition
                {
                    Line = options.LineNumber,
                    Offset = 1
                },
                RightFileEnd = new FilePosition
                {
                    Line = options.LineNumber,
                    Offset = 999
                }
            }
        };

        // Post the thread to Azure DevOps
        await SendRequestAsync(threadUrl, HttpMethod.Post, thread, "Failed to create thread");
    }

    /// <summary>
    /// Format comment with severity badge if provided
    /// </summary>
    private static string FormatCommentWithSeverity(string commentText, string? severity)
    {
        if (string.IsNullOrEmpty(severity))
        {
            return commentText;
        }

        // If comment already has severity formatting, return as-is
        if (commentText.TrimStart().StartsWith("**["))
        {
            return commentText;
        }

        // Add severity badge to the beginning
        return $"**[{severity}]**\n\n{commentText}";
    }

    #region Private Helper Methods

    private static string GetBaseUrl(string organization, string project, string repository)
    {
        return $"https://dev.azure.com/{organization}/{project}/_apis/git/repositories/{repository}";
    }

    private static string GetPrDetailsUrl(string baseUrl, int pullRequestId)
    {
        return $"{baseUrl}/pullRequests/{pullRequestId}?api-version={ApiVersion}";
    }

    private static string GetDiffsUrl(string baseUrl, string sourceBranch, string targetBranch)
    {
        return $"{baseUrl}/diffs/commits?baseVersion={targetBranch}&targetVersion={sourceBranch}&$top=2000&api-version={ApiVersion}";
    }

    private static string GetBlobUrl(string baseUrl, string sha)
    {
        return $"{baseUrl}/blobs/{sha}?api-version={ApiVersion}";
    }

    private static string GetThreadUrl(string baseUrl, int pullRequestId)
    {
        return $"{baseUrl}/pullRequests/{pullRequestId}/threads?api-version={ApiVersion}";
    }

    private async Task<Dictionary<string, string>> GetDefaultHeadersAsync()
    {
        var authOptions = await _authenticator.GetAuthOptionsAsync();

        if (string.IsNullOrEmpty(authOptions.Token))
        {
            throw new InvalidOperationException("Azure DevOps authentication token is not available.");
        }

        var authorization = authOptions.Type == "pat"
            ? $"Basic {Convert.ToBase64String(Encoding.ASCII.GetBytes($":{authOptions.Token}"))}"
            : $"Bearer {authOptions.Token}";

        return new Dictionary<string, string>
        {
            { "Authorization", authorization },
            { "Content-Type", "application/json" },
            { "Accept", "application/json" }
        };
    }

    [GeneratedRegex(@"https://dev\.azure\.com/(.+?)/(.+?)/_git/(.+?)/pullrequest/(\d+)", RegexOptions.IgnoreCase)]
    private static partial Regex DevAzureRegex();

    [GeneratedRegex(@"https://(.+?)\.visualstudio\.com/(.+?)/_git/(.+?)/pullrequest/(\d+)", RegexOptions.IgnoreCase)]
    private static partial Regex VisualStudioRegex();

    private static PrInfo ParsePrUrl(string prUrl)
    {
        var prInfo = ParsePrUrlInternal(prUrl, DevAzureRegex()) 
                     ?? ParsePrUrlInternal(prUrl, VisualStudioRegex());

        if (prInfo == null)
        {
            throw new ArgumentException("Invalid Azure DevOps PR URL format", nameof(prUrl));
        }

        return prInfo;
    }

    private static PrInfo? ParsePrUrlInternal(string prUrl, Regex pattern)
    {
        var match = pattern.Match(prUrl);

        if (!match.Success)
        {
            return null;
        }

        return new PrInfo
        {
            Organization = match.Groups[1].Value,
            Project = match.Groups[2].Value,
            Repository = match.Groups[3].Value,
            PullRequestId = int.Parse(match.Groups[4].Value)
        };
    }

    private static bool IsSupportedChange(GitChange change)
    {
        var supportedChangeTypes = new[] { "add", "edit", "delete", "rename" };
        return supportedChangeTypes.Contains(change.ChangeType, StringComparer.OrdinalIgnoreCase)
               && change.Item.GitObjectType.Equals("blob", StringComparison.OrdinalIgnoreCase)
               && !string.IsNullOrEmpty(change.Item.Path)
               && !string.IsNullOrEmpty(change.Item.Url);
    }

    private async Task<T> SendRequestAsync<T>(string url, HttpMethod method, object? body = null, string errorMessage = "Error")
    {
        var headers = await GetDefaultHeadersAsync();
        
        var request = new HttpRequestMessage(method, url);
        
        foreach (var header in headers)
        {
            request.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        if (body != null && (method == HttpMethod.Post || method == HttpMethod.Put || method == HttpMethod.Patch))
        {
            var jsonContent = JsonSerializer.Serialize(body, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });
            request.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
        }

        var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"{errorMessage}: HTTP {response.StatusCode}: {response.ReasonPhrase}");
        }

        var responseContent = await response.Content.ReadAsStringAsync();
        
        return JsonSerializer.Deserialize<T>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? throw new InvalidOperationException("Failed to deserialize response");
    }

    private async Task SendRequestAsync(string url, HttpMethod method, object? body = null, string errorMessage = "Error")
    {
        var headers = await GetDefaultHeadersAsync();
        
        var request = new HttpRequestMessage(method, url);
        
        foreach (var header in headers)
        {
            request.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        if (body != null && (method == HttpMethod.Post || method == HttpMethod.Put || method == HttpMethod.Patch))
        {
            var jsonContent = JsonSerializer.Serialize(body, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });
            request.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
        }

        var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"{errorMessage}: HTTP {response.StatusCode}: {response.ReasonPhrase}");
        }
    }

    private async Task<string?> GetBlobContentAsync(string url)
    {
        var headers = await GetDefaultHeadersAsync();
        
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));
        
        foreach (var header in headers.Where(h => h.Key != "Content-Type" && h.Key != "Accept"))
        {
            request.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadAsStringAsync();
    }

    private async Task<PullRequest> GetPrDetailsAsync(string url)
    {
        var prDetails = await SendRequestAsync<PullRequest>(url, HttpMethod.Get, null, "Failed to get PR details");

        if (!prDetails.Status.Equals("active", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The PR is not active.");
        }

        if (!prDetails.MergeStatus.Equals("succeeded", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The PR has merge conflict.");
        }

        return prDetails;
    }

    private async Task<GitChange[]> GetGitChangesAsync(string url)
    {
        var data = await SendRequestAsync<CommitDiffs>(url, HttpMethod.Get, null, "Failed to get git changes");
        return data.Changes ?? [];
    }

    private async Task<FilePatch> GetFilePatchAsync(GitChange gitChange, string baseUrl)
    {
        var fileItem = gitChange.Item;
        var filePath = fileItem.Path;
        string? sourceContent = null;
        string? newContent = null;

        // Download original file content (if exists)
        if (!string.IsNullOrEmpty(fileItem.OriginalObjectId))
        {
            var url = GetBlobUrl(baseUrl, fileItem.OriginalObjectId);
            sourceContent = await GetBlobContentAsync(url);
        }

        // Download new file content (if exists)
        if (!string.IsNullOrEmpty(fileItem.ObjectId))
        {
            var url = GetBlobUrl(baseUrl, fileItem.ObjectId);
            newContent = await GetBlobContentAsync(url);
        }

        // Generate unified diff patch and count lines
        var (patch, linesAdded, linesDeleted) = GenerateUnifiedDiffWithStats(filePath, sourceContent ?? "", newContent ?? "");
        
        return new FilePatch
        {
            FilePath = filePath,
            SourceContent = sourceContent,
            NewContent = newContent,
            Patch = patch,
            ChangeType = gitChange.ChangeType,
            LinesAdded = linesAdded,
            LinesDeleted = linesDeleted
        };
    }

    private static (string patch, int linesAdded, int linesDeleted) GenerateUnifiedDiffWithStats(string fileName, string oldText, string newText)
    {
        var differ = new Differ();
        var builder = new InlineDiffBuilder(differ);
        var diff = builder.BuildDiffModel(oldText, newText);

        var sb = new StringBuilder();
        sb.AppendLine($"--- a/{fileName}");
        sb.AppendLine($"+++ b/{fileName}");

        var oldLineNumber = 1;
        var newLineNumber = 1;
        var hunkStart = 0;
        var hunkLines = new List<string>();
        int linesAdded = 0;
        int linesDeleted = 0;

        foreach (var line in diff.Lines)
        {
            switch (line.Type)
            {
                case ChangeType.Unchanged:
                    if (hunkLines.Count > 0)
                    {
                        // Write pending hunk
                        WriteHunk(sb, hunkStart, oldLineNumber - hunkStart, newLineNumber - hunkStart, hunkLines);
                        hunkLines.Clear();
                    }
                    hunkStart = oldLineNumber;
                    oldLineNumber++;
                    newLineNumber++;
                    break;

                case ChangeType.Deleted:
                    if (hunkLines.Count == 0)
                    {
                        hunkStart = oldLineNumber;
                    }
                    hunkLines.Add($"-{line.Text}");
                    linesDeleted++;
                    oldLineNumber++;
                    break;

                case ChangeType.Inserted:
                    if (hunkLines.Count == 0)
                    {
                        hunkStart = newLineNumber;
                    }
                    hunkLines.Add($"+{line.Text}");
                    linesAdded++;
                    newLineNumber++;
                    break;

                case ChangeType.Modified:
                    if (hunkLines.Count == 0)
                    {
                        hunkStart = oldLineNumber;
                    }
                    hunkLines.Add($"-{line.Text}");
                    hunkLines.Add($"+{line.Text}");
                    linesDeleted++;
                    linesAdded++;
                    oldLineNumber++;
                    newLineNumber++;
                    break;
            }
        }

        // Write final hunk
        if (hunkLines.Count > 0)
        {
            WriteHunk(sb, hunkStart, oldLineNumber - hunkStart, newLineNumber - hunkStart, hunkLines);
        }

        return (sb.ToString(), linesAdded, linesDeleted);
    }

    private static void WriteHunk(StringBuilder sb, int oldStart, int oldCount, int newCount, List<string> lines)
    {
        sb.AppendLine($"@@ -{oldStart},{oldCount} +{oldStart},{newCount} @@");
        foreach (var line in lines)
        {
            sb.AppendLine(line);
        }
	}

	/// <summary>
	/// Escapes a file path by replacing / and \ with ~~~
	/// </summary>
	/// <param name="filePath">Original file path</param>
	/// <returns>Escaped file path</returns>
	private static string EscapePath(string filePath)
	{
		return filePath
			.Replace("/", PathSeparator)
			.Replace("\\", PathSeparator);
	}

	#endregion
}
