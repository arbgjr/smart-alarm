---
applyTo: "**/*.{cs,csproj,sln,props,targets}"
---
# 🧐 Code Review Instructions – C#/.NET

## General Principles
- Review for security, maintainability, and performance.
- Enforce Clean Architecture and SOLID principles.
- Check for error handling, validation, and structured logging (Serilog).
- Ensure code is well documented and testable.
- Validate naming conventions and project organization.
- Register technical debt and bugs found during review.
- Reference `systemPatterns.md` and project standards.

## Review Process
1. **Automated Checks**
   - All tests must pass (xUnit, integration, e2e).
   - Check for linting/formatting (EditorConfig, StyleCop).
   - Code coverage ≥ 80% for critical code.
2. **Manual Review**
   - Readability and clarity
   - Layer separation (Domain, Application, Infrastructure, API)
   - Security (no secrets, proper exception handling)
   - Validation (FluentValidation)
   - Logging (Serilog)
   - Async/await usage
   - Test coverage (xUnit, Moq, AAA pattern)
   - Compliance (LGPD, OWASP)
3. **Feedback**
   - Provide actionable, constructive feedback.
   - Reference standards and give examples.

---
> Document all findings and reference the relevant guideline. If in doubt, consult systemPatterns.md or the team.
