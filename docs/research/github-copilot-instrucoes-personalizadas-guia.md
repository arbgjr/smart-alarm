# Custom Instructions Guide for GitHub Copilot

This comprehensive guide explains how to use custom instruction files with GitHub Copilot to improve code quality, enforce team standards, and boost productivity.

## Table of Contents

1. [Introduction](#introduction)
2. [Types of Custom Instruction Files](#types-of-custom-instruction-files)
   - [.github/copilot-instructions.md](#githubcopilot-instructionsmd)
   - [.instructions.md Files](#instructionsmd-files)
   - [.prompt.md Files](#promptmd-files)
3. [VS Code Settings for GitHub Copilot](#vs-code-settings-for-github-copilot)
4. [Best Practices and Examples](#best-practices-and-examples)
5. [References and Additional Resources](#references-and-additional-resources)

## Introduction

GitHub Copilot can provide answers better aligned with your team's practices, coding standards, and project requirements when it receives the right context. Instead of repeatedly providing this context in every prompt, you can create files that automatically add this information to your Copilot interactions.

Custom instructions help Copilot understand:
- Your coding style and conventions
- Project structure and architecture
- Specific libraries or frameworks you are using
- Team best practices and standards

## Types of Custom Instruction Files

### .github/copilot-instructions.md

This is a repository-wide instruction file that applies to all GitHub Copilot conversations related to your repository.

#### Purpose
- Establishes global coding standards and practices
- Automatically applies to all Copilot interactions in the repository
- Ensures consistency among team members

#### Setup
1. Create a `.github` folder at the root of your repository (if it doesn't exist)
2. Add a file named `copilot-instructions.md` inside this folder
3. Write your instructions in Markdown format

#### Example Content
```markdown
# Coding Standards

## General
- Use camelCase for variable and function names
- Use PascalCase for class names
- Use meaningful variable names that describe their purpose
- Keep functions small and focused on a single task
- Comment complex logic, but let clean code speak for itself

## JavaScript/TypeScript
- Use double quotes for strings
- Use semicolons at the end of statements
- Prefer const over let when variables won't be reassigned
- Use async/await for asynchronous code instead of promises

## Testing
- Write unit tests for all business logic
- Use Jest for testing
- Follow the AAA pattern (Arrange, Act, Assert)

## Error Handling
- Use try/catch blocks for error handling
- Prefer specific error types over generic ones
- Log errors with contextual information
```

#### How It Works
- Copilot automatically includes these instructions in the context for each chat request
- The instructions are not directly visible in the chat but influence Copilot's responses
- You can check if the instructions were used by reviewing the References list in a response

### .instructions.md Files

These are task-specific instruction files that can be configured to be included for specific files or folders.

#### Purpose
- Create targeted instructions for specific tasks or file types
- More granular control compared to repository-wide instructions
- Apply different standards to different parts of your code

#### Setup
1. Create `.instructions.md` files in relevant directories
2. Add the `applyTo` metadata at the top of the file to specify which files it applies to
3. Configure VS Code to use instruction files (settings covered later)

#### Example Content
```markdown
---
applyTo: "src/components/**"
---
# Standards for React Components

## Structure
- Use functional components with hooks
- Organize imports: React first, then hooks, then components, then styles
- Extract complex UI logic into custom hooks
- Use prop-types or TypeScript interfaces for all props

## State Management
- Use useState for local component state
- Use useContext for shared state between components
- Avoid prop drilling

## Styling
- Use CSS modules for component styling
- Follow the BEM naming convention
- Keep component-specific styles alongside the component
```

### .prompt.md Files

Prompt files let you save common prompt instructions in Markdown files that you can reuse in conversations.

#### Purpose
- Store reusable prompt templates
- Include context you frequently need
- Share common prompts with your team

#### Setup
1. Enable prompt files in VS Code settings
2. Create files with the `.prompt.md` extension
3. Store them in a designated folder (commonly `.github/prompts`)

#### Example Content
```markdown
# React Form Component Generator

Your goal is to generate a new React form component.

Requirements:
- Use Formik for form state management
- Use Yup for validation
- Include field validation
- Show validation errors below each field
- Support the following field types: text, email, password, select, checkbox
- Create a responsive layout

The component should:
1. Accept a configuration object for fields
2. Handle form submission
3. Show loading state during submission
4. Show success/error messages after submission
```

#### How to Use
1. In Copilot Chat, click the Attach Context icon (paperclip)
2. Choose "Prompt..." and select your prompt file
3. Optionally add specific details for your current request
4. Send the chat prompt

## VS Code Settings for GitHub Copilot

You can customize Copilot's behavior through VS Code settings. Here are the main settings:

### General Settings
```json
{
  "github.copilot.enable": true,
  "github.copilot.editor.enableCodeActions": true,
  "github.copilot.renameSuggestions.triggerAutomatically": true,
  "chat.commandCenter.enabled": true,
  "github.copilot.chat.followUps": true
}
```

### Instruction File Settings
```json
{
  "chat.promptFiles": true,
  "chat.promptFilesLocations": [
    ".github/prompts"
  ]
}
```

### Task-Specific Custom Instructions
```json
{
  "github.copilot.chat.codeGeneration.instructions": [
    {
      "text": "Follow our coding standards including proper error handling, logging, and test coverage."
    },
    {
      "file": "./docs/coding-standards.md"
    }
  ],
  "github.copilot.chat.testGeneration.instructions": [
    {
      "text": "Generate tests using Jest. Include the happy path, edge cases, and error scenarios."
    }
  ],
  "github.copilot.chat.codeReview.instructions": [
    {
      "text": "Review code for performance issues, security vulnerabilities, and adherence to our coding standards."
    }
  ],
  "github.copilot.chat.commitMessageGeneration.instructions": [
    {
      "text": "Follow the conventional commits format. Be descriptive about what changed and why."
    }
  ],
  "github.copilot.chat.pullRequestDescriptionGeneration.instructions": [
    {
      "text": "Include context, changes made, tests performed, and any pending issues."
    }
  ]
}
```

## Best Practices and Examples

### General Best Practices

1. **Keep instructions concise and specific**
   - Focus on concrete rules instead of vague preferences
   - Avoid overly complex or lengthy instructions

2. **Start with high-priority rules**
   - Put the most important guidelines first
   - Group related instructions

3. **Avoid instructions that won't work**
   These types of instructions generally don't work well:
   - References to external resources
   - Instructions about response tone or style
   - Requests for specific sizes or response formats

4. **Check if instructions are working**
   - Check response references to see if your instruction files are being used
   - Test with simple prompts to confirm instructions are being applied

### Example: Team Coding Standards

```markdown
# Team Coding Standards

## Naming Conventions
- Use camelCase for variables and functions
- Use PascalCase for classes and components
- Use UPPER_SNAKE_CASE for constants
- Prefix private methods with underscore
- Use descriptive names that explain the purpose

## JavaScript Guidelines
- Use ES6+ features where appropriate
- Prefer const over let, avoid var
- Use arrow functions for callbacks
- Use template literals instead of string concatenation
- Use destructuring for objects and arrays

## React Guidelines
- Use functional components with hooks
- Extract reusable logic into custom hooks
- Keep components small and focused
- Use prop-types or TypeScript for type checking
- Follow the presentational/container component pattern

## Testing
- Write tests for all business logic
- Test components for proper rendering and interactions
- Use mocks for external dependencies
- Name tests descriptively using the "should..." format
```

### Example: SonarQube Rules as Instructions

```markdown
# SonarQube Rules

## Code Smells
- Keep cyclomatic complexity below 15
- Keep function length below 60 lines
- Avoid duplicate code blocks
- Limit parameters to 5 per function
- Remove unused variables and imports

## Bugs
- Always check for null before accessing properties
- Do not reassign parameters
- Close resources in finally blocks
- Do not catch generic exceptions
- Do not use floating point values in equality comparisons

## Security
- Validate all user inputs
- Do not hardcode credentials
- Use parameterized queries for database access
- Do not log sensitive information
- Use secure communication protocols
```

## References and Additional Resources

1. [GitHub Copilot Documentation](https://docs.github.com/en/copilot/customizing-copilot/adding-repository-custom-instructions-for-github-copilot)
2. [VS Code Custom Instructions Guide](https://code.visualstudio.com/blogs/2025/03/26/custom-instructions)
3. [Copilot Instructions Format](https://copilot-instructions.md/)
4. [How to Use copilot-instructions.md](https://medium.com/@a.shtaigmann/how-to-use-copilot-instructions-md-to-improve-github-copilot-suggestions-fcd71b7f787f)
5. [Mastering GitHub Copilot Custom Instructions](https://medium.com/@anil.goyal0057/mastering-github-copilot-custom-instructions-with-github-copilot-instructions-md-f353e5abf2b1)
6. [Rules for Better Code Generation](https://www.reddit.com/r/vibecoding/comments/1l0ynlv/rules_i_give_claude_to_get_better_code_curious/)
7. [Save Hours with Custom Instructions](https://www.linkedin.com/pulse/save-hours-giving-github-copilot-custom-instructions-code-raymon-s-j4tke/)
8. [VS Code Copilot Settings Reference](https://code.visualstudio.com/docs/copilot/reference/copilot-settings)

----

This guide is intended to help you get the most out of GitHub Copilot in your development workflow. By properly using custom instruction files, you can significantly improve the quality and relevance of Copilot's suggestions while enforcing coding standards in your team.
