/**
 * Azure DevOps API Integration
 * 
 * This module provides functions to interact with Azure DevOps REST API
 * for fetching pull request changes and posting comments.
 * 
 * Authentication is done via Personal Access Token (PAT) from environment variable.
 */

import { createPatch } from 'diff';
import { AuthOptions, CommitDiffs, FilePatch, GitChange, GitItem, PrCommentOptions, PrInfo, PrThread, PullRequest } from './models.js';
import { get } from 'http';

// Azure DevOps REST API version
const API_VERSION = '7.1';

export class AzureDevOpsHelper {
  private prUrl: string
  private tokenProvider: () => Promise<AuthOptions>;

  constructor(prUrl: string, tokenProvider: () => Promise<AuthOptions>) {
    this.prUrl = prUrl;
    this.tokenProvider = tokenProvider;
  }
  /**
   * Build the base URL for Azure DevOps Git API
   * 
   * @param organization - Azure DevOps organization name
   * @param project - Project name
   * @param repository - Repository name
   * @returns Base URL for repository API endpoints
   */
  private getBaseUrl(organization: string, project: string, repository: string) {
    return `https://dev.azure.com/${organization}/${project}/_apis/git/repositories/${repository}`;
  }

  /**
   * Build URL to fetch PR details
   * 
   * @param baseUrl - Repository base URL
   * @param pullRequestId - Pull request ID
   * @returns URL to fetch PR details
   */
  private getPrDetailsUrl(baseUrl: string, pullRequestId: number) {
    return `${baseUrl}/pullRequests/${pullRequestId}?api-version=${API_VERSION}`;
  }

  /**
   * Build URL to fetch commit diffs between branches
   * 
   * @param baseUrl - Repository base URL
   * @param sourceBranch - Source branch name (encoded)
   * @param targetBranch - Target branch name (encoded)
   * @returns URL to fetch diffs
   */
  private getDiffsUrl(baseUrl: string, sourceBranch: string, targetBranch: string) {
    return `${baseUrl}/diffs/commits?baseVersion=${targetBranch}&targetVersion=${sourceBranch}&$top=2000&api-version=${API_VERSION}`;
  }

  /**
   * Build URL to fetch blob content by SHA
   * 
   * @param baseUrl - Repository base URL
   * @param sha - Git object SHA
   * @returns URL to fetch blob content
   */
  private getBlobUrl(baseUrl: string, sha: string) {
    return `${baseUrl}/blobs/${sha}?api-version=${API_VERSION}`;
  }

  /**
   * Build URL for PR threads endpoint
   * 
   * @param baseUrl - Repository base URL
   * @param pullRequestId - Pull request ID
   * @returns URL for threads API
   */
  private getThreadUrl(baseUrl: string, pullRequestId: number) {
    return `${baseUrl}/pullRequests/${pullRequestId}/threads?api-version=${API_VERSION}`;
  }

  /**
   * Get default headers for Azure DevOps API requests
   * 
   * Includes Basic authentication with PAT and standard content type headers.
   * 
   * @returns Headers object for API requests
   * @throws Error if PAT is not set in environment
   */
  private async getDefaultHeaders() {
    const authOptions = await this.tokenProvider();

    if (!authOptions.token) {
      throw new Error('Azure DevOps PAT is not set in environment variables.');
    }

    const authorization = authOptions.type === 'pat'
      ? `Basic ${btoa(`:${authOptions.token}`)}`
      : `Bearer ${authOptions.token}`;
    return {
      'Authorization': authorization,
      'Content-Type': "application/json",
      'Accept': "application/json",
    };
  };

  /**
   * Parse Azure DevOps PR URL to extract components
   * 
   * Supports both URL formats:
   * - https://dev.azure.com/{org}/{project}/_git/{repo}/pullrequest/{id}
   * - https://{org}.visualstudio.com/{project}/_git/{repo}/pullrequest/{id}
   * 
   * @param prUrl - Full PR URL from Azure DevOps
   * @returns Parsed PR information
   * @throws Error if URL format is invalid
   */
  private parsePrUrl(prUrl: string): PrInfo {
    const devAzureRegex =
      /https:\/\/dev\.azure\.com\/(.+?)\/(.+?)\/_git\/(.+?)\/pullrequest\/(\d+)/i;
    const visualStudioRegex =
      /https:\/\/(.+?)\.visualstudio\.com\/(.+?)\/_git\/(.+?)\/pullrequest\/(\d+)/i;
    const prInfo = this.parsePrUrlInternal(prUrl, devAzureRegex) || this.parsePrUrlInternal(prUrl, visualStudioRegex);

    if (!prInfo) throw new Error('Invalid Azure DevOps PR URL format');

    return prInfo;
  }

  /**
   * Internal helper to parse PR URL with a specific regex pattern
   * 
   * @param prUrl - Full PR URL
   * @param pattern - Regex pattern to match
   * @returns Parsed PR info or undefined if no match
   */
  private parsePrUrlInternal(prUrl: string, pattern: RegExp): PrInfo | undefined {
    const match = prUrl.match(pattern);

    if (!match) return undefined;

    return {
      organization: match[1],
      project: match[2],
      repository: match[3],
      pullRequestId: parseInt(match[4], 10),
    };
  }

  /**
   * Check if a Git change is supported for processing
   * 
   * Only processes:
   * - Add or edit operations (not delete/rename/move)
   * - Blob files (not folders/trees)
   * - Items with valid paths and URLs
   * 
   * @param change - Git change object from Azure DevOps
   * @returns True if change should be processed
   */
  private isSupportedChange(change: GitChange): boolean {
    const supportedChangeTypes = ['add', 'edit'];
    return supportedChangeTypes.includes(change.changeType)
      && change.item.gitObjectType === 'blob'
      && change.item.path !== undefined
      && change.item.url !== undefined;
  }

  /**
   * Send an authenticated request to Azure DevOps API
   * 
   * Automatically includes authentication headers and handles errors.
   * 
   * @param url - API endpoint URL
   * @param errorMessage - Error message prefix for failures
   * @param options - Fetch options (method, headers, body, etc.)
   * @returns Response object
   * @throws Error if request fails
   */
  private async sendRequest(url: string, errorMessage: string = 'Error', options: RequestInit = {}): Promise<Response> {
    // Merge default auth headers with custom headers
    const headers = await this.getDefaultHeaders();
    options.headers = {
      ...headers,
      ...options.headers,
    };

    const response = await fetch(url, options);

    if (!response.ok) {
      throw new Error(`${errorMessage}: HTTP ${response.status}: ${response.statusText}`);
    }

    return response;
  }

  /**
   * Download file content from Azure DevOps blob storage
   * 
   * @param url - Blob URL from Azure DevOps
   * @returns File content as text
   */
  private async getBlobContent(url: string): Promise<string | undefined> {
    const headers = {
      'Accept': 'text/plain',
    };
    const fileResponse = await this.sendRequest(url, 'Failed to download blob content', { headers });
    return await fileResponse.text();
  }

  /**
   * Fetch pull request details from Azure DevOps
   * 
   * Validates that PR is active and has no merge conflicts.
   * 
   * @param url - PR details API URL
   * @returns Pull request details
   * @throws Error if PR is not active or has merge conflicts
   */
  private async getPrDetails(url: string): Promise<PullRequest> {
    const response = await this.sendRequest(url, 'Failed to get PR details');
    const prDetails = await response.json() as PullRequest;

    if (prDetails?.status !== "active") {
      throw new Error("The PR is not active.");
    }

    if (prDetails?.mergeStatus !== "succeeded") {
      throw new Error("The PR has merge conflict.");
    }

    return prDetails;
  }

  /**
   * Fetch all Git changes (commits) between branches
   * 
   * @param url - Diffs API URL
   * @returns Array of Git changes
   */
  private async getGitChanges(url: string): Promise<GitChange[]> {
    const response = await this.sendRequest(url, 'Failed to get git changes');
    const data = await response.json() as CommitDiffs;
    return data.changes || [];
  }

  /**
   * Generate a unified diff patch for a file change
   * 
   * Downloads both original and new versions of the file,
   * then generates a unified diff patch.
   * 
   * @param fileItem - Git item representing the changed file
   * @param baseUrl - Repository base URL
   * @returns File patch with path, content, and diff
   */
  private async getFilePatch(fileItem: GitItem, baseUrl: string): Promise<FilePatch> {
    const filePath = fileItem.path;
    let sourceContent: string | undefined;
    let newContent: string | undefined;

    // Download original file content (if exists)
    if (fileItem.originalObjectId) {
      const url = this.getBlobUrl(baseUrl, fileItem.originalObjectId);
      sourceContent = await this.getBlobContent(url);
    }

    // Download new file content (if exists)
    if (fileItem.objectId) {
      const url = this.getBlobUrl(baseUrl, fileItem.objectId);
      newContent = await this.getBlobContent(url);
    }

    // Generate unified diff patch
    const patch = createPatch(filePath, sourceContent || '', newContent || '');
    return { filePath, sourceContent, patch };
  }

  /**
   * Get all file changes in a pull request with unified diffs
   * 
   * This is the main exported function for fetching PR changes.
   * 
   * Process:
   * 1. Parse PR URL to extract organization, project, repo, and PR ID
   * 2. Fetch PR details to get source and target branches
   * 3. Fetch all Git changes (commits) between branches
   * 4. Filter to only supported changes (add/edit on blob files)
   * 5. Download file contents and generate unified diffs in parallel
   * 
   * @param prUrl - Full Azure DevOps PR URL
   * @returns Array of file patches with paths, content, and diffs
   * @throws Error if PR is invalid, has conflicts, or has no changes
   */
  public async getPrFileChanges(): Promise<FilePatch[]> {
    // Parse PR URL to extract components
    const { organization, project, repository, pullRequestId } = this.parsePrUrl(this.prUrl);
    const baseUrl = this.getBaseUrl(organization, project, repository);

    // Get PR details including source and target branches
    const prDetailsUrl = this.getPrDetailsUrl(baseUrl, pullRequestId);
    const prDetails = await this.getPrDetails(prDetailsUrl);
    const sourceBranch = encodeURIComponent(prDetails?.sourceRefName?.replace("refs/heads/", ""));
    const targetBranch = encodeURIComponent(prDetails?.targetRefName?.replace("refs/heads/", ""));

    if (!sourceBranch || !targetBranch) {
      throw new Error("Could not determine source or target branch from PR details.");
    }

    // Fetch all changes between branches
    const diffsUrl = this.getDiffsUrl(baseUrl, sourceBranch, targetBranch);
    const changes = await this.getGitChanges(diffsUrl);

    if (changes.length === 0) {
      throw new Error("No changed files found in this PR.");
    }

    // Filter to only process supported file types (add/edit on blob files)
    const fileItems = changes
      .filter(c => this.isSupportedChange(c))
      .map(c => c.item);

    if (fileItems.length === 0) {
      throw new Error("No supported code file found in this PR.");
    }

    // Download all files and generate diffs in parallel
    const getFileTasks = fileItems.map(f => this.getFilePatch(f, baseUrl));
    const fileChanges = await Promise.all(getFileTasks);
    return fileChanges;
  }

  /**
   * Post a comment to a pull request thread
   * 
   * Creates a new thread on a specific file at the specified line and offset.
   * The comment will be visible in the Azure DevOps PR interface.
   * 
   * @param options - Comment options including PR URL, comment text, file path, and position
   * @throws Error if comment posting fails
   */
  public async postPrComment(options: PrCommentOptions) {
    const { comment, filePath, rightFileStartLine, rightFileStartOffset, rightFileEndLine, rightFileEndOffset } = options;

    // Parse PR URL to extract components
    const { organization, project, repository, pullRequestId } = this.parsePrUrl(this.prUrl);
    const baseUrl = this.getBaseUrl(organization, project, repository);
    const threadUrl = this.getThreadUrl(baseUrl, pullRequestId);

    // Build thread object with comment and file position
    const thread: PrThread = {
      comments: [{ content: comment }],
      threadContext: {
        filePath,
        rightFileStart: {
          line: rightFileStartLine,
          offset: rightFileStartOffset
        },
        rightFileEnd: {
          line: rightFileEndLine,
          offset: rightFileEndOffset
        },
      }
    };

    // Post the thread to Azure DevOps
    await this.sendRequest(
      threadUrl,
      'Failed to create thread',
      {
        method: 'POST',
        body: JSON.stringify(thread)
      });
  }
}