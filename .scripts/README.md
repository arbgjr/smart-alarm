# Git Sync Scripts

Automated Git synchronization scripts for the Smart Alarm project. These scripts handle the complete workflow of adding, committing, pulling with rebase, and pushing changes to the remote repository.

## Available Scripts

### PowerShell Version (`git-sync.ps1`)

Cross-platform PowerShell script that works on Windows, Linux, and macOS.

#### Usage

```powershell
# Basic usage with default commit message
.\git-sync.ps1

# With custom commit message
.\git-sync.ps1 -Message "feat(api): add new alarm endpoint"

# Force push after rebase (use with caution)
.\git-sync.ps1 -Message "fix(auth): resolve token validation" -Force
```

#### Parameters

- `-Message`: Custom commit message (optional)
- `-Force`: Force push with lease after rebase (optional, use with caution)

### Bash Version (`git-sync.sh`)

Pure Bash script optimized for Unix-like systems (Linux, macOS, WSL).

#### Usage

```bash
# Basic usage with default commit message
./git-sync.sh

# With custom commit message
./git-sync.sh "feat(api): add new alarm endpoint"

# Force push after rebase (use with caution)
./git-sync.sh --force "fix(auth): resolve token validation"

# Show help
./git-sync.sh --help
```

#### Options

- `--force, -f`: Force push with lease after rebase (use with caution)
- `--help, -h`: Show help message

## Prerequisites

### Required Environment Variables

Both scripts require the following environment variables:

```bash
# GitHub authentication
export GITHUB_USERNAME="your-github-username"
export GITHUB_PAT="your-personal-access-token"

# Optional: Custom commit message file
export GIT_COMMIT_MSG_FILE="path/to/commit-message.txt"
```

### System Requirements

- **Git**: Must be installed and accessible in PATH or common locations
- **PowerShell** (for .ps1 script): PowerShell Core 6+ recommended for cross-platform support
- **Bash** (for .sh script): Bash 4.0+ with standard Unix utilities

## Features

### Common Features

- ✅ **Comprehensive Error Handling**: Robust error detection and recovery
- ✅ **Structured Logging**: JSON-formatted logs with timestamps and context
- ✅ **Conflict Detection**: Checks for unmerged files before operations
- ✅ **Remote URL Management**: Temporarily uses authenticated URLs, restores originals
- ✅ **Flexible Commit Messages**: Supports parameter, file, or default messages
- ✅ **Status Validation**: Checks repository status before operations
- ✅ **Cleanup on Exit**: Always restores original state, even on failures

### Security Features

- 🔐 **Credential Protection**: Temporary use of authenticated URLs with automatic restoration
- 🔐 **Input Validation**: Validates all inputs and environment variables
- 🔐 **Safe Defaults**: Uses conservative options unless explicitly overridden

### Workflow

1. **Validation Phase**
   - Check Git repository validity
   - Locate Git executable
   - Validate GitHub credentials
   - Check for merge conflicts

2. **Preparation Phase**
   - Get commit message from various sources
   - Store original remote URL
   - Configure authenticated remote temporarily

3. **Synchronization Phase**
   - Add all changes to staging
   - Commit with provided/default message
   - Pull with rebase from remote
   - Push changes to remote

4. **Cleanup Phase**
   - Restore original remote URL
   - Clear commit message file (if successful)
   - Provide comprehensive status logging

## Error Handling

Both scripts include comprehensive error handling:

- **Git Conflicts**: Detects and reports unresolved merge conflicts
- **Network Issues**: Handles authentication and connection failures
- **Repository State**: Validates repository status and Git configuration
- **Credential Issues**: Clear error messages for missing or invalid credentials

## Examples

### Using with VS Code Tasks

Add to your `.vscode/tasks.json`:

```json
{
    "label": "Git Sync (PowerShell)",
    "type": "shell",
    "command": "pwsh",
    "args": ["-File", "${workspaceFolder}/.scripts/git-sync.ps1"],
    "group": "build",
    "presentation": {
        "echo": true,
        "reveal": "always",
        "focus": false,
        "panel": "shared"
    }
},
{
    "label": "Git Sync (Bash)",
    "type": "shell",
    "command": "${workspaceFolder}/.scripts/git-sync.sh",
    "group": "build",
    "presentation": {
        "echo": true,
        "reveal": "always",
        "focus": false,
        "panel": "shared"
    }
}
```

### Commit Message File Usage

Create a commit message file and set the environment variable:

```bash
echo "feat(alarm): implement snooze functionality" > commit-message.txt
export GIT_COMMIT_MSG_FILE="$(pwd)/commit-message.txt"
./git-sync.sh
```

### Conventional Commits

Both scripts work well with conventional commits:

```bash
# Features
./git-sync.sh "feat(api): add alarm deletion endpoint"

# Bug fixes
./git-sync.sh "fix(auth): resolve JWT token validation issue"

# Documentation
./git-sync.sh "docs(readme): update installation instructions"

# Refactoring
./git-sync.sh "refactor(service): extract alarm validation logic"
```

## Troubleshooting

### Common Issues

1. **Git not found**
   - Ensure Git is installed and in PATH
   - Check common installation directories

2. **Authentication failures**
   - Verify GITHUB_USERNAME and GITHUB_PAT are set correctly
   - Ensure PAT has appropriate repository permissions

3. **Merge conflicts**
   - Resolve conflicts manually using `git mergetool` or your editor
   - Complete the merge with `git commit`
   - Run the sync script again

4. **Permission denied**
   - Make sure the script is executable: `chmod +x git-sync.sh`
   - Check file permissions and ownership

### Debug Mode

Both scripts provide detailed logging. For additional debugging:

```bash
# PowerShell
$VerbosePreference = "Continue"
.\git-sync.ps1 -Verbose

# Bash
bash -x ./git-sync.sh "your commit message"
```

## Contributing

When modifying these scripts:

1. Maintain backward compatibility with existing environment variables
2. Follow the established error handling patterns
3. Update documentation for any new features or breaking changes
4. Test on multiple platforms before committing

## License

These scripts are part of the Smart Alarm project and follow the same licensing terms.
