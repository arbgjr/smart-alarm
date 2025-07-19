
# Security Architecture – Smart Alarm

## Overview

This document details the security architecture of Smart Alarm, covering authentication, authorization, data protection, privacy (LGPD/GDPR), secure logging, and OWASP best practices.

## Authentication and Authorization

- JWT Bearer for API authentication
- FIDO2/WebAuthn for passwordless authentication
- RBAC (Role-Based Access Control) for granular authorization
- All sensitive endpoints protected with `[Authorize]`

## Data Protection

- AES-256-GCM for data at rest
- TLS 1.3 for data in transit
- BYOK (Bring Your Own Key) for sensitive data
- Keys protected in HSM (Vault)
- End-to-end encryption for critical alarms

## Privacy and Compliance (LGPD/GDPR)

- Granular consent and consent logging
- Cryptographic erasure (key revocation)
- Automatic TTL for sensitive data
- Access audit and compliance logs

## Secure Logging

- Serilog with structured logs
- Never log sensitive data or secrets
- TraceId included in all logs and error responses

## OWASP Best Practices

- Input validation and sanitization everywhere
- Protection against XSS, CSRF, SQL Injection, and more
- Automated security testing

## Observability and Monitoring

- Application Insights and OpenTelemetry for tracing
- Automatic alerts for failures and unauthorized access attempts

## References

- [OWASP Top 10](https://owasp.org/www-project-top-ten/)
- [LGPD](https://www.gov.br/cidadania/pt-br/acesso-a-informacao/lgpd)

---

**Status:** Security documentation complete and up to date.
