# Contributing to Smart Alarm - Neurodivergent-Focused Development

Welcome to the Smart Alarm project! We're building an alarm system specifically designed for neurodivergent users with ADHD, autism, and other cognitive differences. Your contributions help create technology that genuinely improves people's daily lives and health management.

## 🧠 Our Mission and Values

Smart Alarm exists to serve a community that has historically been marginalized by mainstream technology design. Every contribution should advance our core values:

**Accessibility First**: Features should reduce cognitive load and accommodate different ways of processing information, not add complexity that excludes people.

**Privacy as a Right**: Neurodivergent users often share sensitive information about attention patterns, medication schedules, and daily routines. We protect this data as if our own lives depended on it.

**Community-Driven Design**: The best insights about neurodivergent needs come from neurodivergent people themselves. We prioritize community feedback over theoretical accessibility guidelines.

**Reliability Above Everything**: When someone depends on our system for medication reminders or critical appointments, failure isn't just inconvenient—it can have serious health consequences.

## 🚀 Quick Start for Contributors

### Development Environment Setup

1. **Fork and clone the repository**
   ```bash
   git clone https://github.com/your-username/smart-alarm.git
   cd smart-alarm
   ```

2. **Install dependencies for all services**
   ```bash
   # Backend dependencies (.NET 8)
   dotnet restore SmartAlarm.sln
   
   # Frontend dependencies (React PWA)
   cd frontend && npm install && cd ..
   
   # Microservice dependencies (.NET 8)
   cd services/alarm-service && dotnet restore && cd ../..
   cd services/ai-service && dotnet restore && cd ../..
   cd services/integration-service && dotnet restore && cd ../..
   ```

3. **Set up your environment**
   ```bash
   cp .env.example .env
   # Edit .env with your local configuration
   ```

4. **Start development servers**
   ```bash
   # Run all services (requires Docker Compose)
   docker-compose -f docker-compose.dev.yml up
   
   # Or start services individually:
   # Terminal 1: Frontend
   cd frontend && npm start
   
   # Terminal 2: Go service
   cd services/alarm-service && go run cmd/server/main.go
   
   # Terminal 3: Python service  
   cd services/ai-service && python app/main.py
   
   # Terminal 4: Integration service
   cd services/integration-service && npm run dev
   ```

### Making Your First Contribution

Start with documentation improvements or accessibility enhancements—these contributions are always needed and help you understand our user-focused approach.

Look for issues labeled `good-first-issue`, `accessibility`, or `neurodivergent-feedback`. These issues include detailed context about user needs and provide good entry points for new contributors.

## 🎯 Types of Contributions We Need

### 🧪 User Experience and Accessibility Testing

**Who we need**: Neurodivergent users, accessibility experts, UX researchers, and anyone passionate about inclusive design.

**What you can contribute**:
- Test interfaces with screen readers and keyboard navigation
- Provide feedback on cognitive load and interface complexity
- Suggest improvements based on lived experience with neurodivergent conditions
- Document accessibility issues with specific examples and suggested fixes

**Getting started**: No coding required! Install the development version and try accomplishing real tasks like setting up medication reminders or managing daily routines. Document your experience, especially any moments of confusion or frustration.

### 💻 Frontend Development

**Technologies**: React 18, TypeScript, Tailwind CSS, PWA APIs

**What we need**:
- Accessible components that work with assistive technologies
- Performance optimizations for users with limited devices or connectivity
- PWA features that ensure offline reliability
- Visual design improvements that reduce cognitive stress

**Key focus areas**:
- Screen reader optimization and ARIA implementation
- Keyboard navigation patterns that feel natural and efficient
- Form designs that prevent errors and guide users toward success
- Calendar interfaces that accommodate different time-processing styles

### 📱 Frontend Development (React PWA)

**Technologies**: React 18, TypeScript, Vite, TailwindCSS, Zustand, PWA

**What we need**:
- Accessibility improvements for neurodivergent users
- PWA features that work reliably offline
- ML integration that respects user privacy
- Real-time synchronization across devices
- Performance optimizations for cognitive load reduction

**Key focus areas**:
- WCAG AAA compliance with neurodivergent-specific considerations
- Service worker implementation for reliable offline functionality
- State management that reduces cognitive overhead
- Component design that accommodates different interaction patterns
- Testing strategies that validate accessibility and usability

### 🔧 Backend Development

**Technologies**: C# (.NET 8), Entity Framework Core, Clean Architecture, Microservices

**What we need**:
- Performance optimizations for high-frequency alarm operations
- AI algorithms (ML.NET) that recognize neurodivergent behavioral patterns
- SignalR integration for real-time multi-device synchronization
- Security enhancements that protect sensitive health data

**Key focus areas**:
- Database performance for rapid alarm CRUD operations
- Machine learning models trained on neurodivergent usage patterns  
- API design that supports graceful degradation and error recovery
- Privacy-preserving analytics that benefit users without compromising individual data
- Serverless architecture optimization for OCI Functions

### 🔒 Security and Privacy

**What we need**:
- Security audits of multi-language architecture
- Privacy impact assessments for AI features
- Encryption implementations that protect sensitive neurodivergent data
- Compliance reviews for LGPD, GDPR, and healthcare data regulations

**Critical areas**:
- Authentication flows that balance security with accessibility
- Data encryption that protects behavioral patterns and medical information
- Privacy-preserving AI that provides insights without exposing individual data
- Audit logging that supports compliance without creating surveillance

### 📚 Documentation and Education

**What we need**:
- User guides written in clear, accessible language
- Developer documentation that explains neurodivergent design principles
- Translation of technical concepts into community-friendly explanations
- Research documentation about neurodivergent technology needs

**Focus areas**:
- API documentation with accessibility considerations
- User onboarding guides that reduce initial confusion
- Troubleshooting documentation that addresses common neurodivergent user challenges
- Community education about inclusive technology design

## 📋 Contribution Process

### Issue Creation and Discussion

Before starting significant work, create an issue or comment on existing issues to discuss your approach. This prevents duplicate effort and ensures your contribution aligns with project goals.

When creating issues:
- Use our issue templates for bug reports, feature requests, or accessibility concerns
- Include context about how the issue affects neurodivergent users specifically
- Provide concrete examples and suggest potential solutions
- Tag issues appropriately (accessibility, performance, security, etc.)

### Development Workflow

1. **Create a feature branch**
   ```bash
   git checkout -b feature/meaningful-description
   # or
   git checkout -b fix/issue-description
   ```

2. **Follow coding standards**
   - Frontend: Use ESLint and Prettier configurations included in the project
   - Go: Follow standard Go formatting and use golangci-lint
   - Python: Use Black formatter and follow PEP 8 guidelines
   - All languages: Include comprehensive comments explaining accessibility considerations

3. **Write tests that include accessibility validation**
   - Unit tests for business logic
   - Integration tests for cross-service functionality
   - Accessibility tests using tools like axe-core
   - User journey tests that simulate neurodivergent usage patterns

4. **Update documentation**
   - Add or update relevant documentation in the `docs/` directory
   - Include accessibility notes in API documentation
   - Update user guides if the change affects user-facing features

### Pull Request Guidelines

**PR Title Format**: Use conventional commits format
- `feat: add voice command support for alarm creation`
- `fix: resolve screen reader navigation in calendar view`
- `docs: update accessibility testing procedures`
- `a11y: improve keyboard navigation in settings panel`

**PR Description Template**:
```markdown
## Summary
Brief description of what this PR accomplishes and why it's needed.

## Changes Made
- Specific change 1
- Specific change 2
- Accessibility improvement 3

## Accessibility Impact
How does this change affect users with different neurodivergent conditions?
- ADHD users: [specific impact]
- Autism spectrum users: [specific impact]
- Dyslexic users: [specific impact]
- Screen reader users: [specific impact]

## Testing Performed
- [ ] Backend unit tests pass (`dotnet test`)
- [ ] Frontend unit tests pass (`npm test`)
- [ ] Integration tests pass (`dotnet test --filter Category=Integration`)
- [ ] E2E tests pass (`npm run test:e2e`)
- [ ] Accessibility testing with automated tools and manual validation
- [ ] PWA functionality tested (offline mode, push notifications)
- [ ] Cross-device synchronization tested
- [ ] Performance testing if relevant (Core Web Vitals, API response times)
- [ ] Manual testing with assistive technology if applicable

## Documentation Updated
- [ ] Code comments include accessibility rationale
- [ ] User documentation updated if needed
- [ ] API documentation updated if needed

## Breaking Changes
List any breaking changes and migration steps if applicable.
```

### Code Review Process

All contributions go through code review with focus on:

**Functionality**: Does the code work correctly and handle edge cases?

**Accessibility**: Does the implementation genuinely improve the experience for neurodivergent users?

**Performance**: Will this change maintain the fast, responsive experience users need?

**Security**: Does the implementation protect sensitive user data appropriately?

**Maintainability**: Is the code clear, well-documented, and testable?

Reviewers may include neurodivergent community members who provide feedback on usability and accessibility from lived experience perspectives.

## 🏆 Recognition and Community

### Contributor Recognition

We recognize contributions through:
- Contributor spotlight in project documentation
- Attribution in release notes for significant contributions
- Community showcasing of particularly innovative accessibility solutions
- Conference speaking opportunities for contributors who advance neurodivergent technology

### Community Guidelines

**Inclusive Language**: Use person-first language when discussing neurodivergent conditions ("person with ADHD" rather than "ADHD person") unless community members specify other preferences.

**Respectful Feedback**: Focus feedback on code, design, and functionality rather than personal characteristics. Assume positive intent and provide constructive suggestions.

**Accessibility-First Mindset**: When reviewing contributions, consider how changes affect users with different cognitive processing styles, not just whether they meet technical requirements.

**Privacy Consciousness**: Be mindful that discussing neurodivergent user needs may reveal personal information. Keep examples general and protect individual privacy.

## 🆘 Getting Help and Support

### Where to Ask Questions

- **GitHub Discussions**: Best for design questions, accessibility guidance, and community discussion
- **Discord**: Real-time help with development setup and quick questions
- **GitHub Issues**: Bug reports, feature requests, and specific technical problems