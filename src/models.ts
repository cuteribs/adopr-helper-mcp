/**
 * Type definitions for Azure DevOps API models
 * 
 * These interfaces define the structure of data exchanged with
 * Azure DevOps REST API and internal data structures.
 */

export interface AuthOptions {
    type: 'interactive' | 'pat';
    token: string; // Token or credentials based on auth type
}

/**
 * Parsed pull request information from URL
 */
export interface PrInfo {
    organization: string;      // Azure DevOps organization name
    project: string;           // Project name
    repository: string;        // Repository name
    pullRequestId: number;     // PR ID number
}

/**
 * Pull request details from Azure DevOps API
 */
export interface PullRequest {
    pullRequestId: number;     // PR ID
    status: string;            // PR status (active, completed, abandoned)
    mergeStatus: string;       // Merge status (succeeded, conflicts, etc.)
    sourceRefName: string;     // Source branch ref (e.g., refs/heads/feature)
    targetRefName: string;     // Target branch ref (e.g., refs/heads/main)
}

/**
 * Response from Azure DevOps diffs/commits API
 */
export interface CommitDiffs {
    changes: GitChange[];      // Array of file changes
}

/**
 * A single file change in a commit
 */
export interface GitChange {
    item: GitItem;             // The changed file item
    changeType: 'add' | 'edit' | 'delete' | 'rename' | 'move' | string;  // Type of change
}

/**
 * Git item (file or folder) metadata
 */
export interface GitItem {
    objectId?: string;         // New version SHA (if exists)
    originalObjectId?: string; // Original version SHA (if exists)
    gitObjectType: 'blob' | 'tree' | string;  // Type: blob (file) or tree (folder)
    commitId: string;          // Commit SHA that includes this change
    path: string;              // File path relative to repo root
    isFolder?: true;           // True if this is a folder
    url: string;               // API URL to fetch this item
}

/**
 * File patch with unified diff
 */
export interface FilePatch {
    filePath: string;          // Path to the file
    sourceContent?: string;    // Original file content (optional)
    patch: string;             // Unified diff patch
}

/**
 * Pull request thread structure for posting comments
 */
export interface PrThread {
    comments: PrComment[];     // Array of comments in the thread
    threadContext: {           // Context for where to place the thread
        filePath: string;                                        // File path
        rightFileStart?: { line: number; offset: number };      // Start position
        rightFileEnd?: { line: number; offset: number };        // End position
    };
}

/**
 * A single comment in a PR thread
 */
export interface PrComment {
    content: string;           // Comment text (supports Markdown)
}

/**
 * Options for posting a PR comment
 */
export interface PrCommentOptions {
    prUrl: string;             // Full PR URL
    comment: string;           // Comment text to post
    filePath: string;          // File to attach comment to
    rightFileStartLine: number;      // Start line number
    rightFileStartOffset: number;    // Offset in start line
    rightFileEndLine: number;        // End line number
    rightFileEndOffset: number;      // Offset in end line
}