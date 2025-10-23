/**
 * Interactive test script for VS Code
 * Run with: node test-interactive.js
 */

import readline from 'readline';
import { getPrChanges, postPrComment } from './dist/azureDevOps.js';

const rl = readline.createInterface({
  input: process.stdin,
  output: process.stdout
});

function question(prompt) {
  return new Promise((resolve) => rl.question(prompt, resolve));
}

async function main() {
  console.log('═══════════════════════════════════════════════════════════');
  console.log('  Azure DevOps PR Helper MCP - Interactive Test');
  console.log('═══════════════════════════════════════════════════════════\n');

  // Check for PAT
  const pat = process.env.AZURE_DEVOPS_PAT;
  if (!pat) {
    console.error('❌ Error: AZURE_DEVOPS_PAT environment variable not set');
    console.log('\nPlease set it first:');
    console.log('  Windows CMD:  set AZURE_DEVOPS_PAT=your-pat-here');
    console.log('  PowerShell:   $env:AZURE_DEVOPS_PAT="your-pat-here"');
    console.log('  Bash:         export AZURE_DEVOPS_PAT="your-pat-here"\n');
    rl.close();
    return;
  }

  console.log('✓ PAT found in environment\n');

  // Main menu
  while (true) {
    console.log('\nWhat would you like to test?');
    console.log('  1. Get PR changes (fetch file diffs)');
    console.log('  2. Post a comment to PR');
    console.log('  3. Exit\n');

    const choice = await question('Enter choice (1-3): ');

    if (choice === '1') {
      await testGetPrChanges(pat);
    } else if (choice === '2') {
      await testPostComment(pat);
    } else if (choice === '3') {
      console.log('\nGoodbye! 👋\n');
      break;
    } else {
      console.log('Invalid choice. Please try again.');
    }
  }

  rl.close();
}

async function testGetPrChanges(pat) {
  console.log('\n─────────────────────────────────────────────────────────');
  console.log('TEST: Get PR Changes');
  console.log('─────────────────────────────────────────────────────────\n');

  const prUrl = await question('Enter PR URL: ');
  
  if (!prUrl.trim()) {
    console.log('❌ No URL provided');
    return;
  }

  console.log('\n⏳ Fetching PR changes...\n');

  try {
    const changes = await getPrChanges(prUrl.trim(), pat);
    
    console.log('✅ Success!\n');
    console.log(`📊 Total files changed: ${changes.length}\n`);
    
    if (changes.length > 0) {
      console.log('Files:');
      changes.forEach((change, index) => {
        console.log(`  ${index + 1}. ${change.path}`);
        console.log(`     Change type: ${change.changeType}`);
        console.log(`     Diff lines: ${change.diff.split('\n').length}`);
      });
      
      const showDiff = await question('\nShow diff for which file? (number or skip): ');
      const fileIndex = parseInt(showDiff) - 1;
      
      if (!isNaN(fileIndex) && fileIndex >= 0 && fileIndex < changes.length) {
        console.log('\n─────────────────────────────────────────────────────────');
        console.log(`Diff for: ${changes[fileIndex].path}`);
        console.log('─────────────────────────────────────────────────────────\n');
        console.log(changes[fileIndex].diff);
      }
    } else {
      console.log('ℹ️  No file changes found');
    }
    
  } catch (error) {
    console.error('❌ Error:', error.message);
    if (error.stack) {
      console.error('\nStack trace:', error.stack);
    }
  }
}

async function testPostComment(pat) {
  console.log('\n─────────────────────────────────────────────────────────');
  console.log('TEST: Post Comment to PR');
  console.log('─────────────────────────────────────────────────────────\n');

  const prUrl = await question('Enter PR URL: ');
  
  if (!prUrl.trim()) {
    console.log('❌ No URL provided');
    return;
  }

  const comment = await question('Enter comment text: ');
  
  if (!comment.trim()) {
    console.log('❌ No comment provided');
    return;
  }

  const filePath = await question('File path (optional, press Enter to skip): ');
  const lineNum = await question('Line number (optional, press Enter to skip): ');
  const threadId = await question('Thread ID to reply to (optional, press Enter to skip): ');

  console.log('\n⏳ Posting comment...\n');

  try {
    const result = await postPrComment({
      prUrl: prUrl.trim(),
      pat,
      comment: comment.trim(),
      filePath: filePath.trim() || undefined,
      lineNumber: lineNum.trim() ? parseInt(lineNum.trim()) : undefined,
      threadId: threadId.trim() ? parseInt(threadId.trim()) : undefined,
    });
    
    console.log('✅ Success!\n');
    console.log(`Comment ID: ${result.id}`);
    console.log(`Content: ${result.content}\n`);
    
  } catch (error) {
    console.error('❌ Error:', error.message);
    if (error.stack) {
      console.error('\nStack trace:', error.stack);
    }
  }
}

// Run
main().catch((error) => {
  console.error('Fatal error:', error);
  process.exit(1);
});
