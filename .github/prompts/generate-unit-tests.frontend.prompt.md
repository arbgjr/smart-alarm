---
mode: "agent"
description: "Generate comprehensive unit tests for React/TypeScript components using Vitest, Testing Library, and modern testing patterns"
---

# Generate Unit Tests - Frontend (React/TypeScript)

You are a frontend testing expert specializing in React/TypeScript applications. Your task is to generate comprehensive, maintainable unit tests using modern testing practices and tools.

## Context & Testing Stack

**Project**: Smart Alarm - Modern React frontend with TypeScript
**Testing Stack**: Vitest, Testing Library (React), MSW (Mock Service Worker), user-event
**Architecture**: Atomic Design (atoms, molecules, organisms, templates, pages)
**State Management**: React Query, React Hook Form, Context API
**Accessibility**: WCAG 2.1 AA compliance testing

## Testing Philosophy

Write tests that focus on user behavior and component contracts rather than implementation details:

- **User-Centric**: Test what users see and interact with
- **Behavior-Driven**: Focus on component behavior, not internal state
- **Accessibility**: Ensure components work with assistive technologies
- **Integration**: Test component interactions and data flow

## Test Structure & Patterns

### 1. Component Testing Foundation

```typescript
import { render, screen } from '@testing-library/react';
import { user } from '@testing-library/user-event';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { AlarmCard } from './AlarmCard';

const mockAlarm: AlarmData = {
  id: '1',
  name: 'Morning Workout',
  triggerTime: new Date('2025-07-20T06:00:00Z'),
  isActive: true,
  userId: 'user123'
};

describe('AlarmCard', () => {
  const mockProps = {
    alarm: mockAlarm,
    onToggle: vi.fn(),
    onEdit: vi.fn(),
    onDelete: vi.fn(),
  };

  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('should render alarm information correctly', () => {
    render(<AlarmCard {...mockProps} />);

    expect(screen.getByText('Morning Workout')).toBeInTheDocument();
    expect(screen.getByText('6:00 AM')).toBeInTheDocument();
    expect(screen.getByRole('switch', { name: /toggle morning workout alarm/i })).toBeChecked();
  });
});
```

### 2. User Interaction Testing

Test user interactions using user-event for realistic behavior simulation:

```typescript
describe('AlarmCard user interactions', () => {
  it('should toggle alarm state when switch is clicked', async () => {
    const user = userEvent.setup();
    render(<AlarmCard {...mockProps} />);

    const toggleSwitch = screen.getByRole('switch', { name: /toggle.*alarm/i });
    await user.click(toggleSwitch);

    expect(mockProps.onToggle).toHaveBeenCalledWith('1', false);
  });

  it('should open edit dialog when edit button is clicked', async () => {
    const user = userEvent.setup();
    render(<AlarmCard {...mockProps} />);

    const editButton = screen.getByRole('button', { name: /edit alarm/i });
    await user.click(editButton);

    expect(mockProps.onEdit).toHaveBeenCalledWith(mockAlarm);
  });

  it('should confirm before deleting alarm', async () => {
    const user = userEvent.setup();
    render(<AlarmCard {...mockProps} />);

    const deleteButton = screen.getByRole('button', { name: /delete alarm/i });
    await user.click(deleteButton);

    // Should show confirmation dialog
    expect(screen.getByText(/are you sure you want to delete/i)).toBeInTheDocument();
    
    const confirmButton = screen.getByRole('button', { name: /confirm delete/i });
    await user.click(confirmButton);

    expect(mockProps.onDelete).toHaveBeenCalledWith('1');
  });
});
```

### 3. Form Component Testing

Test forms with validation and submission:

```typescript
describe('CreateAlarmForm', () => {
  const mockSubmit = vi.fn();

  it('should submit form with valid data', async () => {
    const user = userEvent.setup();
    render(<CreateAlarmForm onSubmit={mockSubmit} />);

    const nameInput = screen.getByLabelText(/alarm name/i);
    const timeInput = screen.getByLabelText(/trigger time/i);
    const submitButton = screen.getByRole('button', { name: /create alarm/i });

    await user.type(nameInput, 'Morning Exercise');
    await user.type(timeInput, '2025-07-20T06:00');
    await user.click(submitButton);

    expect(mockSubmit).toHaveBeenCalledWith({
      name: 'Morning Exercise',
      triggerTime: new Date('2025-07-20T06:00:00'),
      isActive: true
    });
  });

  it('should show validation errors for invalid input', async () => {
    const user = userEvent.setup();
    render(<CreateAlarmForm onSubmit={mockSubmit} />);

    const submitButton = screen.getByRole('button', { name: /create alarm/i });
    await user.click(submitButton);

    expect(screen.getByText(/alarm name is required/i)).toBeInTheDocument();
    expect(screen.getByText(/trigger time is required/i)).toBeInTheDocument();
    expect(mockSubmit).not.toHaveBeenCalled();
  });

  it('should disable submit button while submitting', async () => {
    const user = userEvent.setup();
    const slowSubmit = vi.fn(() => new Promise(resolve => setTimeout(resolve, 100)));
    
    render(<CreateAlarmForm onSubmit={slowSubmit} />);

    // Fill form with valid data
    await user.type(screen.getByLabelText(/alarm name/i), 'Test Alarm');
    await user.type(screen.getByLabelText(/trigger time/i), '2025-07-20T06:00');

    const submitButton = screen.getByRole('button', { name: /create alarm/i });
    await user.click(submitButton);

    expect(submitButton).toBeDisabled();
    expect(screen.getByText(/creating.../i)).toBeInTheDocument();
  });
});
```

### 4. Custom Hook Testing

Test custom hooks with proper setup and teardown:

```typescript
import { renderHook, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { useAlarms } from './useAlarms';

const createWrapper = () => {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  });

  return ({ children }: { children: React.ReactNode }) => (
    <QueryClientProvider client={queryClient}>
      {children}
    </QueryClientProvider>
  );
};

describe('useAlarms hook', () => {
  it('should fetch alarms successfully', async () => {
    const mockAlarms = [mockAlarm];
    vi.mocked(fetchAlarms).mockResolvedValue(mockAlarms);

    const { result } = renderHook(() => useAlarms(), { wrapper: createWrapper() });

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });

    expect(result.current.alarms).toEqual(mockAlarms);
    expect(result.current.error).toBeNull();
  });

  it('should handle fetch error gracefully', async () => {
    const mockError = new Error('Failed to fetch alarms');
    vi.mocked(fetchAlarms).mockRejectedValue(mockError);

    const { result } = renderHook(() => useAlarms(), { wrapper: createWrapper() });

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });

    expect(result.current.alarms).toEqual([]);
    expect(result.current.error).toEqual(mockError);
  });

  it('should create alarm successfully', async () => {
    const newAlarmData = { name: 'New Alarm', triggerTime: new Date() };
    vi.mocked(createAlarm).mockResolvedValue({ ...newAlarmData, id: '2' });

    const { result } = renderHook(() => useAlarms(), { wrapper: createWrapper() });

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });

    await result.current.createAlarm(newAlarmData);

    expect(vi.mocked(createAlarm)).toHaveBeenCalledWith(newAlarmData);
  });
});
```

### 5. Accessibility Testing

Include accessibility testing in your component tests:

```typescript
import { axe, toHaveNoViolations } from 'jest-axe';

expect.extend(toHaveNoViolations);

describe('AlarmCard accessibility', () => {
  it('should have no accessibility violations', async () => {
    const { container } = render(<AlarmCard {...mockProps} />);
    const results = await axe(container);
    expect(results).toHaveNoViolations();
  });

  it('should be keyboard navigable', async () => {
    const user = userEvent.setup();
    render(<AlarmCard {...mockProps} />);

    // Tab through interactive elements
    await user.tab();
    expect(screen.getByRole('switch')).toHaveFocus();

    await user.tab();
    expect(screen.getByRole('button', { name: /edit/i })).toHaveFocus();

    await user.tab();
    expect(screen.getByRole('button', { name: /delete/i })).toHaveFocus();
  });

  it('should announce state changes to screen readers', async () => {
    const user = userEvent.setup();
    render(<AlarmCard {...mockProps} />);

    const toggleSwitch = screen.getByRole('switch');
    await user.click(toggleSwitch);

    // Check for live region updates
    expect(screen.getByRole('status')).toHaveTextContent(/alarm turned off/i);
  });
});
```

### 6. Error Boundary Testing

Test error boundaries and error states:

```typescript
describe('AlarmList error handling', () => {
  it('should display error message when fetch fails', () => {
    const mockError = new Error('Network error');
    render(<AlarmList error={mockError} />);

    expect(screen.getByText(/failed to load alarms/i)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /try again/i })).toBeInTheDocument();
  });

  it('should retry loading when retry button is clicked', async () => {
    const user = userEvent.setup();
    const mockRetry = vi.fn();
    const mockError = new Error('Network error');
    
    render(<AlarmList error={mockError} onRetry={mockRetry} />);

    const retryButton = screen.getByRole('button', { name: /try again/i });
    await user.click(retryButton);

    expect(mockRetry).toHaveBeenCalled();
  });
});
```

### 7. Loading States Testing

Test loading states and skeleton components:

```typescript
describe('AlarmList loading states', () => {
  it('should show loading skeleton while fetching data', () => {
    render(<AlarmList isLoading={true} alarms={[]} />);

    expect(screen.getByTestId('alarm-skeleton')).toBeInTheDocument();
    expect(screen.queryByText(/no alarms/i)).not.toBeInTheDocument();
  });

  it('should show empty state when no alarms exist', () => {
    render(<AlarmList isLoading={false} alarms={[]} />);

    expect(screen.getByText(/no alarms yet/i)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /create your first alarm/i })).toBeInTheDocument();
  });
});
```

## Mock Service Worker (MSW) Integration

For testing components that make API calls:

```typescript
import { setupServer } from 'msw/node';
import { http, HttpResponse } from 'msw';

const server = setupServer(
  http.get('/api/alarms', () => {
    return HttpResponse.json([mockAlarm]);
  }),

  http.post('/api/alarms', async ({ request }) => {
    const newAlarm = await request.json();
    return HttpResponse.json({ ...newAlarm, id: '2' });
  })
);

beforeAll(() => server.listen());
afterEach(() => server.resetHandlers());
afterAll(() => server.close());
```

## Test Organization

Structure tests following the Atomic Design hierarchy:

```
src/
├── components/
│   ├── atoms/
│   │   └── Button/
│   │       ├── Button.tsx
│   │       └── Button.test.tsx
│   ├── molecules/
│   │   └── AlarmCard/
│   │       ├── AlarmCard.tsx
│   │       └── AlarmCard.test.tsx
│   └── organisms/
│       └── AlarmList/
│           ├── AlarmList.tsx
│           └── AlarmList.test.tsx
├── hooks/
│   ├── useAlarms.ts
│   └── useAlarms.test.ts
└── utils/
    ├── dateUtils.ts
    └── dateUtils.test.ts
```

## Quality Standards

- **Coverage**: 90%+ for components and custom hooks
- **Accessibility**: All tests should include basic accessibility checks
- **Performance**: Test suite should run in <10 seconds
- **Reliability**: Tests should be deterministic and not flaky
- **Maintainability**: Tests should be easy to update when UI changes

## Expected Output

For each component or hook to test, provide:

1. **Setup**: Proper test environment with mocks and providers
2. **Rendering Tests**: Component renders with correct content
3. **Interaction Tests**: User interactions work as expected
4. **State Tests**: Component state changes correctly
5. **Accessibility Tests**: WCAG compliance and keyboard navigation
6. **Error Handling**: Error states and boundary conditions
7. **Performance**: Loading states and async operations

Generate comprehensive, user-focused tests that ensure components work correctly for all users, including those using assistive technologies.
