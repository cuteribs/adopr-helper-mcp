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
    /// Get all file changes in a pull request with unified diffs
    /// 
    /// Process:
    /// 1. Parse PR URL to extract organization, project, repo, and PR ID
    /// 2. Fetch PR details to get source and target branches
    /// 3. Fetch all Git changes (commits) between branches
    /// 4. Filter to only supported changes (add/edit on blob files)
    /// 5. Download file contents and generate unified diffs in parallel
    /// </summary>
    public async Task<FilePatch[]> GetPrFileChangesAsync()
    {
        // Parse PR URL to extract components
        var prInfo = ParsePrUrl(_prUrl);
        var baseUrl = GetBaseUrl(prInfo.Organization, prInfo.Project, prInfo.Repository);

        // Get PR details including source and target branches
        var prDetailsUrl = GetPrDetailsUrl(baseUrl, prInfo.PullRequestId);
        var prDetails = await GetPrDetailsAsync(prDetailsUrl);
        
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
            .Select(c => c.Item)
            .ToArray();

        if (fileItems.Length == 0)
        {
            throw new InvalidOperationException("No supported code file found in this PR.");
        }

        // Download all files and generate diffs in parallel
        var getFileTasks = fileItems.Select(f => GetFilePatchAsync(f, baseUrl));
        var fileChanges = await Task.WhenAll(getFileTasks);
        
        return fileChanges;
    }

    /// <summary>
    /// Post a comment to a pull request thread
    /// 
    /// Creates a new thread on a specific file at the specified line and offset.
    /// The comment will be visible in the Azure DevOps PR interface.
    /// </summary>
    public async Task PostPrCommentAsync(PrCommentOptions options)
    {
        // Parse PR URL to extract components
        var prInfo = ParsePrUrl(options.PrUrl);
        var baseUrl = GetBaseUrl(prInfo.Organization, prInfo.Project, prInfo.Repository);
        var threadUrl = GetThreadUrl(baseUrl, prInfo.PullRequestId);

        // Build thread object with comment and file position
        var thread = new PrThread
        {
            Comments = [new PrComment { Content = options.Comment }],
            ThreadContext = new ThreadContext
            {
                FilePath = options.FilePath,
                RightFileStart = new FilePosition
                {
                    Line = options.RightFileStartLine,
                    Offset = options.RightFileStartOffset
                },
                RightFileEnd = new FilePosition
                {
                    Line = options.RightFileEndLine,
                    Offset = options.RightFileEndOffset
                }
            }
        };

        // Post the thread to Azure DevOps
        await SendRequestAsync(threadUrl, HttpMethod.Post, thread, "Failed to create thread");
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
        var supportedChangeTypes = new[] { "add", "edit" };
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

    private async Task<FilePatch> GetFilePatchAsync(GitItem fileItem, string baseUrl)
    {
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

        // Generate unified diff patch
        var patch = GenerateUnifiedDiff(filePath, sourceContent ?? "", newContent ?? "");
        
        return new FilePatch
        {
            FilePath = filePath,
            SourceContent = sourceContent,
            Patch = patch
        };
    }

    private static string GenerateUnifiedDiff(string fileName, string oldText, string newText)
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
                    oldLineNumber++;
                    break;

                case ChangeType.Inserted:
                    if (hunkLines.Count == 0)
                    {
                        hunkStart = newLineNumber;
                    }
                    hunkLines.Add($"+{line.Text}");
                    newLineNumber++;
                    break;

                case ChangeType.Modified:
                    if (hunkLines.Count == 0)
                    {
                        hunkStart = oldLineNumber;
                    }
                    hunkLines.Add($"-{line.Text}");
                    hunkLines.Add($"+{line.Text}");
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

        return sb.ToString();
    }

    private static void WriteHunk(StringBuilder sb, int oldStart, int oldCount, int newCount, List<string> lines)
    {
        sb.AppendLine($"@@ -{oldStart},{oldCount} +{oldStart},{newCount} @@");
        foreach (var line in lines)
        {
            sb.AppendLine(line);
        }
    }

    #endregion
}
