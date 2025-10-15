# Smart Alarm Frontend - PWA

A modern, production-ready Progressive Web Application built with React 18, TypeScript, and comprehensive AI-powered features for intelligent alarm management.

## 🚀 Production Features

### ✅ Complete PWA Implementation
- **Service Worker**: Workbox-powered caching with network-first strategy
- **Offline Support**: Full offline functionality with background sync
- **Installable**: Native app-like experience on mobile and desktop
- **Push Notifications**: Web Push API with VAPID keys
- **App Manifest**: Complete PWA manifest with icons and metadata

### ✅ Advanced State Management
- **Zustand Stores**: Centralized state with persistence
- **React Query Integration**: Intelligent caching and synchronization
- **Optimistic Updates**: Immediate UI feedback with server reconciliation
- **Offline State Management**: Seamless offline/online transitions

### ✅ AI-Powered Sleep Intelligence
- **ML Data Collection**: Privacy-first behavioral tracking
- **Sleep Pattern Analysis**: Intelligent cycle detection and optimization
- **Smart Recommendations**: Personalized suggestions with confidence scoring
- **Alarm Optimization**: Automatic timing adjustment for better wake experience

### ✅ Real-time Multi-Device Sync
- **SignalR Integration**: Live synchronization across devices
- **Conflict Resolution**: Automatic handling of concurrent changes
- **Device Presence**: Multi-device awareness and status tracking
- **Push Notifications**: Native notifications for alarm events

### ✅ Production-Grade Testing
- **E2E Testing**: Comprehensive Playwright testing suite
- **Component Testing**: React Testing Library integration
- **Accessibility Testing**: WCAG AAA compliance validation
- **Performance Testing**: Core Web Vitals monitoring

## 🛠️ Tech Stack

### Core Technologies
- **React 18** - Latest React with concurrent features
- **TypeScript 5.0** - Type-safe development
- **Vite 4** - Lightning-fast build tool
- **TailwindCSS** - Utility-first styling

### State & Data Management
- **Zustand 4** - Lightweight state management
- **React Query 4** - Server state synchronization
- **React Hook Form** - Performant form handling
- **Zod** - Runtime type validation

### PWA & Performance
- **Vite PWA Plugin** - Service worker generation
- **Workbox** - Advanced caching strategies
- **Web Push API** - Native push notifications
- **Background Sync** - Offline data synchronization

### Real-time & ML
- **SignalR Client** - Real-time communication
- **Custom ML Engine** - Client-side behavioral analysis
- **Privacy-first Design** - Local data processing

### Testing & Quality
- **Vitest** - Unit testing framework
- **Playwright** - End-to-end testing
- **Testing Library** - Component testing
- **ESLint** - Code quality and consistency

## 📁 Project Structure

```
frontend/
├── public/                          # Static assets
│   ├── pwa-192x192.png             # PWA icons
│   ├── pwa-512x512.png
│   └── manifest.webmanifest        # PWA manifest
├── src/
│   ├── components/                  # Reusable components
│   │   ├── common/                 # Common UI components
│   │   ├── forms/                  # Form components
│   │   ├── layout/                 # Layout components
│   │   └── ml/                     # ML/AI components
│   ├── hooks/                      # Custom React hooks
│   │   ├── useAlarms.ts
│   │   ├── useAuth.ts
│   │   └── index.ts
│   ├── pages/                      # Page components
│   │   ├── Dashboard.tsx
│   │   ├── Alarms.tsx
│   │   ├── Login.tsx
│   │   └── Insights.tsx
│   ├── services/                   # API services
│   │   ├── alarmService.ts
│   │   ├── authService.ts
│   │   └── index.ts
│   ├── stores/                     # Zustand stores
│   │   ├── authStore.ts
│   │   ├── alarmsStore.ts
│   │   └── uiStore.ts
│   ├── types/                      # TypeScript types
│   │   ├── api.ts
│   │   ├── auth.ts
│   │   └── components.ts
│   ├── utils/                      # Utility functions
│   │   ├── mlDataCollector.ts      # ML data collection
│   │   ├── alarmOptimizer.ts       # Smart alarm optimization
│   │   ├── signalRConnection.ts    # Real-time communication
│   │   ├── pushNotifications.ts    # Push notification handling
│   │   ├── backgroundSync.ts       # Offline sync utilities
│   │   └── realTimeSyncManager.ts  # Multi-device synchronization
│   ├── styles/                     # Global styles
│   │   └── index.css
│   ├── App.tsx                     # Main app component
│   ├── main.tsx                    # App entry point
│   └── vite-env.d.ts              # Vite type definitions
├── tests/                          # Test files
│   └── e2e/                       # E2E tests
│       ├── auth.spec.ts
│       ├── alarms.spec.ts
│       ├── ml-insights.spec.ts
│       ├── global-setup.ts
│       └── global-teardown.ts
├── docker-compose.test.yml         # Test infrastructure
├── playwright.config.ts           # Playwright configuration
├── tailwind.config.js             # TailwindCSS configuration
├── tsconfig.json                  # TypeScript configuration
├── vite.config.ts                 # Vite configuration
└── package.json                   # Dependencies and scripts
```

## 🚀 Quick Start

### Prerequisites

- Node.js 18+ and npm
- Docker (for E2E tests)
- Backend API running (see main README)

### Installation

```bash
# Clone the repository
git clone https://github.com/arbgjr/smart-alarm.git
cd smart-alarm/frontend

# Install dependencies
npm install

# Start development server
npm run dev
```

The application will be available at `http://localhost:5173`.

### Available Scripts

```bash
# Development
npm run dev              # Start development server
npm run build           # Build for production
npm run preview         # Preview production build

# Testing
npm test                # Run unit tests
npm run test:ui         # Run tests with UI
npm run test:e2e        # Run E2E tests
npm run test:e2e:docker # Run E2E tests with Docker

# Code Quality
npm run lint            # Run ESLint
npm run typecheck       # Run TypeScript compiler

# Docker Testing
npm run docker:test:up   # Start test infrastructure
npm run docker:test:down # Stop test infrastructure
```

## 📱 PWA Features

### Service Worker

The PWA uses Workbox for advanced caching strategies:

```typescript
// vite.config.ts
VitePWA({
  registerType: 'autoUpdate',
  workbox: {
    globPatterns: ['**/*.{js,css,html,ico,png,svg}'],
    runtimeCaching: [
      {
        urlPattern: /^https:\/\/api\.smartalarm\.com\/.*$/,
        handler: 'NetworkFirst',
        options: {
          cacheName: 'api-cache',
          networkTimeoutSeconds: 10
        }
      }
    ]
  }
})
```

### Offline Support

- **Automatic Sync**: Queues actions when offline and syncs when online
- **Background Sync**: Uses service worker for background synchronization
- **Conflict Resolution**: Handles data conflicts intelligently

### Push Notifications

```typescript
// Enable push notifications
const { requestPermission, subscribe } = usePushNotifications();

await requestPermission();
const subscription = await subscribe();

// Schedule alarm notifications
await scheduleAlarm(alarmId, '07:00', 'Morning Alarm');
```

## 🧠 AI & ML Features

### Smart Sleep Analytics

The ML system provides intelligent sleep insights:

```typescript
// Enable ML data collection
const { enableCollection, getLocalAnalytics } = useMLDataCollection();

enableCollection();
const analytics = getLocalAnalytics();

// Get personalized recommendations
const recommendations = generateRecommendations(analytics);
```

### Intelligent Alarm Optimization

```typescript
// Get optimized alarm time
const { calculateOptimalTime } = useAlarmOptimization();

const optimization = calculateOptimalTime('07:00', '23:00');
console.log(`Optimized time: ${optimization.optimizedTime}`);
console.log(`Confidence: ${optimization.confidenceScore * 100}%`);
```

## 🔄 Real-time Features

### SignalR Integration

```typescript
// Connect to real-time hub
const { connect, sendAlarmEvent } = useSignalRConnection();

await connect();

// Send alarm update to other devices
await sendAlarmEvent({
  alarmId: 'alarm-123',
  type: 'updated',
  data: { isEnabled: true }
});
```

### Multi-Device Sync

```typescript
// Real-time synchronization
const { initialize, sendAlarmUpdate } = useRealTimeSync();

await initialize();

// Sync alarm changes across devices
await sendAlarmUpdate('alarm-123', 'enabled', { deviceId: 'device-1' });
```

## 🎨 UI Components

### Design System

The app uses a consistent design system with:

- **Responsive Design**: Mobile-first approach with breakpoints
- **Accessibility**: WCAG AAA compliance with proper ARIA labels
- **Dark Mode**: Full dark mode support with system preference detection
- **Animations**: Smooth transitions and micro-interactions

### Key Components

```tsx
// Alarm card component with optimistic updates
<AlarmCard 
  alarm={alarm}
  onToggle={toggleAlarm}
  onEdit={editAlarm}
  onDelete={deleteAlarm}
  showOptimization={true}
/>

// ML insights dashboard
<SleepInsights 
  analytics={analytics}
  recommendations={recommendations}
  onEnableML={enableMLCollection}
/>

// Real-time sync status
<SyncStatus 
  status={syncStatus}
  deviceCount={deviceCount}
  onSync={performSync}
/>
```

## 🧪 Testing

### Unit Tests (Vitest)

```bash
# Run unit tests
npm test

# Watch mode
npm run test:watch

# Coverage report
npm run test:coverage
```

### E2E Tests (Playwright)

```bash
# Run all E2E tests
npm run test:e2e

# Run specific test file
npm run test:e2e:auth

# Run with Docker infrastructure
npm run test:e2e:docker

# Debug mode
npm run test:e2e:debug
```

### Test Categories

- **Authentication**: Login, registration, logout, token handling
- **Alarm Management**: CRUD operations, offline support, validation
- **ML Insights**: Analytics, recommendations, privacy controls
- **Real-time Sync**: Multi-device synchronization, conflict resolution
- **Accessibility**: WCAG compliance, keyboard navigation, screen reader support

## 🔧 Configuration

### Environment Variables

```bash
# API Configuration
VITE_API_BASE_URL=https://api.smartalarm.com
VITE_SIGNALR_HUB_URL=https://api.smartalarm.com/smartalarmhub

# Push Notifications
VITE_VAPID_PUBLIC_KEY=your-vapid-public-key

# Feature Flags
VITE_ENABLE_PWA=true
VITE_ENABLE_ML=true
VITE_ENABLE_PUSH_NOTIFICATIONS=true
```

### Vite Configuration

```typescript
export default defineConfig({
  plugins: [
    react(),
    VitePWA({
      registerType: 'autoUpdate',
      workbox: {
        globPatterns: ['**/*.{js,css,html,ico,png,svg}']
      }
    })
  ],
  resolve: {
    alias: {
      '@': resolve(__dirname, './src')
    }
  }
});
```

## 📊 Performance

### Core Web Vitals Targets

- **First Contentful Paint (FCP)**: < 1.5s
- **Largest Contentful Paint (LCP)**: < 2.5s
- **Cumulative Layout Shift (CLS)**: < 0.1
- **Time to Interactive (TTI)**: < 3s

### Optimization Techniques

- **Code Splitting**: Lazy loading of routes and components
- **Tree Shaking**: Remove unused code from bundles
- **Image Optimization**: WebP format with fallbacks
- **Caching**: Intelligent service worker caching
- **Bundle Analysis**: Regular bundle size monitoring

## 🔒 Security

### Content Security Policy

```javascript
const csp = {
  "default-src": ["'self'"],
  "script-src": ["'self'", "'unsafe-inline'"],
  "style-src": ["'self'", "'unsafe-inline'"],
  "connect-src": ["'self'", "https://api.smartalarm.com", "wss://api.smartalarm.com"],
  "worker-src": ["'self'"]
};
```

### Security Features

- **Input Validation**: Client-side validation with Zod
- **XSS Protection**: Sanitized user inputs
- **CSRF Protection**: Token-based protection
- **Secure Storage**: Encrypted sensitive data in localStorage
- **Privacy by Design**: Local ML processing, minimal data collection

## 🚀 Production Deployment

### Build for Production

```bash
# Build optimized bundle
npm run build

# Preview production build
npm run preview

# Analyze bundle size
npm run build:analyze
```

### CDN Deployment

```bash
# Deploy to CDN (example with AWS S3 + CloudFront)
aws s3 sync dist/ s3://smartalarm-frontend
aws cloudfront create-invalidation --distribution-id E123456 --paths "/*"
```

### Docker Deployment

```dockerfile
FROM node:18-alpine as builder

WORKDIR /app
COPY package*.json ./
RUN npm ci --only=production

COPY . .
RUN npm run build

FROM nginx:alpine
COPY --from=builder /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/nginx.conf

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
```

## 📈 Monitoring

### Analytics

- **User Engagement**: Page views, session duration, feature usage
- **Performance**: Core Web Vitals, load times, error rates
- **PWA Metrics**: Install rates, offline usage, push notification engagement
- **ML Metrics**: Data collection rates, prediction accuracy, user feedback

### Error Tracking

```typescript
// Error boundary with logging
class ErrorBoundary extends Component {
  componentDidCatch(error: Error, errorInfo: ErrorInfo) {
    // Log to monitoring service
    logError(error, errorInfo);
  }
}
```

## 🤝 Contributing

### Development Workflow

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Run tests: `npm test && npm run test:e2e`
5. Submit a pull request

### Code Style

- **ESLint**: Consistent code formatting
- **Prettier**: Automatic code formatting
- **TypeScript**: Strict type checking
- **Conventional Commits**: Semantic commit messages

### Pull Request Checklist

- [ ] Tests pass locally
- [ ] New features have tests
- [ ] Documentation updated
- [ ] Accessibility considerations addressed
- [ ] Performance impact considered
- [ ] Security implications reviewed

---

For more information:
- [Main Project README](../README.md)
- [Production Deployment Guide](../PRODUCTION-DEPLOYMENT.md)
- [API Documentation](../docs/api/)
- [Architecture Overview](../docs/architecture/)