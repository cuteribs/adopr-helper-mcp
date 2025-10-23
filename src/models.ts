export interface PrInfo {
    organization: string;
    project: string;
    repository: string;
    pullRequestId: number;
}

export interface PullRequest {
    pullRequestId: number;
    status: string;
    mergeStatus: string;
    sourceRefName: string;
    targetRefName: string;
}

export interface CommitDiffs {
    changes: GitChange[];
}

export interface GitChange {
    item: GitItem;
    changeType: 'add' | 'edit' | 'delete' | 'rename' | 'move' | string;
}

export interface GitItem {
    objectId?: string;
    originalObjectId?: string;
    gitObjectType: 'blob' | 'tree' | string;
    commitId: string;
    path: string;
    isFolder?: true;
    url: string;
}

export interface FilePatch {
    filePath: string;
    sourceContent?: string;
    patch: string;
}

export interface PrThread {
    comments: PrComment[];
    threadContext: {
        filePath: string;
        rightFileStart?: { line: number; offset: number };
        rightFileEnd?: { line: number; offset: number };
    };
}

export interface PrComment {
    content: string;
}

export interface PrCommentOptions {
    prUrl: string;
    comment: string;
    filePath: string;
    rightFileStartLine: number;
    rightFileStartOffset: number;
    rightFileEndLine: number;
    rightFileEndOffset: number;
}