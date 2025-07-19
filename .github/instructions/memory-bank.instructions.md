---
applyTo: "**"
---
# Memory Bank Instructions

## 1. Purpose & Importance

The Memory Bank is Smart Alarm's **knowledge repository** - a collection of continuously updated Markdown files that provide context, architectural decisions, and project state to GitHub Copilot. It serves as the **single source of truth** for project understanding, enabling consistent and informed AI-assisted development.

## 2. Memory Bank Structure

### Core Files (Hierarchical Context)
```
projectbrief.md          # Project charter and immutable scope
├── productContext.md    # User problems and business value
├── systemPatterns.md    # Architecture patterns and coding standards  
├── techContext.md       # Complete technology stack
└── activeContext.md     # Current sprint focus and recent changes
    └── progress.md      # Completed work and achievements log
```

### File Hierarchy & Inheritance
- **Higher-level files** provide foundational context that flows down
- **Lower-level files** inherit context and add specific details
- Each file serves a distinct purpose in the knowledge hierarchy
- Updates to higher-level files may require updates to dependent files

## 3. File-Specific Responsibilities

### `projectbrief.md` - The Project Charter
- **Purpose**: Immutable project mission, scope, and constraints
- **Contains**: Core objectives, technology decisions, deployment targets
- **Update Frequency**: Rarely - only for major scope changes
- **Inheritance**: Foundation for all other Memory Bank files

### `productContext.md` - The Business Why
- **Purpose**: User problems, market context, and business value proposition  
- **Contains**: Problem statements, user experience goals, business outcomes
- **Update Frequency**: Quarterly or when market focus changes
- **Inheritance**: Inherited by activeContext.md for feature prioritization

### `systemPatterns.md` - The How We Build
- **Purpose**: Architectural patterns, coding standards, and design decisions
- **Contains**: Clean Architecture rules, naming conventions, testing standards
- **Update Frequency**: When new patterns are established or standards change
- **Inheritance**: Governs all code generation and review instructions

### `techContext.md` - The Technology Stack
- **Purpose**: Complete inventory of technologies, frameworks, and tools
- **Contains**: Backend stack, databases, messaging, observability, deployment
- **Update Frequency**: When technologies are added, upgraded, or replaced
- **Inheritance**: Informs all infrastructure and deployment decisions

### `activeContext.md` - The Current Sprint
- **Purpose**: What's happening now - current development focus and recent changes
- **Contains**: Current sprint goals, active features, recent decisions, next steps
- **Update Frequency**: Continuously - updated with each significant change
- **Inheritance**: Most dynamic file, inherits from all others

### `progress.md` - The Achievement Log  
- **Purpose**: Historical record of completed work and major milestones
- **Contains**: Completed features, resolved technical debt, architecture evolution
- **Update Frequency**: After each completed milestone or sprint
- **Inheritance**: Inherits context from activeContext.md as work is completed

## 4. Update Workflow & Triggers

### Automatic Update Triggers
- **New Feature Completed**: Update activeContext.md and progress.md
- **Architecture Decision Made**: Update systemPatterns.md and document in ADR
- **Technology Added/Changed**: Update techContext.md
- **Major Milestone**: Update progress.md with achievements
- **Sprint Planning**: Update activeContext.md with new focus areas

### Manual Update Triggers  
- **Explicit Command**: `"update memory bank"` - triggers comprehensive review
- **Context Drift Detected**: When Copilot responses seem inconsistent with project state
- **Onboarding**: Before new team members start development
- **Release Planning**: Before major releases to ensure context accuracy

### Update Process
```bash
# 1. Analyze current state
@workspace analyze current context using memory bank

# 2. Identify inconsistencies  
@workspace compare memory bank with current codebase

# 3. Update specific files
@workspace update activeContext.md with recent changes
@workspace update progress.md with completed features

# 4. Validate consistency
@workspace verify memory bank consistency across all files
```

## 5. Integration with Development Workflow

### Planning Phase (`// mode: plan`)
```
1. Consult Memory Bank for current context
2. Review activeContext.md for current priorities
3. Check systemPatterns.md for architectural constraints
4. Plan implementation following established patterns
```

### Implementation Phase (`// mode: act`)
```
1. Generate code following systemPatterns.md guidelines
2. Use techContext.md for technology choices
3. Reference productContext.md for business requirements
4. Update activeContext.md with progress
```

### Completion Phase
```
1. Update activeContext.md with completed work
2. Move completed items to progress.md
3. Document any new patterns in systemPatterns.md
4. Update techContext.md if new technologies were used
```

## 6. Key Commands & Usage Patterns

### Analysis Commands
- `@workspace analyze current context using memory bank` - Get project understanding
- `@workspace compare memory bank with current codebase` - Find inconsistencies  
- `@workspace summarize technical debt from memory bank` - Review known issues

### Planning Commands
- `@workspace plan next steps using memory bank context` - Strategic planning
- `@workspace identify prerequisites using memory bank` - Dependency analysis
- `@workspace validate approach against system patterns` - Architecture compliance

### Update Commands
- `@workspace update memory bank` - Comprehensive update based on recent changes
- `@workspace update activeContext.md with [specific changes]` - Targeted update
- `@workspace sync progress.md with completed features` - Achievement logging

## 7. Quality Assurance

### Consistency Checks
- **Cross-Reference Validation**: Ensure information is consistent across files
- **Code-Context Alignment**: Verify Memory Bank reflects actual codebase state
- **Pattern Compliance**: Confirm systemPatterns.md matches actual implementation
- **Technology Inventory**: Validate techContext.md includes all current technologies

### Update Validation
```csharp
// Before major updates, validate:
- Does activeContext.md reflect current sprint priorities?
- Is techContext.md current with package.json/csproj dependencies?  
- Are completed features properly logged in progress.md?
- Do systemPatterns.md match actual code patterns?
```

## 8. Integration with GitHub Copilot Instructions

### Copilot Instruction References
All GitHub Copilot instructions should reference Memory Bank for context:

```markdown
## Context Sources
- Consult Memory Bank (`memory-bank/`) for project context
- Follow patterns established in `systemPatterns.md`
- Consider current priorities from `activeContext.md`  
- Use technology stack from `techContext.md`
```

### Instruction Updates
When updating Copilot instructions, ensure they:
- Reference current Memory Bank structure
- Include commands for accessing Memory Bank context
- Specify which Memory Bank files are most relevant
- Include update triggers for Memory Bank maintenance

## 9. Troubleshooting & Recovery

### Common Issues
- **Stale Context**: Memory Bank doesn't reflect current project state
- **Inconsistent Information**: Contradictory information across files
- **Missing Context**: New features not reflected in Memory Bank
- **Pattern Drift**: Code patterns diverge from documented standards

### Recovery Actions
```bash
# Full context refresh
@workspace analyze entire codebase and update memory bank

# Specific file updates  
@workspace update techContext.md based on current dependencies
@workspace update activeContext.md based on recent commits

# Consistency validation
@workspace validate memory bank consistency and fix conflicts
```

## 10. Best Practices

### Maintenance Schedule
- **Daily**: Update activeContext.md with work progress
- **Weekly**: Review and update progress.md with completed features
- **Monthly**: Validate techContext.md and systemPatterns.md currency
- **Quarterly**: Comprehensive review of all Memory Bank files

### Writing Guidelines
- **Be Specific**: Include concrete examples and implementation details
- **Stay Current**: Remove outdated information promptly
- **Cross-Reference**: Link related concepts across files
- **Use Context**: Provide enough context for autonomous AI development

### Integration Testing
- Regularly test Memory Bank effectiveness by:
  - Starting new features with only Memory Bank context
  - Validating Copilot generates code following documented patterns
  - Checking if generated code aligns with current architecture
  - Verifying consistency between Memory Bank and actual implementation

---

**Remember**: The Memory Bank is only valuable if it accurately reflects the current project state. Regular updates are essential for effective AI-assisted development.
