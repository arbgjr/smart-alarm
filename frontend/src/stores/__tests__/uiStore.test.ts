import { describe, it, expect, beforeEach, vi, afterEach } from 'vitest';
import { renderHook, act } from '@testing-library/react';
import { useUIStore, useSystemTheme, useModal } from '../uiStore';

// Mock localStorage
const localStorageMock = {
  getItem: vi.fn(),
  setItem: vi.fn(),
  removeItem: vi.fn(),
  clear: vi.fn(),
};
Object.defineProperty(window, 'localStorage', { value: localStorageMock });

// Mock matchMedia
const mockMatchMedia = vi.fn().mockImplementation(query => ({
  matches: query.includes('dark'),
  media: query,
  onchange: null,
  addListener: vi.fn(),
  removeListener: vi.fn(),
  addEventListener: vi.fn(),
  removeEventListener: vi.fn(),
  dispatchEvent: vi.fn(),
}));
Object.defineProperty(window, 'matchMedia', { value: mockMatchMedia });

// Mock document.documentElement
const mockClassList = {
  toggle: vi.fn(),
  add: vi.fn(),
  remove: vi.fn(),
  contains: vi.fn(),
};
Object.defineProperty(document, 'documentElement', {
  value: { classList: mockClassList },
  writable: true,
});

// Mock Intl.DateTimeFormat for timezone detection
Object.defineProperty(Intl, 'DateTimeFormat', {
  value: vi.fn(() => ({
    resolvedOptions: () => ({ timeZone: 'America/New_York' })
  })),
});

describe('UIStore', () => {
  beforeEach(() => {
    // Reset store state
    useUIStore.getState().resetToDefaults();
    vi.clearAllMocks();
  });

  afterEach(() => {
    vi.clearAllMocks();
  });

  describe('Initial State', () => {
    it('should have correct initial state', () => {
      const { result } = renderHook(() => useUIStore());
      
      expect(result.current.theme).toBe('system');
      expect(result.current.language).toBe('en');
      expect(result.current.sidebarCollapsed).toBe(false);
      expect(result.current.showWelcomeMessage).toBe(true);
      expect(result.current.isLoading).toBe(false);
      
      // Check default preferences
      expect(result.current.preferences).toEqual({
        compactMode: false,
        showSeconds: false,
        use24HourFormat: false,
        weekStartsOnMonday: true,
        dateFormat: 'DD/MM/YYYY',
        timeZone: 'America/New_York',
      });
      
      // Check default modals are closed
      expect(result.current.modals.createAlarm).toBe(false);
      expect(result.current.modals.editAlarm).toBe(false);
      expect(result.current.modals.settings).toBe(false);
      expect(result.current.modals.confirmDelete).toBe(false);
    });
  });

  describe('Theme Management', () => {
    it('should set light theme', () => {
      const { result } = renderHook(() => useUIStore());
      
      act(() => {
        result.current.setTheme('light');
      });

      expect(result.current.theme).toBe('light');
      expect(mockClassList.toggle).toHaveBeenCalledWith('dark', false);
    });

    it('should set dark theme', () => {
      const { result } = renderHook(() => useUIStore());
      
      act(() => {
        result.current.setTheme('dark');
      });

      expect(result.current.theme).toBe('dark');
      expect(mockClassList.toggle).toHaveBeenCalledWith('dark', true);
    });

    it('should set system theme and detect preference', () => {
      // Mock system preference for dark theme
      mockMatchMedia.mockReturnValueOnce({
        matches: true,
        media: '(prefers-color-scheme: dark)',
        onchange: null,
        addListener: vi.fn(),
        removeListener: vi.fn(),
        addEventListener: vi.fn(),
        removeEventListener: vi.fn(),
        dispatchEvent: vi.fn(),
      });

      const { result } = renderHook(() => useUIStore());
      
      act(() => {
        result.current.setTheme('system');
      });

      expect(result.current.theme).toBe('system');
      expect(mockMatchMedia).toHaveBeenCalledWith('(prefers-color-scheme: dark)');
      expect(mockClassList.toggle).toHaveBeenCalledWith('dark', true);
    });

    it('should apply system theme correctly', () => {
      const { result } = renderHook(() => useUIStore());
      
      // Mock light system preference
      mockMatchMedia.mockReturnValueOnce({
        matches: false,
        media: '(prefers-color-scheme: dark)',
        addEventListener: vi.fn(),
      });

      act(() => {
        result.current.setTheme('system');
      });

      act(() => {
        result.current.applySystemTheme();
      });

      expect(mockClassList.toggle).toHaveBeenCalledWith('dark', false);
    });
  });

  describe('Language Management', () => {
    it('should set language', () => {
      const { result } = renderHook(() => useUIStore());
      
      act(() => {
        result.current.setLanguage('pt');
      });

      expect(result.current.language).toBe('pt');
    });

    it('should support multiple languages', () => {
      const { result } = renderHook(() => useUIStore());
      
      const languages = ['en', 'pt', 'es', 'fr'] as const;
      
      languages.forEach(lang => {
        act(() => {
          result.current.setLanguage(lang);
        });
        expect(result.current.language).toBe(lang);
      });
    });
  });

  describe('Layout Management', () => {
    it('should toggle sidebar', () => {
      const { result } = renderHook(() => useUIStore());
      
      expect(result.current.sidebarCollapsed).toBe(false);
      
      act(() => {
        result.current.toggleSidebar();
      });

      expect(result.current.sidebarCollapsed).toBe(true);
      
      act(() => {
        result.current.toggleSidebar();
      });

      expect(result.current.sidebarCollapsed).toBe(false);
    });

    it('should set sidebar collapsed state directly', () => {
      const { result } = renderHook(() => useUIStore());
      
      act(() => {
        result.current.setSidebarCollapsed(true);
      });

      expect(result.current.sidebarCollapsed).toBe(true);

      act(() => {
        result.current.setSidebarCollapsed(false);
      });

      expect(result.current.sidebarCollapsed).toBe(false);
    });

    it('should set welcome message visibility', () => {
      const { result } = renderHook(() => useUIStore());
      
      expect(result.current.showWelcomeMessage).toBe(true);
      
      act(() => {
        result.current.setWelcomeMessage(false);
      });

      expect(result.current.showWelcomeMessage).toBe(false);
    });
  });

  describe('Preferences Management', () => {
    it('should update preferences partially', () => {
      const { result } = renderHook(() => useUIStore());
      
      act(() => {
        result.current.setPreferences({
          compactMode: true,
          showSeconds: true,
        });
      });

      expect(result.current.preferences.compactMode).toBe(true);
      expect(result.current.preferences.showSeconds).toBe(true);
      expect(result.current.preferences.use24HourFormat).toBe(false); // Unchanged
    });

    it('should update notification settings', () => {
      const { result } = renderHook(() => useUIStore());
      
      act(() => {
        result.current.setNotifications({
          sound: false,
          vibration: false,
          email: true,
        });
      });

      expect(result.current.notifications.sound).toBe(false);
      expect(result.current.notifications.vibration).toBe(false);
      expect(result.current.notifications.email).toBe(true);
      expect(result.current.notifications.enabled).toBe(true); // Unchanged
    });

    it('should update accessibility settings', () => {
      const { result } = renderHook(() => useUIStore());
      
      act(() => {
        result.current.setAccessibility({
          highContrast: true,
          largeText: true,
          reducedMotion: true,
        });
      });

      expect(result.current.accessibility.highContrast).toBe(true);
      expect(result.current.accessibility.largeText).toBe(true);
      expect(result.current.accessibility.reducedMotion).toBe(true);
      expect(result.current.accessibility.screenReader).toBe(false); // Unchanged
    });
  });

  describe('Modal Management', () => {
    it('should open and close modals', () => {
      const { result } = renderHook(() => useUIStore());
      
      act(() => {
        result.current.openModal('createAlarm');
      });

      expect(result.current.modals.createAlarm).toBe(true);
      expect(result.current.modals.editAlarm).toBe(false);

      act(() => {
        result.current.closeModal('createAlarm');
      });

      expect(result.current.modals.createAlarm).toBe(false);
    });

    it('should open multiple modals', () => {
      const { result } = renderHook(() => useUIStore());
      
      act(() => {
        result.current.openModal('createAlarm');
        result.current.openModal('settings');
      });

      expect(result.current.modals.createAlarm).toBe(true);
      expect(result.current.modals.settings).toBe(true);
    });

    it('should close all modals', () => {
      const { result } = renderHook(() => useUIStore());
      
      act(() => {
        result.current.openModal('createAlarm');
        result.current.openModal('editAlarm');
        result.current.openModal('settings');
      });

      expect(result.current.modals.createAlarm).toBe(true);
      expect(result.current.modals.editAlarm).toBe(true);
      expect(result.current.modals.settings).toBe(true);

      act(() => {
        result.current.closeAllModals();
      });

      expect(result.current.modals.createAlarm).toBe(false);
      expect(result.current.modals.editAlarm).toBe(false);
      expect(result.current.modals.settings).toBe(false);
      expect(result.current.modals.confirmDelete).toBe(false);
    });
  });

  describe('Loading State', () => {
    it('should set loading state', () => {
      const { result } = renderHook(() => useUIStore());
      
      act(() => {
        result.current.setLoading(true);
      });

      expect(result.current.isLoading).toBe(true);

      act(() => {
        result.current.setLoading(false);
      });

      expect(result.current.isLoading).toBe(false);
    });
  });

  describe('Reset to Defaults', () => {
    it('should reset all settings to defaults', () => {
      const { result } = renderHook(() => useUIStore());
      
      // Change various settings
      act(() => {
        result.current.setTheme('dark');
        result.current.setLanguage('pt');
        result.current.setSidebarCollapsed(true);
        result.current.setWelcomeMessage(false);
        result.current.setLoading(true);
        result.current.openModal('createAlarm');
        result.current.setPreferences({ compactMode: true });
      });

      // Verify changes
      expect(result.current.theme).toBe('dark');
      expect(result.current.language).toBe('pt');
      expect(result.current.sidebarCollapsed).toBe(true);
      expect(result.current.showWelcomeMessage).toBe(false);
      expect(result.current.isLoading).toBe(true);
      expect(result.current.modals.createAlarm).toBe(true);
      expect(result.current.preferences.compactMode).toBe(true);

      // Reset to defaults
      act(() => {
        result.current.resetToDefaults();
      });

      // Verify reset
      expect(result.current.theme).toBe('system');
      expect(result.current.language).toBe('en');
      expect(result.current.sidebarCollapsed).toBe(false);
      expect(result.current.showWelcomeMessage).toBe(true);
      expect(result.current.isLoading).toBe(false);
      expect(result.current.modals.createAlarm).toBe(false);
      expect(result.current.preferences.compactMode).toBe(false);
    });
  });

  describe('Selective Hooks', () => {
    it('should provide useSystemTheme hook', () => {
      const { result } = renderHook(() => useSystemTheme());
      
      expect(result.current.theme).toBe('system');
      expect(result.current.isSystem).toBe(true);
      expect(result.current.effectiveTheme).toBe('light'); // Default mock
      expect(typeof result.current.setTheme).toBe('function');
    });

    it('should calculate effective theme correctly', () => {
      // Mock dark system preference
      mockMatchMedia.mockReturnValueOnce({
        matches: true,
      });

      const { result } = renderHook(() => useSystemTheme());
      
      expect(result.current.effectiveTheme).toBe('dark');

      act(() => {
        result.current.setTheme('light');
      });

      expect(result.current.theme).toBe('light');
      expect(result.current.isSystem).toBe(false);
      expect(result.current.effectiveTheme).toBe('light');
    });

    it('should provide useModal hook', () => {
      const { result } = renderHook(() => useModal('createAlarm'));
      
      expect(result.current.isOpen).toBe(false);
      expect(typeof result.current.open).toBe('function');
      expect(typeof result.current.close).toBe('function');

      act(() => {
        result.current.open();
      });

      expect(result.current.isOpen).toBe(true);

      act(() => {
        result.current.close();
      });

      expect(result.current.isOpen).toBe(false);
    });
  });

  describe('Persistence', () => {
    it('should persist UI state to localStorage', () => {
      const { result } = renderHook(() => useUIStore());
      
      act(() => {
        result.current.setTheme('dark');
        result.current.setLanguage('pt');
        result.current.setPreferences({ compactMode: true });
      });

      expect(localStorageMock.setItem).toHaveBeenCalled();
      const setItemCalls = vi.mocked(localStorageMock.setItem).mock.calls;
      const persistCall = setItemCalls.find(call => call[0] === 'smart-alarm-ui');
      
      expect(persistCall).toBeDefined();
      const persistedData = JSON.parse(persistCall![1]);
      expect(persistedData.state.theme).toBe('dark');
      expect(persistedData.state.language).toBe('pt');
      expect(persistedData.state.preferences.compactMode).toBe(true);
    });
  });

  describe('Edge Cases', () => {
    it('should handle invalid theme gracefully', () => {
      const { result } = renderHook(() => useUIStore());
      
      // This would be caught by TypeScript, but testing runtime behavior
      act(() => {
        (result.current.setTheme as any)('invalid-theme');
      });

      // Should still work and not crash
      expect(result.current.theme).toBe('invalid-theme');
    });

    it('should handle system theme changes', () => {
      const { result } = renderHook(() => useUIStore());
      
      // Mock system theme change listener
      const mockAddEventListener = vi.fn();
      mockMatchMedia.mockReturnValueOnce({
        matches: false,
        addEventListener: mockAddEventListener,
      });

      act(() => {
        result.current.setTheme('system');
      });

      // Should set up event listener for system theme changes
      expect(mockAddEventListener).toHaveBeenCalledWith('change', expect.any(Function));
    });

    it('should handle missing window object gracefully', () => {
      // Mock window as undefined (SSR scenario)
      const originalWindow = global.window;
      (global as any).window = undefined;

      const { result } = renderHook(() => useUIStore());
      
      act(() => {
        result.current.setTheme('dark');
      });

      // Should not crash
      expect(result.current.theme).toBe('dark');

      // Restore window
      global.window = originalWindow;
    });
  });
});