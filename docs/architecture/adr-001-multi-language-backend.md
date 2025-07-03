# ADR-001: Intelligent Alarm Webapp Architecture for Neurodivergent Users

**Status:** Accepted  
**Date:** 2025-01-02  
**Authors:** Development Team  
**Decision Makers:** Product Owner, Tech Lead  

## Context and Problem

We are developing an intelligent alarm webapp specifically designed for neurodivergent users (ADHD, ASD). The system must combine alarm functionality with a calendar visual interface, incorporating AI for behavioral analysis and contextual suggestions.

The main architectural challenges identified include the need for high reliability for critical alarms (medication), an accessible interface for different types of neurodiversity, robust offline operation, OWASP-compliant security for sensitive mental health data, and LGPD compliance for neurodiversity information.

The business model requires open source architecture with a managed service option, Bring Your Own Key (BYOK) implementation for AI, and minimal operational costs during the market validation phase.

## Architectural Decisions

### Frontend: React 18 + TypeScript + PWA

**Decision:** We will use React 18 with TypeScript as the frontend base, implementing a Progressive Web App (PWA) for offline functionality and notifications.

**Justification:** React offers the largest ecosystem of accessible components available, facilitating the implementation of features specific to neurodiversity. TypeScript significantly reduces bugs related to data types, which is critical when dealing with mental health information. PWA enables essential offline functionality for reliable alarms and notifications across platforms.

React's native support for server-side rendering and code splitting allows important performance optimizations for users with connectivity limitations or less powerful devices, common among the neurodivergent population who may have financial constraints.

### Calendar Library: React Big Calendar (MVP) → FullCalendar Premium (Scale)

**Decision:** We will start with React Big Calendar for the MVP, migrating to FullCalendar Premium when revenue justifies the investment.

**Justification:** React Big Calendar offers 80% of the necessary features for free, including native drag-and-drop and multiple views (month, week, day). This decision aligns with our strategy of minimal costs during validation.

FullCalendar Premium ($480/year) will be considered when we need specific Timeline views for complex temporal pattern visualization or when we have a paying user base that justifies the investment. Migration is relatively simple due to similar APIs.

### Backend: Unified Architecture in C#

**Decision:** The entire backend will be implemented exclusively in C# (.NET), using Clean Architecture and SOLID principles. Specialized services (Alarms, AI/Behavioral Analysis, Integration) will be maintained, but all written in C# and organized as independent .NET projects, preferably serverless (Azure Functions).

**Justification:** Unification in C#/.NET eliminates the complexity of multiple languages, facilitates onboarding, standardizes logging, error handling, and security, and allows the use of consistent static analysis, testing, and monitoring tools. Modern .NET (6+) offers performance close to low-level languages for CRUD operations, as well as excellent productivity and corporate support. ML.NET covers AI needs, and external integrations are done with mature libraries from the .NET ecosystem. All communication between services is done via asynchronous events (Azure Event Grid) or HTTP, always with authentication, validation, and robust error handling.

**Adopted Patterns:** Clean Architecture, SOLID, automated testing, input/output validation, structured logging, JWT/FIDO2 authentication, and documentation via Swagger/OpenAPI. All services are designed to be testable, secure, and easily auditable.

### Cloud Provider: Oracle Cloud Infrastructure (OCI)

**Decision:** OCI will be our main provider for all serverless and database functionalities.

**Justification:** Cost analysis showed a 70% saving compared to AWS for similar workloads. OCI offers 10TB of free data egress per month versus $0.09/GB after minimal limits on other providers. For a global application, this represents significant savings.

OCI's Always Free Tier (2 million permanently free invocations) allows extensive development and testing without operational costs. Although the ecosystem is smaller than AWS, the necessary functionalities (Functions, Autonomous Database, Object Storage) are mature and well documented.

### Database: Oracle Autonomous Database (JSON Document Store)

**Decision:** We will use Oracle Autonomous Database configured as a JSON Document Store for main data.

**Justification:** This solution offers NoSQL flexibility for unstructured data (custom neurodiversity configurations) with SQL capability when needed for analytics and reporting.

The converged database model eliminates the need for multiple data systems, reducing operational complexity and costs. Native Time-to-Live (TTL) allows automatic expiration of old alarms, important for LGPD compliance.

### Authentication and Security: FIDO2/WebAuthn + AES-256-GCM

**Decision:** We will implement passwordless authentication via FIDO2/WebAuthn with AES-256-GCM encryption for data at rest.

**Justification:** Neurodivergent users often face difficulties managing complex passwords. WebAuthn offers more accessible authentication via biometrics or hardware keys.

AES-256-GCM with keys derived via PBKDF2 (100,000 iterations) ensures OWASP-compliant security. For neurodiversity data, we will implement k-anonymity and differential privacy techniques for analytics that preserve individual privacy.

### PWA and Notifications: Service Workers + Firebase Cloud Messaging (Fallback)

**Decision:** Service Workers as the primary notification mechanism, with FCM as a fallback for limited browsers.

**Justification:** Service Workers offer full control over notifications and offline operation. iOS Safari has significant limitations, but a PWA installed on the home screen circumvents most restrictions.

FCM as a fallback ensures notifications work even in browsers with aggressive background processing throttling. Redundancy strategy is critical for medication-related alarms where failures can have serious consequences.

### AI and Behavioral Analysis: ML.NET (C# Backend)

**Decision:** All behavioral analysis and AI will be performed in the backend in C# using ML.NET, with the possibility of integrating Python libraries only when absolutely necessary, via Python.NET, always keeping the main logic and sensitive data under C# backend control.

**Justification:** ML.NET covers most machine learning scenarios needed for pattern analysis, recommendations, and personalization. When necessary, integrations with TensorFlow or PyTorch can be done via .NET libraries. Local processing on the frontend may be considered only for offline features, but never for sensitive data.

### External APIs: Integration via C#

**Decision:** All integrations with external APIs (calendars, notifications, holidays, etc.) will be done via .NET libraries, with standardized authentication, logging, and error handling.

**Justification:** The .NET ecosystem offers mature libraries for integration with most relevant providers. Whenever possible, prefer RESTful APIs and OAuth2/OpenID Connect authentication.

### Licensing: Business Source License 1.1

**Decision:** The core will be released under Business Source License 1.1, transitioning to GPL after 4 years.

**Justification:** BSL allows open source for development and testing but protects against direct commercial competitors for 4 years. After this period, automatic transition to GPL ensures contribution to the open source community.

The hybrid architecture keeps the core open source while enterprise features (BYOK, advanced analytics) remain proprietary, allowing financial sustainability.

### Offline Sync: Dexie.js + CRDT (Conflict-free Replicated Data Types)

**Decision:** Dexie.js for local storage with CRDT for automatic conflict resolution.

*This ADR will be reviewed after the first 3 months of development or when major assumptions are invalidated by real usage data.*
