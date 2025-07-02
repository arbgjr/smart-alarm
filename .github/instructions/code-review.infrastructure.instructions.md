---
applyTo: "**/*.{tf,tfvars,yml,yaml,sh,ps1,bat,cmd,ansible,ini,conf,env,github/workflows/**,dockerfile,Dockerfile}"
---
# 🧐 Code Review Instructions – Infrastructure & Automation

## General Principles
- Review for security, idempotency, maintainability, and clarity.
- Enforce best practices for each tool (Terraform, Ansible, GitHub Actions, Shell, Docker, etc).
- Check for secrets, credentials, and sensitive data exposure.
- Ensure code is well documented and modular.
- Validate naming conventions and organization.
- Register technical debt and bugs found during review.
- Reference `systemPatterns.md` and project standards.

## Review Process
1. **Automated Checks**
   - Linting/validation (tflint, ansible-lint, shellcheck, yamllint, hadolint, etc).
   - All CI/CD pipelines must pass.
2. **Manual Review**
   - Readability and clarity
   - Idempotency and repeatability
   - Security (no hardcoded secrets, proper permissions)
   - Documentation and comments
   - Modularity and reuse
   - Compliance (LGPD, OWASP, cloud provider policies)
3. **Feedback**
   - Provide actionable, constructive feedback.
   - Reference standards and give examples.

---
> Document all findings and reference the relevant guideline. If in doubt, consult systemPatterns.md or the team.
