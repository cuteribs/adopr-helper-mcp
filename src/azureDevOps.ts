import { createPatch } from 'diff';
import { CommitDiffs, FilePatch, GitChange, GitItem, PrCommentOptions, PrInfo, PrThread, PullRequest } from './models.js';
import { get } from 'http';

const API_VERSION = '7.1';
const PAT = process.env.AZURE_DEVOPS_PAT;

function getBaseUrl(organization: string, project: string, repository: string) {
  return `https://dev.azure.com/${organization}/${project}/_apis/git/repositories/${repository}`;
}

function getPrDetailsUrl(baseUrl: string, pullRequestId: number) {
  return `${baseUrl}/pullRequests/${pullRequestId}?api-version=${API_VERSION}`;
}

function getDiffsUrl(baseUrl: string, sourceBranch: string, targetBranch: string) {
  return `${baseUrl}/diffs/commits?baseVersion=${targetBranch}&targetVersion=${sourceBranch}&$top=2000&api-version=${API_VERSION}`;
}

function getBlobUrl(baseUrl: string, sha: string) {
  return `${baseUrl}/blobs/${sha}?api-version=${API_VERSION}`;
}

function getThreadUrl(baseUrl: string, pullRequestId: number) {
  return `${baseUrl}/pullRequests/${pullRequestId}/threads?api-version=${API_VERSION}`;
}

function getDefaultHeaders() {
  if (!PAT) {
    throw new Error('Azure DevOps PAT is not set in environment variables.');
  }

  return {
    'Authorization': `Basic ${btoa(`:${PAT}`)}`,
    'Content-Type': "application/json",
    'Accept': "application/json",
  };
};

async function sendRequest(url: string, errorMessage: string = 'Error', options: RequestInit = {}): Promise<Response> {
  options.headers = {
    ...getDefaultHeaders(),
    ...options.headers,
  };

  const response = await fetch(url, options);

  if (!response.ok) {
    console.error(options);
    throw new Error(`${errorMessage}: HTTP ${response.status}: ${response.statusText}`);
  }

  return response;
}

// Parse PR URL to extract organization, project, repository, and PR ID
function parsePrUrl(prUrl: string): PrInfo {
  const devAzureRegex =
    /https:\/\/dev\.azure\.com\/(.+?)\/(.+?)\/_git\/(.+?)\/pullrequest\/(\d+)/i;
  const visualStudioRegex =
    /https:\/\/(.+?)\.visualstudio\.com\/(.+?)\/_git\/(.+?)\/pullrequest\/(\d+)/i;
  const prInfo = parsePrUrlInternal(prUrl, devAzureRegex) || parsePrUrlInternal(prUrl, visualStudioRegex);

  if (!prInfo) throw new Error('Invalid Azure DevOps PR URL format');

  return prInfo;
}

function parsePrUrlInternal(prUrl: string, pattern: RegExp): PrInfo | undefined {
  const match = prUrl.match(pattern);

  if (!match) return undefined;

  return {
    organization: match[1],
    project: match[2],
    repository: match[3],
    pullRequestId: parseInt(match[4], 10),
  };
}

// Check if a change is supported (only add/edit operations on blob files)
function isSupportedChange(change: GitChange): boolean {
  const supportedChangeTypes = ['add', 'edit'];
  return supportedChangeTypes.includes(change.changeType)
    && change.item.gitObjectType === 'blob'
    && change.item.path !== undefined
    && change.item.url !== undefined;
}

// Get file content from blob URL
async function getBlobContent(url: string): Promise<string | undefined> {
  const headers = {
    'Accept': 'text/plain',
  };
  const fileResponse = await sendRequest(url, 'Failed to download blob content', { headers });
  return await fileResponse.text();
}

async function getPrDetails(url: string): Promise<PullRequest> {
  const response = await sendRequest(url, 'Failed to get PR details');
  const prDetails = await response.json() as PullRequest;

  if (prDetails?.status !== "active") {
    throw new Error("The PR is not active.");
  }

  if (prDetails?.mergeStatus !== "succeeded") {
    throw new Error("The PR has merge conflict.");
  }

  return prDetails;
}

async function getGitChanges(url: string): Promise<GitChange[]> {
  const response = await sendRequest(url, 'Failed to get git changes');
  const data = await response.json() as CommitDiffs;
  return data.changes || [];
}

async function getFilePatch(fileItem: GitItem, baseUrl: string): Promise<FilePatch> {
  const filePath = fileItem.path;
  let sourceContent: string | undefined;
  let newContent: string | undefined;

  if (fileItem.originalObjectId) {
    const url = getBlobUrl(baseUrl, fileItem.originalObjectId);
    sourceContent = await getBlobContent(url);
  }

  if (fileItem.objectId) {
    const url = getBlobUrl(baseUrl, fileItem.objectId);
    newContent = await getBlobContent(url);
  }

  const patch = createPatch(filePath, sourceContent || '', newContent || '');
  return { filePath, sourceContent, patch };
}

// Get all file changes in a PR
export async function getPrFileChanges(prUrl: string): Promise<FilePatch[]> {
  const { organization, project, repository, pullRequestId } = parsePrUrl(prUrl);
  const baseUrl = getBaseUrl(organization, project, repository);

  // Get PR details
  const prDetailsUrl = getPrDetailsUrl(baseUrl, pullRequestId);
  const prDetails = await getPrDetails(prDetailsUrl);
  const sourceBranch = encodeURIComponent(prDetails?.sourceRefName?.replace("refs/heads/", ""));
  const targetBranch = encodeURIComponent(prDetails?.targetRefName?.replace("refs/heads/", ""));

  if (!sourceBranch || !targetBranch) {
    throw new Error("Could not determine source or target branch from PR details.");
  }

  const diffsUrl = getDiffsUrl(baseUrl, sourceBranch, targetBranch);
  const changes = await getGitChanges(diffsUrl);

  if (changes.length === 0) {
    throw new Error("No changed files found in this PR.");
  }

  // Filter supported files
  const fileItems = changes
    .filter(c => isSupportedChange(c))
    .map(c => c.item);

  if (fileItems.length === 0) {
    throw new Error("No supported code file found in this PR.");
  }

  // Download files in parallel with concurrency limit (moved to utils)
  const getFileTasks = fileItems.map(f => getFilePatch(f, baseUrl));
  const fileChanges = await Promise.all(getFileTasks);
  return fileChanges;
}

// Post a comment to a PR
export async function postPrComment(options: PrCommentOptions) {
  const { prUrl, comment, filePath, rightFileStartLine, rightFileStartOffset, rightFileEndLine, rightFileEndOffset } = options;
  const { organization, project, repository, pullRequestId } = parsePrUrl(prUrl);
  const baseUrl = getBaseUrl(organization, project, repository);
  const threadUrl = getThreadUrl(baseUrl, pullRequestId);

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

  await sendRequest(
    threadUrl,
    'Failed to create thread',
    {
      method: 'POST',
      body: JSON.stringify(thread)
    });
}
