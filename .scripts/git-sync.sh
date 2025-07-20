#!/bin/bash
# Smart Alarm Git Sync Script - Bash Edition
# Automated Git synchronization script for Smart Alarm project
# Usage: ./git-sync.sh [commit-message] [--force]

set -euo pipefail  # Exit on error, undefined vars, pipe failures

# Global variables
readonly SCRIPT_NAME="git-sync.sh"
readonly DEFAULT_COMMIT_MSG="chore(sync): synchronize local changes with remote repository"
COMMIT_MESSAGE=""
FORCE_PUSH=false
ORIGINAL_REMOTE=""
GIT_EXECUTABLE=""

# Color codes for output
readonly RED='\033[0;31m'
readonly GREEN='\033[0;32m'
readonly YELLOW='\033[1;33m'
readonly BLUE='\033[0;34m'
readonly NC='\033[0m' # No Color

#======================================
# Helper Functions
#======================================

# Structured logging function
log_json() {
    local level="$1"
    local message="$2"
    local data="${3:-}"
    local timestamp
    timestamp=$(date -u +"%Y-%m-%dT%H:%M:%S.%3NZ" 2>/dev/null || date -u +"%Y-%m-%dT%H:%M:%SZ")

    local log_entry="{\"timestamp\":\"$timestamp\",\"level\":\"$level\",\"message\":\"$message\",\"script\":\"$SCRIPT_NAME\""

    if [[ -n "$data" ]]; then
        log_entry+=",\"data\":$data"
    fi

    log_entry+="}"
    echo "$log_entry"
}

# Colored console output for readability
log_console() {
    local level="$1"
    local message="$2"
    local color=""

    case "$level" in
        "Error") color="$RED" ;;
        "Warning") color="$YELLOW" ;;
        "Success") color="$GREEN" ;;
        "Info") color="$BLUE" ;;
        *) color="$NC" ;;
    esac

    echo -e "${color}[$level]${NC} $message" >&2
}

# Combined logging - JSON to stdout, colored to stderr
log_message() {
    local level="$1"
    local message="$2"
    local data="${3:-}"

    log_json "$level" "$message" "$data"
    log_console "$level" "$message"
}

# Check if current directory is a Git repository
check_git_repository() {
    if [[ ! -d ".git" ]]; then
        log_message "Error" "Current directory is not a Git repository"
        return 1
    fi
    return 0
}

# Find Git executable
find_git_executable() {
    local git_path

    # Try to find git in PATH
    if git_path=$(command -v git 2>/dev/null); then
        log_message "Info" "Git found in PATH: $git_path"
        echo "$git_path"
        return 0
    fi

    # Try common installation paths for different systems
    local common_paths=(
        "/usr/bin/git"
        "/usr/local/bin/git"
        "/opt/homebrew/bin/git"
        "/snap/bin/git"
        "/usr/local/git/bin/git"
    )

    for path in "${common_paths[@]}"; do
        if [[ -x "$path" ]]; then
            log_message "Warning" "Git not in PATH, but found at: $path"
            echo "$path"
            return 0
        fi
    done

    log_message "Error" "Git executable not found in PATH or common locations"
    return 1
}

# Execute Git command with error handling
execute_git_command() {
    local git_path="$1"
    shift
    local args=("$@")
    local output
    local exit_code

    log_message "Info" "Executing: git ${args[*]}"

    if output=$("$git_path" "${args[@]}" 2>&1); then
        exit_code=0
    else
        exit_code=$?
    fi

    if [[ $exit_code -eq 0 ]]; then
        echo "$output"
        return 0
    else
        log_message "Error" "Git command failed with exit code $exit_code" "{\"output\":\"$(echo "$output" | tr '\n' ' ')\"}"
        echo "$output" >&2
        return $exit_code
    fi
}

# Get commit message from various sources
get_commit_message() {
    local provided_message="$1"

    # Use provided message first
    if [[ -n "$provided_message" ]]; then
        log_message "Info" "Using provided commit message"
        echo "$provided_message"
        return 0
    fi

    # Try to read from commit message file
    if [[ -n "${GIT_COMMIT_MSG_FILE:-}" ]] && [[ -f "$GIT_COMMIT_MSG_FILE" ]]; then
        local file_content
        if file_content=$(cat "$GIT_COMMIT_MSG_FILE" 2>/dev/null) && [[ -n "${file_content// }" ]]; then
            log_message "Info" "Using commit message from file: $GIT_COMMIT_MSG_FILE"
            echo "$file_content"
            return 0
        else
            log_message "Warning" "Failed to read commit message file: $GIT_COMMIT_MSG_FILE"
        fi
    fi

    # Use default message
    log_message "Info" "Using default commit message"
    echo "$DEFAULT_COMMIT_MSG"
}

# Clear commit message file
clear_commit_message_file() {
    if [[ -n "${GIT_COMMIT_MSG_FILE:-}" ]] && [[ -f "$GIT_COMMIT_MSG_FILE" ]]; then
        if > "$GIT_COMMIT_MSG_FILE" 2>/dev/null; then
            log_message "Info" "Commit message file cleared successfully"
        else
            log_message "Warning" "Failed to clear commit message file: $GIT_COMMIT_MSG_FILE"
        fi
    fi
}

# Validate GitHub credentials
validate_github_credentials() {
    if [[ -z "${GITHUB_USERNAME:-}" ]] || [[ -z "${GITHUB_PAT:-}" ]]; then
        log_message "Error" "GitHub credentials not found. Set GITHUB_USERNAME and GITHUB_PAT environment variables."
        return 1
    fi
    return 0
}

# Parse command line arguments
parse_arguments() {
    while [[ $# -gt 0 ]]; do
        case "$1" in
            --force|-f)
                FORCE_PUSH=true
                shift
                ;;
            --help|-h)
                show_help
                exit 0
                ;;
            -*)
                log_message "Error" "Unknown option: $1"
                show_help
                exit 1
                ;;
            *)
                if [[ -z "$COMMIT_MESSAGE" ]]; then
                    COMMIT_MESSAGE="$1"
                else
                    log_message "Error" "Multiple commit messages provided"
                    show_help
                    exit 1
                fi
                shift
                ;;
        esac
    done
}

# Show help message
show_help() {
    cat << EOF
Usage: $0 [OPTIONS] [COMMIT_MESSAGE]

Smart Alarm Git Sync Script - Automated Git synchronization

OPTIONS:
    -f, --force     Force push after rebase (use with caution)
    -h, --help      Show this help message

ARGUMENTS:
    COMMIT_MESSAGE  Custom commit message (optional)

EXAMPLES:
    $0 "feat(api): add new alarm endpoint"
    $0 --force "fix(auth): resolve token validation issue"
    $0  # Uses commit message from file or default

ENVIRONMENT VARIABLES:
    GITHUB_USERNAME      GitHub username for authentication
    GITHUB_PAT          GitHub Personal Access Token
    GIT_COMMIT_MSG_FILE Git commit message file path (optional)

EOF
}

# Cleanup function for script termination
cleanup() {
    local exit_code=$?

    # Restore original remote URL if it was changed
    if [[ -n "$ORIGINAL_REMOTE" ]] && [[ -n "$GIT_EXECUTABLE" ]]; then
        if execute_git_command "$GIT_EXECUTABLE" remote set-url origin "$ORIGINAL_REMOTE" >/dev/null 2>&1; then
            log_message "Info" "Original remote URL restored"
        else
            log_message "Warning" "Failed to restore original remote URL"
        fi
    fi

    if [[ $exit_code -eq 0 ]]; then
        log_message "Success" "Git sync completed successfully"
    else
        log_message "Error" "Git sync failed with exit code $exit_code"
    fi

    exit $exit_code
}

#======================================
# Main Execution
#======================================

main() {
    # Set up signal handlers for cleanup
    trap cleanup EXIT
    trap 'exit 130' INT TERM

    log_message "Info" "Starting Smart Alarm Git Sync process"

    # Parse command line arguments
    parse_arguments "$@"

    # Validate prerequisites
    if ! check_git_repository; then
        exit 1
    fi

    if ! GIT_EXECUTABLE=$(find_git_executable); then
        exit 1
    fi

    if ! validate_github_credentials; then
        exit 1
    fi

    # Get commit message
    COMMIT_MESSAGE=$(get_commit_message "$COMMIT_MESSAGE")
    log_message "Info" "Commit message: $COMMIT_MESSAGE"

    # Store original remote URL for restoration
    if ! ORIGINAL_REMOTE=$(execute_git_command "$GIT_EXECUTABLE" remote get-url origin 2>/dev/null); then
        log_message "Warning" "Could not retrieve original remote URL"
        ORIGINAL_REMOTE=""
    fi

    local authenticated_remote="https://${GITHUB_USERNAME}:${GITHUB_PAT}@github.com/arbgjr/smart-alarm.git"

    # Check for unmerged files (conflicts)
    if execute_git_command "$GIT_EXECUTABLE" ls-files --unmerged >/dev/null 2>&1; then
        local unmerged_files
        unmerged_files=$(execute_git_command "$GIT_EXECUTABLE" ls-files --unmerged 2>/dev/null || echo "")
        if [[ -n "$unmerged_files" ]]; then
            log_message "Error" "Unresolved merge conflicts detected. Resolve conflicts and try again."
            exit 1
        fi
    fi

    # Check repository status
    local has_changes=false
    if execute_git_command "$GIT_EXECUTABLE" status --porcelain >/dev/null 2>&1; then
        local status_output
        status_output=$(execute_git_command "$GIT_EXECUTABLE" status --porcelain 2>/dev/null || echo "")
        if [[ -n "${status_output// }" ]]; then
            has_changes=true
        fi
    fi

    if [[ "$has_changes" == false ]]; then
        log_message "Info" "No changes to commit"

        # Still try to pull in case there are remote changes
        if execute_git_command "$GIT_EXECUTABLE" pull origin main --rebase >/dev/null 2>&1; then
            log_message "Success" "Repository up to date"
        else
            log_message "Warning" "Failed to pull latest changes"
        fi
        exit 0
    fi

    # Configure authenticated remote temporarily
    if ! execute_git_command "$GIT_EXECUTABLE" remote set-url origin "$authenticated_remote" >/dev/null; then
        log_message "Error" "Failed to configure authenticated remote"
        exit 1
    fi

    # Add all changes
    if ! execute_git_command "$GIT_EXECUTABLE" add . >/dev/null; then
        log_message "Error" "Failed to add files to staging area"
        exit 1
    fi
    log_message "Success" "Files added to staging area"

    # Commit changes
    local commit_output
    if commit_output=$(execute_git_command "$GIT_EXECUTABLE" commit -m "$COMMIT_MESSAGE" 2>&1); then
        log_message "Success" "Changes committed successfully"
    elif [[ $? -eq 1 ]]; then
        log_message "Info" "Nothing to commit"
    else
        log_message "Error" "Failed to commit changes"
        exit 1
    fi

    # Pull with rebase
    if ! execute_git_command "$GIT_EXECUTABLE" pull origin main --rebase >/dev/null; then
        log_message "Error" "Failed to pull with rebase"
        exit 1
    fi
    log_message "Success" "Rebase completed successfully"

    # Push changes
    local push_args=(push)
    if [[ "$FORCE_PUSH" == true ]]; then
        push_args+=(--force-with-lease)
        log_message "Warning" "Using force push with lease"
    fi

    if ! execute_git_command "$GIT_EXECUTABLE" "${push_args[@]}" >/dev/null; then
        log_message "Error" "Failed to push changes"
        exit 1
    fi
    log_message "Success" "Changes pushed to remote successfully"

    # Clear commit message file on success
    clear_commit_message_file
}

# Execute main function with all arguments
main "$@"
