# Neurodivergent Intelligent Alarms - Project Overview & Setup Instructions

## Project Vision

You are building a specialized webapp designed specifically for neurodivergent users (ADHD, Autism Spectrum) that combines intelligent alarms with visual calendar interface. The application focuses on accessibility, reliability, and understanding the unique cognitive patterns of neurodivergent individuals.

## Core Principles

**Accessibility First**: Every design decision must consider different types of neurodiversity. This means supporting reduced motion preferences, high contrast modes, clear visual hierarchies, and intuitive navigation patterns that reduce cognitive load.

**Reliability Above All**: Alarms for medication and critical tasks cannot fail. The system must work offline, survive browser restrictions, and provide multiple notification fallbacks to ensure users never miss important reminders.

**Privacy by Design**: Mental health and neurodiversity data is extremely sensitive. All processing should happen locally when possible, with strong encryption for any data that must be stored or transmitted.

## Development Philosophy: Hybrid Approach

**Vibe Coding (30% of time)**: Use your human intuition for UX/UI decisions that require empathy and understanding of neurodivergent needs. Architecture decisions, debugging complex interactions, and accessibility customizations should rely on human insight.

**AI-Assisted Development (70% of time)**: Leverage AI for implementing well-defined functionality, boilerplate code, API integrations, testing, and refactoring. This includes TypeScript interfaces, Service Worker implementation, database operations, and utility functions.

## Project Structure

```
src/
├── components/          # Reusable UI components
│   ├── forms/          # Alarm creation/editing forms
│   ├── calendar/       # Calendar view components
│   ├── notifications/  # Notification UI components
│   └── accessibility/  # Accessibility-specific components
├── hooks/              # Custom React hooks
│   ├── useAlarms.ts    # Alarm CRUD operations
│   ├── useOfflineSync.ts # Offline synchronization
│   └── useNotifications.ts # Browser notifications
├── services/           # Business logic and external integrations
│   ├── storage/        # IndexedDB operations via Dexie
│   ├── notifications/ # Service Worker notifications
│   └── ai/            # Local TensorFlow.js processing
├── types/             # TypeScript type definitions
├── utils/             # Helper functions and constants
└── workers/           # Service Worker implementation
```

## Initial Setup Instructions

### Environment Setup

Create a new React project with TypeScript and configure it with the accessibility-first approach that neurodivergent users require:

```bash
npm create vite@latest neurodivergent-alarms -- --template react-ts
cd neurodivergent-alarms
npm install
```

Install core dependencies that support both functionality and accessibility:

```bash
# Core UI and Calendar
npm install react-big-calendar dnd-kit @dnd-kit/core @dnd-kit/modifiers
npm install lucide-react @headlessui/react

# Forms and Validation
npm install react-hook-form @hookform/resolvers zod

# Storage and Offline Support
npm install dexie workbox-webpack-plugin

# Styling and Accessibility
npm install tailwindcss @tailwindcss/forms @tailwindcss/typography
npm install @tailwindcss/aspect-ratio autoprefixer postcss

# Development Tools
npm install -D @types/react @types/react-dom
npm install -D eslint-plugin-jsx-a11y prettier
```

### Accessibility Configuration

Configure ESLint with accessibility rules that catch common issues affecting neurodivergent users:

```json
// .eslintrc.json
{
  "extends": [
    "react-app",
    "react-app/jest",
    "plugin:jsx-a11y/recommended"
  ],
  "plugins": ["jsx-a11y"],
  "rules": {
    "jsx-a11y/no-distracting-elements": "error",
    "jsx-a11y/media-has-caption": "error",
    "jsx-a11y/no-autofocus": "error"
  }
}
```

### Tailwind Configuration for Neurodivergent UX

Configure Tailwind with accessibility considerations and neurodivergent-friendly defaults:

```javascript
// tailwind.config.js
module.exports = {
  content: ["./src/**/*.{js,jsx,ts,tsx}"],
  theme: {
    extend: {
      fontFamily: {
        'dyslexic': ['OpenDyslexic', 'Arial', 'sans-serif'],
      },
      animation: {
        'reduced-motion': 'none',
      },
      colors: {
        // High contrast color palette
        'neuro-primary': '#0066cc',
        'neuro-secondary': '#66b3ff',
        'neuro-accent': '#ff6b35',
        'neuro-success': '#28a745',
        'neuro-warning': '#ffc107',
        'neuro-error': '#dc3545',
      }
    },
  },
  plugins: [
    require('@tailwindcss/forms'),
    require('@tailwindcss/typography'),
  ],
}
```

## Core Type Definitions

Create foundational TypeScript interfaces that capture the complexity of neurodivergent alarm needs:

```typescript
// src/types/alarm.ts
export interface Alarm {
  id: string;
  title: string;
  description?: string;
  datetime: Date;
  recurrence?: RecurrencePattern;
  category: AlarmCategory;
  priority: AlarmPriority;
  isEnabled: boolean;
  accessibility: AccessibilitySettings;
  neurodivergentOptions: NeurodivergentOptions;
  createdAt: Date;
  updatedAt: Date;
}

export interface NeurodivergentOptions {
  requireConfirmation: boolean;
  snoozeOptions: number[]; // minutes
  visualCues: VisualCueType[];
  audioOptions: AudioOptions;
  customInstructions?: string;
}

export interface AccessibilitySettings {
  reducedMotion: boolean;
  highContrast: boolean;
  fontPreference: 'default' | 'dyslexic' | 'large';
  screenReaderOptimized: boolean;
}
```

## Development Workflow

### Phase 1: Foundation (Weeks 1-2)
Focus on creating the core architecture with accessibility baked in from the start. Implement the basic alarm data model, storage layer with Dexie, and fundamental UI components that work well with screen readers and various input methods.

### Phase 2: Core Functionality (Weeks 3-4)
Build the alarm CRUD operations with validation, form handling optimized for cognitive load reduction, and basic calendar integration. Ensure all interactive elements follow accessibility guidelines and provide clear feedback.

### Phase 3: Calendar Integration (Weeks 5-6)
Implement React Big Calendar with customizations for neurodivergent users, including drag-and-drop with clear visual feedback, keyboard navigation support, and multiple view options that reduce cognitive overwhelm.

### Phase 4: PWA and Notifications (Week 7)
Implement Service Workers for reliable notifications, PWA manifest for app-like experience, and offline functionality that ensures alarms work regardless of connectivity.

### Phase 5: Polish and Launch (Week 8)
Conduct accessibility testing with actual neurodivergent users, performance optimization, and deployment preparation.

## Key Success Metrics

**Technical Performance**: Lighthouse score above 90 in all categories, bundle size under 500KB, first load under 3 seconds, and 100% offline functionality.

**Accessibility Compliance**: Full WCAG 2.1 AA compliance, tested with multiple screen readers, keyboard navigation support, and high contrast mode functionality.

**User Experience**: Time to create alarm under 30 seconds, notification reliability above 95%, and positive feedback from neurodivergent beta users on cognitive load reduction.

## Important Considerations

Remember that neurodivergent users may have different relationships with technology, time management, and cognitive processing. Design patterns that work for neurotypical users may create barriers or additional stress for your target audience. Always prioritize clarity, predictability, and user control over flashy features or complex interactions.

The MVP should focus intensely on doing the core alarm and calendar functionality exceptionally well rather than trying to include every possible feature. Reliability and usability for your specific user base is more valuable than feature breadth at this stage.