# MVP Roadmap - Intelligent Alarm System
*Planning for hybrid development: Vibe Coding + AI-Assisted*

## 🎯 MVP Definition (Minimum Viable Product)

### Essential Core Features
**Basic alarm creation and management** with a simple and accessible interface, user authentication, allowing creation, editing, and deletion of alarms with specific times, behavioral AI analysis, integration with external calendars. **Native browser notifications** using Service Workers for reliable alerts. **Local storage** with persistence using localStorage/IndexedDB to work offline. **Responsive interface** optimized for mobile and desktop devices with a focus on accessibility.

### Features Excluded from MVP
Cloud sync, and automatic backup will be left for future versions.

## 📊 Zero-Cost Technical Stack

### Frontend (Vibe Coding + AI-Assisted)
**React 18 + TypeScript** as the main base, using **Create React App** or **Vite** for quick setup. **React Big Calendar** (free, MIT license) replacing FullCalendar Premium for calendar views. **Tailwind CSS** for fast and responsive styling. **React Hook Form** for optimized forms. **Lucide React** for free vector icons.

### Backend and Storage
The backend is built entirely with C# (.NET 8+), following Clean Architecture and SOLID principles to ensure maintainability, testability, and scalability. All services are designed as independent serverless functions, deployed on Oracle Cloud Infrastructure (OCI Functions) for cost efficiency and scalability. The architecture includes:

- **RESTful APIs** for alarm management, AI routines, and integrations
- **Modular separation**: Domain, Application, Infrastructure, and API layers
- **Validation**: All input/output is validated using FluentValidation
- **Security**: JWT/FIDO2 authentication, RBAC, LGPD compliance, and structured logging (Serilog)
- **Observability**: Integrated with OCI Application Performance Monitoring and Application Insights
- **Data Storage**: Oracle Autonomous Database for persistent data, Object Storage for files and logs
- **Integrations**: External APIs via HttpClientFactory, Polly, and OAuth2/OpenID Connect
- **Documentation**: Swagger/OpenAPI for all endpoints
- **Testing**: Automated with xUnit and Moq, targeting at least 80% coverage for critical code

No secrets are exposed in code or logs. All infrastructure is managed as code (Terraform), and the system is designed for future extensibility and cloud portability.

### Free Deployment
**Vercel** offers unlimited free hosting for personal projects, with automatic SSL and global CDN. **Netlify** as an alternative with the same advantages. **GitHub Pages** for simple open source projects.

## 🗓️ Development Timeline (8 weeks)

### Weeks 1-2: Setup and Foundations
**Development environment setup** using Vite + React + TypeScript + Tailwind. **Project setup on GitHub** with organized folder structure. **Implementation of the base architecture** with custom hooks for state management. **Basic design system** with fundamental accessible components.

During this phase, you will use AI prompts to generate the initial project structure, ESLint/Prettier configurations, and base components. Development will be mainly vibe coding to feel out the right architecture.

### Weeks 3-4: Core Functionality
**Basic alarm system** with full CRUD (Create, Read, Update, Delete). **Integration with Dexie.js** for robust local persistence. **Form components** for creating/editing alarms. **List and view** of created alarms.

Here, AI-assisted development will be intense to implement business logic, form validations, and data handling. Use specific prompts to generate validation functions, custom hooks, and utils.

### Weeks 5-6: Calendar Interface
**Integration of React Big Calendar** with local alarm data. **Visual customization** for better neurodivergent UX. **Navigation and views** (month, week, day). **Basic drag and drop** for repositioning alarms.

Vibe coding will be crucial here to adjust the UX specifically for neurodivergents, while AI can help with the technical integration of the calendar.

### Week 7: Notifications and PWA
**Service Workers implementation** for browser notifications. **Basic Web Push API** integration. **PWA manifest** for app installation. **Offline functionality** with cache strategies.

AI-assisted development will be valuable for correctly implementing Service Workers, as it is complex and error-prone code.

### Week 8: Polish and Launch
**Usability testing** with a focus on accessibility. **Performance optimization** and code splitting. **Bug fixes** and final refinements. **Documentation** and production deploy.

## 🛠️ Hybrid Development Strategy

### Vibe Coding (30% of the time)
Used for **UX/UI decisions** where intuition and human experience are crucial. **Project architecture and structure**, defining how components relate. **Complex debugging** where context and deep understanding are needed. **Specific customizations** for neurodivergence that require empathy and human understanding.

### AI-Assisted Development (70% of the time)
**Feature implementation** where logic is clear and structured. **Configurations and boilerplate** such as tool setup, TypeScript types, interfaces. **Implementation of browser APIs** (Service Workers, IndexedDB, Notifications). **Testing** with generation of test cases and mocks. **Refactoring** and optimization of existing code.

### Strategic Prompts for AI
"Implement a custom hook to manage alarms with TypeScript, including CRUD operations and validations." "Create a Service Worker that manages alarm notifications with fallback for browsers that do not support it." "Generate a TypeScript interface for the Alarm object with all necessary properties and Zod validations."

## 💰 MVP Cost Breakdown

### Guaranteed Zero Costs
**Development**: Only personal time + free tools. **Hosting**: Vercel/Netlify free tier (unlimited for personal projects). **Domain**: Use free subdomain (.vercel.app or .netlify.app). **Storage**: Browser local storage (no server costs). **APIs**: Only native browser APIs (free).

### Optional Future Investments
**Custom domain**: $10-15/year for .com. **Analytics**: Google Analytics (free) or Plausible ($9/month). **Error tracking**: Sentry free tier (5k errors/month). **Performance monitoring**: Web Vitals free via Google.

## 📈 Post-MVP Roadmap (Future Versions)

### V1.1: UX Improvements (Weeks 9-12)
**Visual themes** for different types of neurodivergence. **Advanced accessibility settings**. **Import/Export** of data for manual backup. **Multiple alarm types** (medication, work, exercise).

### V1.2: Basic Intelligence (Weeks 13-20)
**Automatic suggestions** based on local usage patterns. **Alarm templates** for common situations. **Local effectiveness analysis** using TensorFlow.js. **Simple insights** about personal habits.

### V2.0: Sync and Backend (Weeks 21-32)
**Account system** with secure authentication. **Cross-device sync** using serverless architecture. **Cloud backup** with end-to-end encryption. **Integration with external calendars**.

## 🔧 Recommended Tools and Resources

### Development
**VS Code** with React/TypeScript extensions. **GitHub Copilot** or **Codeium** for AI-assisted coding. **React Developer Tools** for debugging. **Lighthouse** for performance auditing.

### Design and UX
**Figma** (free) for wireframes and prototypes. **Accessibility Insights** for accessibility testing. **WebAIM** for WCAG validation. **Free online contrast ratio checkers**.

### Testing and Quality
**Vitest** for unit testing (faster than Jest). **Testing Library** for component testing. **ESLint + Prettier** for code quality. **Husky** for git hooks and quality gates.

## 🎯 MVP Success Metrics

### Technical
**Lighthouse Score** > 90 in all categories. **Bundle size** < 500KB gzipped. **First Load** < 3 seconds. **Offline functionality** 100% operational.

### User
**Time to create alarm** < 30 seconds. **Notification reliability** > 95%. **Accessibility compliance** WCAG 2.1 AA. **Mobile usability** with no critical issues.

### Business
**Daily active usage** from early adopters. **Retention rate** after 7 days of use. **Qualitative user feedback** on neurodivergent UX. **Feature adoption** of core functionalities.

## 🚀 Go-to-Market Strategy

### Initial Validation
**Simple landing page** explaining the concept. **Early access** for online neurodivergent communities. **User interviews** with potential users for feedback. **Product Hunt** launch for initial visibility.

### Community Building
**Open source approach** to attract contributors. **Complete documentation** for technical adoption. **Blog posts** about development and neurodivergence. **Social media** targeting ADHD/autism communities.

### Growth Loops
**Word of mouth** through exceptional UX. **GitHub stars** for technical credibility. **User-generated content** and testimonials. **Partnerships** with neurodivergence organizations.

---

*This roadmap prioritizes fast learning, minimal costs, and market validation, establishing solid foundations for sustainable future growth.*