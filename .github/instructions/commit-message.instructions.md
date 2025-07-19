---
applyTo: "**"
---
# Commit Message Instructions

## 1. Commit Message Structure & Standards

### Conventional Commits Format
All commit messages must follow the Conventional Commits specification for consistency and automation:

```
<type>(<scope>): <description>

[optional body]

[optional footer(s)]
```

### Example Commit Messages
```bash
# Feature implementation
feat(auth): implement JWT token revocation mechanism

Add Redis-based token blacklist to prevent reuse of compromised tokens.
Users can now log out securely, and administrators can revoke specific
tokens during security incidents.

- Add IJwtBlocklistService with Redis implementation
- Implement RevokeTokenCommand with MediatR handler
- Update authentication middleware to check blacklist
- Add comprehensive unit and integration tests

Closes #142
BREAKING CHANGE: Authentication now requires Redis dependency

# Bug fix
fix(alarm): resolve timezone conversion error in recurring alarms

Recurring alarms were using UTC instead of user's timezone when
calculating next occurrence, causing alarms to trigger at wrong times
for users in non-UTC timezones.

- Update RecurringAlarmService to use user's timezone
- Fix AlarmCalculator timezone conversion logic
- Add timezone tests for all supported regions

Fixes #156

# Documentation update
docs(api): update authentication flow documentation

Add comprehensive API documentation for new JWT revocation endpoints
and update existing auth flow diagrams to include token lifecycle.

# Performance improvement
perf(query): optimize alarm retrieval with database indexes

Add composite index on (user_id, next_execution_time) to improve
alarm query performance by 80% for users with many scheduled alarms.

- Add migration for new composite index
- Update AlarmRepository query to use new index
- Add performance tests for alarm retrieval

Improves query time from 150ms to 30ms for users with 100+ alarms

# Refactoring
refactor(domain): extract alarm notification logic to separate service

Move notification logic from AlarmEntity to dedicated AlarmNotificationService
to improve testability and separation of concerns.

- Create AlarmNotificationService in Application layer
- Update AlarmTriggeredEventHandler to use new service
- Maintain backward compatibility with existing notification flows

# Test improvement
test(integration): add comprehensive storage service integration tests

Add full test coverage for OciObjectStorageService including error
scenarios, retry logic, and fallback mechanisms.

- Test successful file upload/download operations
- Test network failure scenarios with retry policies
- Test authentication failure handling
- Test storage quota exceeded scenarios

Coverage improved from 75% to 95% for storage layer
```

## 2. Commit Types & Their Usage

### Primary Types (Most Common)

#### `feat`: New Features
```bash
# New user-facing functionality
feat(calendar): add Google Calendar integration support
feat(ui): implement dark mode toggle in settings page
feat(alarm): add snooze functionality for recurring alarms

# New developer/operational functionality  
feat(monitoring): add OpenTelemetry distributed tracing
feat(deploy): add automated rollback for failed deployments
```

#### `fix`: Bug Fixes
```bash
# User-facing bug fixes
fix(notification): resolve push notifications not sending on iOS
fix(auth): prevent infinite redirect loop on token expiration
fix(alarm): fix alarm not triggering during daylight saving time transition

# Technical bug fixes
fix(database): resolve connection pool exhaustion under high load
fix(memory): fix memory leak in background alarm processing service
```

#### `docs`: Documentation Changes
```bash
# API/User documentation
docs(api): update REST API documentation with new endpoints
docs(user): add troubleshooting guide for alarm setup

# Technical documentation  
docs(architecture): update system diagram with new microservices
docs(deploy): add containerization deployment guide
```

### Secondary Types (Architecture & Maintenance)

#### `refactor`: Code Restructuring
```bash
# Improve code structure without changing functionality
refactor(domain): extract common validation logic to base classes
refactor(api): consolidate duplicate error handling middleware
refactor(query): optimize LINQ queries for better performance
```

#### `perf`: Performance Improvements
```bash
# Measurable performance improvements
perf(database): add indexes to improve query performance by 60%
perf(memory): reduce memory allocation in alarm processing loop
perf(api): implement response caching for frequently accessed endpoints
```

#### `test`: Testing Improvements
```bash
# Add or improve tests without changing functionality
test(unit): add comprehensive tests for alarm calculation logic
test(integration): add end-to-end tests for user authentication flow
test(performance): add load tests for peak alarm processing periods
```

### Supporting Types (Infrastructure & Tooling)

#### `build`: Build System Changes
```bash
# Changes to build process, dependencies, or tools
build(docker): optimize Docker image size by 40%
build(deps): update Entity Framework Core to version 8.0.1
build(ci): add automated security scanning to GitHub Actions
```

#### `ci`: Continuous Integration Changes
```bash
# Changes to CI/CD pipelines or automation
ci(github): add PR template with checklist and review guidelines
ci(deploy): implement blue-green deployment strategy
ci(test): add parallel test execution to reduce build time
```

#### `chore`: Maintenance Tasks
```bash
# Routine maintenance that doesn't affect functionality
chore(deps): update development dependencies to latest versions
chore(config): update environment configuration for new dev setup
chore(cleanup): remove deprecated code and unused dependencies
```

## 3. Scope Guidelines

### Service-Based Scopes (Primary)
```bash
# Microservice-specific changes
feat(ai-service): add behavioral pattern analysis for alarm optimization
fix(alarm-service): resolve Hangfire job duplication issue
docs(integration-service): update external API integration documentation
```

### Layer-Based Scopes (Architecture)
```bash
# Clean Architecture layer changes
refactor(domain): consolidate alarm business rules in domain services
feat(application): implement new command/query handlers for user preferences
fix(infrastructure): resolve OCI Object Storage connection timeout issues
perf(api): optimize controller response serialization
```

### Feature-Based Scopes (Functional)
```bash
# User-facing feature areas
feat(auth): add multi-factor authentication support
fix(calendar): resolve sync issues with Microsoft Graph API
docs(notification): update push notification setup guide
test(upload): add comprehensive file upload validation tests
```

### Cross-Cutting Scopes (Technical)
```bash
# System-wide concerns
feat(observability): add structured logging with correlation IDs
fix(security): patch JWT token validation vulnerability
perf(caching): implement distributed caching with Redis
refactor(config): centralize configuration management across services
```

## 4. Description Guidelines

### Writing Effective Descriptions

#### Imperative Mood (Required)
```bash
# Correct: Use imperative mood (command form)
fix(auth): resolve session timeout issue
add(notification): implement push notification service
update(docs): revise deployment instructions

# Incorrect: Avoid past tense or present continuous
fix(auth): resolved session timeout issue      # Wrong: past tense
fix(auth): resolving session timeout issue    # Wrong: present continuous  
fix(auth): fixes session timeout issue        # Wrong: present tense
```

#### Length Limits
- **Subject line**: Maximum 72 characters
- **Body lines**: Wrap at 72 characters for readability
- **Focus**: One logical change per commit

#### Clarity Requirements
```bash
# Good: Specific and clear
fix(alarm): prevent duplicate notifications for recurring alarms

# Poor: Vague and unclear  
fix(alarm): fix bug
fix(alarm): update some logic
fix(alarm): make improvements
```

## 5. Body Content Standards

### When to Include a Body
Include a commit body when:
- The change is complex or non-obvious
- Multiple files are affected
- Business context is important
- Breaking changes are introduced
- Issue numbers need referencing

### Body Content Structure
```bash
feat(auth): implement OAuth 2.0 integration with external providers

Add support for Google and Microsoft OAuth 2.0 authentication to allow
users to log in with their existing accounts. This reduces friction in
the onboarding process and improves user experience.

Implementation details:
- Add OAuth 2.0 client registration and callback handling
- Implement user account linking for existing Smart Alarm users
- Add security validation for OAuth token exchange
- Update user registration flow to support external authentication

Technical considerations:
- OAuth tokens are stored securely in KeyVault
- User consent is required for calendar access permissions
- Fallback to traditional authentication remains available

Breaking changes:
- Authentication middleware now requires OAuth configuration
- User entity schema includes new external_provider_id field

Closes #89, #112
Resolves USER-AUTH-001
```

### Performance Impact Documentation
```bash
perf(query): optimize user alarm retrieval with database indexing

Add composite index on (user_id, next_execution_time, status) to improve
alarm query performance for users with large numbers of scheduled alarms.

Performance improvements:
- Query execution time: 180ms → 25ms (86% improvement)
- Database CPU utilization: 45% → 12% reduction  
- Memory usage: 15% reduction in query buffer allocation

Technical details:
- Added migration 20250112_AddAlarmQueryOptimizationIndex
- Updated AlarmRepository.GetUpcomingAlarms() to use new index
- Maintained backward compatibility with existing queries

Testing:
- Load tested with 10,000 alarms per user
- Verified performance improvement in all supported databases
- Added performance regression tests to CI pipeline
```

## 6. Footer Standards

### Issue References
```bash
# Link to issue numbers
Closes #123
Fixes #456  
Resolves #789

# Multiple issues
Closes #123, #124
Fixes #456

# External references
Resolves JIRA-TICKET-001
Addresses SECURITY-VULN-042
```

### Breaking Changes
```bash
# Document breaking changes clearly
BREAKING CHANGE: Authentication middleware now requires Redis configuration

The JWT blacklist feature requires Redis for distributed token storage.
Update your appsettings.json to include Redis connection string:

{
  "Redis": {
    "ConnectionString": "localhost:6379"
  }
}

Migration path:
1. Deploy Redis instance
2. Update configuration files  
3. Deploy application with new authentication middleware
4. Test token revocation functionality
```

### Co-authorship and Reviews
```bash
# Multiple contributors
Co-authored-by: Jane Developer <jane@smartalarm.dev>
Co-authored-by: Bob Reviewer <bob@smartalarm.dev>

# Code review acknowledgment
Reviewed-by: Senior Developer <senior@smartalarm.dev>
```

## 7. Commit Granularity & Atomicity

### Atomic Commit Principles
```bash
# Good: Single logical change
feat(auth): add JWT token revocation service

# Good: Related changes that belong together
fix(alarm): resolve timezone conversion in recurring alarm calculation

# Poor: Multiple unrelated changes
feat(auth): add token revocation and fix notification bug and update docs
```

### Commit Size Guidelines
- **Small commits**: Prefer smaller, focused commits over large ones
- **Logical units**: Each commit should be a complete logical unit of work
- **Reviewable**: Changes should be easily reviewable and understandable
- **Testable**: Each commit should maintain system functionality

### Work-in-Progress Commits
```bash
# Use WIP prefix for incomplete work (will be squashed later)
feat(wip): initial implementation of alarm notification service
feat(wip): add unit tests for notification service  
feat(wip): integrate notification service with alarm triggers

# Final squashed commit
feat(notification): implement comprehensive alarm notification service
```

## 8. Special Commit Scenarios

### Merge Commits
```bash
# Merge commit messages (automated)
Merge pull request #123 from feature/jwt-token-revocation

feat(auth): implement JWT token revocation mechanism
```

### Revert Commits
```bash
# Revert previous commits
revert: "feat(auth): implement OAuth 2.0 integration"

This reverts commit 1234567890abcdef due to security vulnerability
in OAuth token validation discovered in security audit.

Temporary rollback while security patch is developed.

Refs #234
```

### Hotfix Commits
```bash
# Critical production fixes
hotfix(security): patch JWT token validation vulnerability

Critical security fix for production deployment. JWT tokens were not
properly validating signature algorithms, allowing potential token
forgery attacks.

- Add explicit algorithm validation in JWT middleware
- Reject tokens with 'none' algorithm
- Add logging for suspicious token validation attempts

Security impact: HIGH
Deployment priority: IMMEDIATE

Fixes SECURITY-2025-001
```

### Documentation-Only Changes
```bash
# Pure documentation updates
docs(readme): update installation instructions for Docker setup

Add step-by-step Docker installation guide and troubleshooting section
for common Docker-related issues developers encounter during setup.

- Add Docker prerequisites and installation links
- Include sample docker-compose.yml configuration
- Add troubleshooting section for common port conflicts
- Update development workflow to include Docker commands
```

## 9. Automated Tooling Integration

### Commit Message Validation
The repository uses automated commit message validation to ensure consistency:

```bash
# These will pass validation
feat(auth): add multi-factor authentication support
fix(api): resolve rate limiting bypass vulnerability  
docs(deployment): update Kubernetes deployment guide

# These will fail validation  
Add new feature          # Missing type and scope
fix: bug                # Missing scope, vague description  
FEAT(auth): add MFA     # Wrong case for type
```

### Changelog Generation
Commit messages are used for automated changelog generation:
- `feat`: New features section
- `fix`: Bug fixes section  
- `BREAKING CHANGE`: Breaking changes section
- `perf`: Performance improvements section

### Semantic Versioning
Commit types determine version bumps:
- `feat`: Minor version bump (1.2.0 → 1.3.0)
- `fix`: Patch version bump (1.2.0 → 1.2.1)
- `BREAKING CHANGE`: Major version bump (1.2.0 → 2.0.0)

## 10. Quality Checklist

### Pre-Commit Validation
Before committing, ensure:
- [ ] Commit follows Conventional Commits format
- [ ] Type and scope are appropriate for the change
- [ ] Description is clear and under 72 characters
- [ ] Body explains "what" and "why", not just "how"
- [ ] Breaking changes are documented in footer
- [ ] Issue numbers are referenced where applicable
- [ ] Code change is atomic and focused on single concern
- [ ] All tests pass and code follows project standards

### Commit Message Review
- [ ] Message accurately describes the change made
- [ ] Technical details are explained in body when needed
- [ ] Business context is provided for user-facing changes
- [ ] Performance impacts are quantified where measurable
- [ ] Security implications are documented for security changes
- [ ] Deployment considerations are noted for infrastructure changes

---

**Remember**: Good commit messages are essential for project maintenance, debugging, and collaboration. They serve as a historical record of why changes were made and how the system evolved over time.
