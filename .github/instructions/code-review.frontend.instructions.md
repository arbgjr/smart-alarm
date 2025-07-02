---
applyTo: "**/*.{ts,tsx,js,jsx,css,scss,less,html,json,md}"
---
# 🧐 Code Review Instructions – Frontend (React/TypeScript)

## General Principles
- Review for security, maintainability, accessibility, and performance.
- Enforce Atomic Design and separation of UI, logic, and state.
- Check for error handling, validation, and accessibility (WCAG).
- Ensure code is well documented and testable.
- Validate naming conventions and project organization.
- Register technical debt and bugs found during review.
- Reference `systemPatterns.md` and project standards.

## Review Process
1. **Automated Checks**
   - All tests must pass (Testing Library, Vitest/Jest).
   - Check for linting/formatting (ESLint, Prettier, stylelint).
   - Code coverage ≥ 80% for critical code.
2. **Manual Review**
   - Readability and clarity
   - Component structure (Atomic Design)
   - Accessibility (WCAG, ARIA)
   - Responsive design
   - Type safety (TypeScript)
   - Test coverage (Testing Library, AAA pattern)
   - Compliance (LGPD, OWASP)
3. **Feedback**
   - Provide actionable, constructive feedback.
   - Reference standards and give examples.

---
> Document all findings and reference the relevant guideline. If in doubt, consult systemPatterns.md or the team.
