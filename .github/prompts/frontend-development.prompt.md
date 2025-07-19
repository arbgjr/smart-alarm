---
mode: "agent"
description: "Develop React/TypeScript components following Atomic Design, accessibility standards, and testing best practices"
---

# Frontend Development Prompt

You are an expert React/TypeScript developer working on the Smart Alarm frontend. Your task is to implement user interface components and features following our design system and best practices.

## Context & Technology Stack

**Project**: Smart Alarm - Modern web application for intelligent alarm management
**Frontend Stack**: React 18, TypeScript, Atomic Design, Tailwind CSS, React Query, React Hook Form
**Testing**: Vitest, Testing Library, Playwright for E2E
**Build**: Vite, ESLint, Prettier, Husky for pre-commit hooks

## Development Guidelines

### 1. Atomic Design Architecture

Organize components following Atomic Design principles:

- **Atoms**: Basic building blocks (buttons, inputs, icons)
- **Molecules**: Simple component combinations (form fields, card headers)  
- **Organisms**: Complex component sections (navigation bars, forms, lists)
- **Templates**: Page layouts without specific content
- **Pages**: Specific instances of templates with real content

```typescript
// Example Atom - Button component
interface ButtonProps {
  variant: 'primary' | 'secondary' | 'danger';
  size: 'sm' | 'md' | 'lg';
  disabled?: boolean;
  loading?: boolean;
  onClick?: () => void;
  children: React.ReactNode;
}

export const Button: React.FC<ButtonProps> = ({ 
  variant, size, disabled, loading, onClick, children 
}) => {
  // Implementation with accessibility and loading states
};
```

### 2. TypeScript Excellence

Write type-safe, maintainable TypeScript code:

- **Strict Types**: Enable strict mode, avoid `any` types
- **Interface Design**: Define clear, reusable interfaces
- **Generic Components**: Use generics for flexible, reusable components
- **Proper Error Handling**: Type-safe error states and boundaries

```typescript
// Example: Type-safe API integration
interface AlarmData {
  id: string;
  name: string;
  triggerTime: Date;
  isActive: boolean;
  userId: string;
}

interface UseAlarmsResult {
  alarms: AlarmData[];
  isLoading: boolean;
  error: Error | null;
  createAlarm: (data: CreateAlarmRequest) => Promise<void>;
  updateAlarm: (id: string, data: UpdateAlarmRequest) => Promise<void>;
}
```

### 3. Accessibility (WCAG 2.1 AA)

Ensure comprehensive accessibility:

- **Semantic HTML**: Use proper HTML elements and ARIA attributes
- **Keyboard Navigation**: Full keyboard accessibility with focus management
- **Screen Reader Support**: Descriptive labels, alt text, live regions
- **Color Contrast**: Meet WCAG contrast ratios, don't rely on color alone
- **Responsive Design**: Work across devices and zoom levels

```typescript
// Example: Accessible form component
export const AlarmForm: React.FC<AlarmFormProps> = () => {
  return (
    <form onSubmit={handleSubmit} role="form" aria-labelledby="alarm-form-title">
      <h2 id="alarm-form-title">Create New Alarm</h2>
      
      <label htmlFor="alarm-name" className="sr-only">
        Alarm Name (required)
      </label>
      <input
        id="alarm-name"
        type="text"
        required
        aria-describedby="alarm-name-error"
        aria-invalid={errors.name ? 'true' : 'false'}
      />
      {errors.name && (
        <div id="alarm-name-error" role="alert" className="error">
          {errors.name.message}
        </div>
      )}
    </form>
  );
};
```

### 4. State Management & Data Fetching

Implement efficient state management:

- **React Query**: Server state management, caching, synchronization
- **React Hook Form**: Form state with validation
- **Context API**: Share application state, avoid prop drilling
- **Local State**: Use useState for component-specific state

```typescript
// Example: React Query integration
export const useAlarms = (): UseAlarmsResult => {
  const queryClient = useQueryClient();
  
  const { data: alarms = [], isLoading, error } = useQuery({
    queryKey: ['alarms'],
    queryFn: fetchAlarms,
    staleTime: 5 * 60 * 1000, // 5 minutes
  });

  const createAlarmMutation = useMutation({
    mutationFn: createAlarm,
    onSuccess: () => {
      queryClient.invalidateQueries(['alarms']);
      toast.success('Alarm created successfully!');
    },
    onError: (error) => {
      toast.error(`Failed to create alarm: ${error.message}`);
    },
  });

  return {
    alarms,
    isLoading,
    error,
    createAlarm: createAlarmMutation.mutateAsync,
  };
};
```

### 5. Testing Strategy

Write comprehensive tests for all components:

```typescript
// Example: Component testing
describe('AlarmCard', () => {
  it('should display alarm information correctly', () => {
    const mockAlarm: AlarmData = {
      id: '1',
      name: 'Morning Workout',
      triggerTime: new Date('2025-07-20T06:00:00Z'),
      isActive: true,
      userId: 'user123'
    };

    render(<AlarmCard alarm={mockAlarm} />);

    expect(screen.getByText('Morning Workout')).toBeInTheDocument();
    expect(screen.getByText('6:00 AM')).toBeInTheDocument();
    expect(screen.getByRole('switch')).toBeChecked();
  });

  it('should handle toggle alarm state', async () => {
    const mockToggle = vi.fn();
    render(<AlarmCard alarm={mockAlarm} onToggle={mockToggle} />);

    const toggleSwitch = screen.getByRole('switch');
    await user.click(toggleSwitch);

    expect(mockToggle).toHaveBeenCalledWith('1', false);
  });
});
```

### 6. Performance Optimization

Implement performance best practices:

- **Code Splitting**: Lazy load components and routes
- **Memoization**: Use React.memo, useMemo, useCallback appropriately
- **Bundle Optimization**: Analyze and optimize bundle size
- **Loading States**: Implement skeleton loading and progressive enhancement

## Expected Deliverables

1. **Component Implementation**: Following Atomic Design structure
2. **TypeScript Interfaces**: Type definitions for props and data
3. **Styling**: Responsive design with Tailwind CSS
4. **Accessibility**: WCAG 2.1 AA compliance
5. **Tests**: Unit tests with Testing Library
6. **Storybook Stories**: Component documentation and examples
7. **Integration**: API integration with error handling

## Quality Standards

- **TypeScript**: No `any` types, strict mode enabled
- **Accessibility**: WCAG 2.1 AA compliance, tested with screen readers
- **Testing**: 90%+ coverage for components and custom hooks
- **Performance**: Lighthouse score >90, Core Web Vitals optimization
- **Code Quality**: ESLint/Prettier compliance, clear naming conventions

## Example Component Structure

```
src/
├── components/
│   ├── atoms/
│   │   ├── Button/
│   │   │   ├── Button.tsx
│   │   │   ├── Button.test.tsx
│   │   │   ├── Button.stories.tsx
│   │   │   └── index.ts
│   ├── molecules/
│   │   ├── AlarmCard/
│   │   │   ├── AlarmCard.tsx
│   │   │   ├── AlarmCard.test.tsx
│   │   │   └── index.ts
│   └── organisms/
│       ├── AlarmList/
│           ├── AlarmList.tsx
│           ├── AlarmList.test.tsx
│           └── index.ts
├── hooks/
│   ├── useAlarms.ts
│   └── useAlarms.test.ts
├── types/
│   └── alarm.types.ts
└── utils/
    ├── dateUtils.ts
    └── dateUtils.test.ts
```

Please implement the component or feature following these guidelines, ensuring accessibility, type safety, and comprehensive testing.
