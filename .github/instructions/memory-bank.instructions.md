---
applyTo: "**"
---

# Memory Bank System

The Memory Bank maintains context between sessions in hierarchically organized Markdown files.

## Hierarchy and Flow

```
projectbrief.md → fundamental base
    ↓
productContext.md  systemPatterns.md  techContext.md
    ↓                   ↓                 ↓
           activeContext.md → current work
                  ↓
             progress.md → status
```

- Information flows from top to bottom
- Upper-level files inform the lower ones
- Conflicts are resolved by prioritizing higher-level files

## Main Files

1. **projectbrief.md**

   - Fundamental requirements and objectives
   - Source of truth for the scope

2. **productContext.md**

   - Project purpose
   - Problems to solve
   - Desired user experience

3. **systemPatterns.md**

   - Architecture
   - Technical decisions
   - Design patterns
   - Component relationships

4. **techContext.md**

   - Technologies used
   - Configurations
   - Dependencies

5. **activeContext.md**

   - Current focus
   - Recent changes
   - Next steps
   - Active decisions

6. **progress.md**
   - Completed features
   - Pending items
   - Current status
   - Known issues

## Workflow

1. **Start**: Consult the Memory Bank for context
2. **Plan**: Plan Mode (// mode: plan)
   - High-level strategies
   - Task breakdown
   - Prioritization
3. **Execute**: Act Mode (// mode: act)
   - Concrete implementation
   - Code generation
   - Testing
4. **Update**: Document changes in the Memory Bank

## Updates

Update when:

- New patterns are discovered
- Significant changes are implemented
- The **update memory bank** command is received
- Context clarification is needed

**Important**: In case of **update memory bank**, review ALL files, focusing on activeContext.md and progress.md.

## 🔗 Integration with Development Tools

### GitHub Copilot

The Memory Bank is integrated with GitHub Copilot through:

- **Automatic instructions**: Configured in `settings.json`
- **Specific prompt files**: For updating and consulting
- **Control commands**: "update memory bank" for updates

### VS Code Tasks

- **Consult Memory Bank**: View current status
- **Check Integration**: Validate references in files
- **Update Memory Bank**: Reminder to use the Copilot command

### Pull Requests

Automatic checklist verifies:

- Consistency with Memory Bank standards
- Need for update after changes
- Impact on project context

### Code Review

Reviewers should check:

- Alignment with `systemPatterns.md`
- Consistency with `activeContext.md`
- Need to update the Memory Bank

## 🎯 Specific Commands

### For Developers

```bash
# Check Memory Bank status
code-copilot: "analyze current context using memory bank"

# Request update
code-copilot: "update memory bank"

# Plan with context
code-copilot: "plan next steps using memory bank context"

# Check consistency
code-copilot: "verify code consistency with memory bank patterns"
```

### For GitHub Copilot

The system automatically responds to the following commands:

- **"update memory bank"**: Performs a full update
- **"// mode: plan"**: Enters planning mode
- **"// mode: act"**: Enters implementation mode
- **"analyze context"**: Analyzes current context
