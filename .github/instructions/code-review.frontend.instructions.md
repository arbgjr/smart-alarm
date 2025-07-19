---
applyTo: "**/*.{ts,tsx,js,jsx,css,scss,less,html,json,md}"
---
# Frontend Code Review Instructions

## 1. React & TypeScript Standards

### Component Architecture
- **Functional Components**: Use React functional components with hooks, avoid class components
- **TypeScript Integration**: Proper type definitions for props, state, and component interfaces
- **Component Composition**: Follow Atomic Design principles (atoms, molecules, organisms, pages)
- **Props Validation**: Use TypeScript interfaces for props, avoid PropTypes

### Hook Usage
- **Custom Hooks**: Extract reusable logic into custom hooks with proper naming (`useAlarmData`, `useCalendarSync`)
- **Dependency Arrays**: Proper dependency arrays in `useEffect`, `useCallback`, `useMemo`
- **State Management**: Use `useState` for local state, context for shared state, avoid prop drilling
- **Performance**: Use `React.memo`, `useMemo`, and `useCallback` judiciously to prevent unnecessary re-renders

## 2. Accessibility (WCAG 2.1 AA)

### Semantic HTML
- **Proper Elements**: Use semantic HTML5 elements (`<main>`, `<section>`, `<article>`, `<nav>`)
- **Heading Structure**: Logical heading hierarchy (h1 → h2 → h3), no skipping levels
- **Form Labels**: Every form input has associated label or `aria-label`
- **Button vs Link**: Use `<button>` for actions, `<a>` for navigation

### ARIA Implementation
- **ARIA Labels**: Descriptive labels for screen readers (`aria-label`, `aria-describedby`)
- **Live Regions**: Use `aria-live` for dynamic content updates (notifications, status messages)
- **Focus Management**: Proper focus indicators, logical tab order, focus trapping in modals
- **Screen Reader Support**: Test with screen readers, provide alternative text for images

### Keyboard Navigation
- **Tab Order**: Logical tab sequence through interactive elements
- **Keyboard Shortcuts**: Support for common keyboard interactions (Enter, Space, Escape, Arrow keys)
- **Focus Visible**: Clear focus indicators for all interactive elements
- **Skip Links**: Skip navigation links for screen reader users

## 3. Performance & Optimization

### Bundle Optimization
- **Code Splitting**: Lazy loading for routes and large components using `React.lazy`
- **Tree Shaking**: Import only needed functions from libraries
- **Bundle Analysis**: Regular bundle size analysis to prevent bloat
- **Critical Path**: Optimize Critical Rendering Path, defer non-critical resources

### Runtime Performance
- **Virtual Scrolling**: Implement for large lists to prevent DOM bloat
- **Image Optimization**: Responsive images, proper formats (WebP, AVIF), lazy loading
- **Caching Strategy**: Implement proper caching for API responses and static assets
- **Memory Leaks**: Clean up subscriptions, timers, and event listeners in `useEffect` cleanup

## 4. State Management & Data Flow

### State Architecture
- **Local vs Global**: Use local state when possible, global state for truly shared data
- **Context Usage**: Avoid overusing React Context, consider state colocation
- **Immutable Updates**: Proper immutable state updates, avoid direct mutations
- **Data Normalization**: Normalize complex data structures for better performance

### API Integration
- **Error Handling**: Comprehensive error handling for API calls with user-friendly messages
- **Loading States**: Clear loading indicators for asynchronous operations
- **Optimistic Updates**: Implement optimistic updates where appropriate for better UX
- **Retry Logic**: Implement retry mechanisms for failed API calls

## 5. Security & Privacy (LGPD Compliance)

### Data Protection
- **Sensitive Data**: No sensitive data stored in localStorage or exposed in client code
- **Authentication**: Secure token handling, automatic logout on token expiration
- **Input Sanitization**: All user inputs sanitized to prevent XSS attacks
- **HTTPS Only**: All API calls use HTTPS, no mixed content

### Privacy Controls
- **Consent Management**: LGPD consent flows properly implemented
- **Data Minimization**: Request only necessary user data, clear data usage explanations
- **User Rights**: Implement data export, deletion, and modification capabilities
- **Cookie Policy**: Proper cookie consent and management

## 6. Progressive Web App (PWA)

### PWA Features
- **Service Worker**: Proper caching strategies, offline functionality
- **App Manifest**: Complete manifest.json with icons, theme colors, display modes
- **Installability**: App installation prompts and offline capabilities
- **Performance**: Meet Core Web Vitals thresholds (LCP, FID, CLS)

### Offline Support
- **Cache Strategy**: Appropriate caching for different resource types
- **Offline UI**: Clear indicators when app is offline, graceful degradation
- **Data Sync**: Background sync for user actions performed offline
- **Storage Management**: Proper storage quota management

## 7. Testing Standards

### Unit Testing
- **Testing Library**: Use React Testing Library, avoid Enzyme
- **User-Centric Tests**: Test behavior from user perspective, not implementation details
- **Accessibility Testing**: Include accessibility tests using jest-axe
- **Mock Strategy**: Mock external dependencies, avoid mocking React internals

### Integration Testing
- **User Flows**: Test complete user workflows end-to-end
- **API Integration**: Test components with real API calls in controlled environment
- **Cross-Browser**: Ensure compatibility across major browsers
- **Mobile Testing**: Test responsive behavior and touch interactions

## 8. Styling & UI Consistency

### CSS Architecture
- **Component Styles**: Scoped styles using CSS Modules or styled-components
- **Design System**: Follow established design tokens and component library
- **Responsive Design**: Mobile-first approach, proper breakpoint usage
- **Performance**: Minimize CSS bundle size, avoid unused styles

### Visual Consistency
- **Design Tokens**: Use consistent spacing, colors, typography from design system
- **Animation**: Subtle, purposeful animations that respect prefers-reduced-motion
- **Loading States**: Consistent loading indicators and skeleton screens
- **Error States**: Clear error messages with actionable next steps

## 9. Browser Compatibility

### Modern Standards
- **ES6+ Features**: Use modern JavaScript features with appropriate polyfills
- **CSS Grid/Flexbox**: Use modern layout techniques with fallbacks where needed
- **Progressive Enhancement**: Core functionality works without JavaScript
- **Feature Detection**: Use feature detection instead of browser sniffing

## 10. Development Experience

### Code Quality
- **Linting**: ESLint and Prettier configured and enforced
- **Type Safety**: Strict TypeScript configuration, minimal `any` usage
- **Error Boundaries**: React Error Boundaries to catch and handle component errors
- **Development Tools**: Proper development tooling and debugging setup

### Documentation
- **Component Documentation**: Storybook or similar for component documentation
- **README Files**: Clear setup and development instructions
- **Code Comments**: Complex logic explained with inline comments
- **API Documentation**: Frontend API integration documented

## Review Checklist

- [ ] TypeScript types properly defined
- [ ] Accessibility requirements met (WCAG 2.1 AA)
- [ ] Performance optimizations implemented
- [ ] Security best practices followed
- [ ] LGPD compliance requirements addressed
- [ ] PWA features working correctly
- [ ] Tests cover critical user paths
- [ ] Responsive design across devices
- [ ] Error handling comprehensive
- [ ] Code follows established patterns and conventions
