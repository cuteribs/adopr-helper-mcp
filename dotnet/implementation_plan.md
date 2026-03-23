# Implementation Plan: MCP Integration for ai-pr-reviewer

## Overview
This plan outlines the modifications needed to align the Azure DevOps MCP server with the requirements specified in `mcp-integration.md` for the ai-pr-reviewer skill.

## Current State
The MCP server currently:
- ✅ Has `get_pr_changes` tool that fetches PR file changes with diffs
- ✅ Has `post_pr_comment` tool to post comments to specific file locations
- ✅ Uses Azure DevOps REST API for authentication and data fetching
- ❌ Returns full file contents and diffs in the response (bloats context)
- ❌ Does not save files to disk with escaped paths
- ❌ Does not create a manifest.json with metadata
- ❌ Tool names don't match expected convention (`azure_devops_fetch_pr_changes` vs `get_pr_changes`)
- ❌ Comment posting tool doesn't support severity and thread_status parameters

## Required Changes

### 1. Rename and Update `azure_devops_fetch_pr_changes` Tool
**Current:** Tool is named `get_pr_changes`  
**Required:** Tool should be named `azure_devops_fetch_pr_changes`

- [ ] Rename tool from `get_pr_changes` to `azure_devops_fetch_pr_changes` in `McpTools.cs`
- [ ] Update input schema to match specification:
  - [ ] Change `prUrl` parameter to `pr_url` (snake_case)
  - [ ] Add `output_folder` parameter (required string)
- [ ] Update tool description to reflect file-saving behavior
- [ ] Modify return value to be a small summary instead of full content

### 2. Implement File System Storage Logic
**Current:** Returns all content in memory  
**Required:** Write files to disk with escaped paths

- [ ] Create new model `ManifestFile` to represent file metadata
- [ ] Create new model `FetchPrResponse` for the tool response
- [ ] Create new model `ManifestData` for manifest.json structure
- [ ] Implement path escaping logic: replace `/` and `\` with `~~~`
- [ ] Update `AzureDevOpsHelper.GetPrFileChangesAsync()` to:
  - [ ] Accept `outputFolder` parameter
  - [ ] Fetch PR metadata (title, author, status, branches, timestamps)
  - [ ] For each changed file:
    - [ ] Escape the file path using `~~~` separator
    - [ ] Write full file content to `{escaped_name}`
    - [ ] Write diff to `{escaped_name}.diff`
  - [ ] Create `manifest.json` with all metadata
  - [ ] Return small summary with file count and stats

### 3. Update Manifest Structure
**Required:** Create comprehensive manifest.json

- [ ] Add PR metadata fields:
  - [ ] `pr_url`, `pr_id`, `pr_title`, `pr_description`
  - [ ] `pr_author` (with `display_name` and `email`)
  - [ ] `pr_status`, `source_branch`, `target_branch`
  - [ ] `created_date`, `fetch_timestamp`
- [ ] Add statistics section:
  - [ ] `total_files`, `total_size_bytes`
  - [ ] `changes` breakdown: `added`, `modified`, `deleted`, `renamed`
- [ ] Add files array with per-file metadata:
  - [ ] `original_path`, `escaped_name`, `diff_name`
  - [ ] `change_type`, `size_bytes`, `diff_size_bytes`
  - [ ] `lines_added`, `lines_deleted`

### 4. Rename and Update `azure_devops_post_comment` Tool
**Current:** Tool is named `post_pr_comment`  
**Required:** Tool should be named `azure_devops_post_comment`

- [ ] Rename tool from `post_pr_comment` to `azure_devops_post_comment` in `McpTools.cs`
- [ ] Update input schema to match specification:
  - [ ] Change all parameters to snake_case (`prUrl` → `pr_url`, `filePath` → `file_path`, etc.)
  - [ ] Simplify line parameters to just `line_number` (single integer)
  - [ ] Add `comment_text` parameter (rename from `comment`)
  - [ ] Add `severity` parameter (optional: Critical, High, Medium, Low)
  - [ ] Add `thread_status` parameter (optional: active, fixed, wontFix, closed)
- [ ] Update `PrCommentOptions` model to match new schema
- [ ] Remove offset-based parameters (rightFileStartOffset, rightFileEndOffset, etc.)
- [ ] Update tool description

### 5. Enhance Comment Formatting
**Required:** Support structured comment format

- [ ] Update `PostPrCommentAsync` to format comments with:
  - [ ] Severity badge (e.g., `**[Critical]**`)
  - [ ] Brief issue title
  - [ ] Detailed explanation
  - [ ] Suggestion section
  - [ ] Reference section (if applicable)
- [ ] Map severity to thread properties if Azure DevOps API supports it

### 6. Error Handling Enhancement
**Required:** Return standardized error responses

- [ ] Create error response model with `success`, `error.code`, `error.message`
- [ ] Update tool handlers to return structured errors:
  - [ ] `PR_NOT_FOUND` - PR doesn't exist or no access
  - [ ] `AUTH_FAILED` - Authentication failed
  - [ ] `FILE_NOT_FOUND` - File doesn't exist in PR
  - [ ] `COMMENT_FAILED` - Failed to post comment
  - [ ] `RATE_LIMITED` - Too many requests
- [ ] Wrap HTTP exceptions and provide meaningful error codes

### 7. Update Documentation
**Required:** Reflect new tool behavior

- [ ] Update README.md:
  - [ ] Change tool names to match new convention
  - [ ] Document file system behavior
  - [ ] Update parameter examples to use snake_case
  - [ ] Add severity and thread_status documentation
  - [ ] Add manifest.json structure documentation
  - [ ] Update example usage

### 8. Testing Considerations
**Required:** Validate new behavior

- [ ] Test path escaping with various file paths (nested directories, special chars)
- [ ] Test file writing with large PRs
- [ ] Test manifest.json generation with complete metadata
- [ ] Test comment posting with severity and thread_status
- [ ] Test error scenarios (auth failure, missing PR, etc.)
- [ ] Verify small response size (no file contents in context)

## Implementation Order

1. **Phase 1: Data Models** (Steps 2, 3)
   - Create new models for manifest, responses, and file metadata
   
2. **Phase 2: Core Logic** (Steps 2, 3)
   - Implement path escaping
   - Implement file writing logic
   - Update `GetPrFileChangesAsync` to save files and create manifest
   
3. **Phase 3: Tool Updates** (Steps 1, 4, 5)
   - Rename tools to match specification
   - Update input schemas to snake_case
   - Simplify comment posting parameters
   - Add severity and thread_status support
   
4. **Phase 4: Error Handling** (Step 6)
   - Implement standardized error responses
   
5. **Phase 5: Documentation & Testing** (Steps 7, 8)
   - Update README
   - Manual testing with real PRs

## Notes

- The MCP server will require write access to the `output_folder` path
- Path escaping must handle both Windows (`\`) and Unix (`/`) separators
- Large PRs may take time to download all files - consider adding progress logging to stderr
- The manifest structure provides rich metadata for the AI reviewer skill to use without loading full files
- Comment formatting should be handled by the AI skill, but the MCP can provide helper formatting if needed

## Success Criteria

- ✅ Tool names match specification exactly
- ✅ Files are written to disk with escaped paths
- ✅ Manifest.json contains all required metadata
- ✅ Tool responses are small (< 1KB typical)
- ✅ Comment posting supports severity and thread_status
- ✅ Error responses follow standardized format
- ✅ Documentation is complete and accurate
- ✅ ai-pr-reviewer skill can successfully use the MCP tools
