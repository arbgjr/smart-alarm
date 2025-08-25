import { create } from 'zustand';
import { persist, createJSONStorage } from 'zustand/middleware';

type Theme = 'light' | 'dark' | 'system';
type Language = 'en' | 'pt' | 'es' | 'fr';

interface NotificationSettings {
  enabled: boolean;
  sound: boolean;
  vibration: boolean;
  email: boolean;
  push: boolean;
  reminderMinutes: number;
}

interface AccessibilitySettings {
  highContrast: boolean;
  largeText: boolean;
  reducedMotion: boolean;
  screenReader: boolean;
  keyboardNavigation: boolean;
}

interface UIPreferences {
  compactMode: boolean;
  showSeconds: boolean;
  use24HourFormat: boolean;
  weekStartsOnMonday: boolean;
  dateFormat: 'MM/DD/YYYY' | 'DD/MM/YYYY' | 'YYYY-MM-DD';
  timeZone: string;
}

interface UIState {
  // Theme and appearance
  theme: Theme;
  language: Language;
  
  // Layout preferences
  sidebarCollapsed: boolean;
  showWelcomeMessage: boolean;
  
  // User preferences
  preferences: UIPreferences;
  notifications: NotificationSettings;
  accessibility: AccessibilitySettings;
  
  // UI state
  isLoading: boolean;
  modals: {
    createAlarm: boolean;
    editAlarm: boolean;
    settings: boolean;
    confirmDelete: boolean;
  };
  
  // Actions
  setTheme: (theme: Theme) => void;
  setLanguage: (language: Language) => void;
  toggleSidebar: () => void;
  setSidebarCollapsed: (collapsed: boolean) => void;
  setWelcomeMessage: (show: boolean) => void;
  setPreferences: (preferences: Partial<UIPreferences>) => void;
  setNotifications: (notifications: Partial<NotificationSettings>) => void;
  setAccessibility: (accessibility: Partial<AccessibilitySettings>) => void;
  setLoading: (loading: boolean) => void;
  
  // Modal actions
  openModal: (modal: keyof UIState['modals']) => void;
  closeModal: (modal: keyof UIState['modals']) => void;
  closeAllModals: () => void;
  
  // Utility actions
  resetToDefaults: () => void;
  applySystemTheme: () => void;
}

// Default values
const defaultPreferences: UIPreferences = {
  compactMode: false,
  showSeconds: false,
  use24HourFormat: false,
  weekStartsOnMonday: true,
  dateFormat: 'DD/MM/YYYY',
  timeZone: Intl.DateTimeFormat().resolvedOptions().timeZone,
};

const defaultNotifications: NotificationSettings = {
  enabled: true,
  sound: true,
  vibration: true,
  email: false,
  push: true,
  reminderMinutes: 5,
};

const defaultAccessibility: AccessibilitySettings = {
  highContrast: false,
  largeText: false,
  reducedMotion: false,
  screenReader: false,
  keyboardNavigation: false,
};

const defaultModals: UIState['modals'] = {
  createAlarm: false,
  editAlarm: false,
  settings: false,
  confirmDelete: false,
};

export const useUIStore = create<UIState>()(
  persist(
    (set, get) => ({
      // Initial state
      theme: 'system',
      language: 'en',
      sidebarCollapsed: false,
      showWelcomeMessage: true,
      preferences: defaultPreferences,
      notifications: defaultNotifications,
      accessibility: defaultAccessibility,
      isLoading: false,
      modals: defaultModals,

      // Theme and language actions
      setTheme: (theme) => {
        set((state) => ({ ...state, theme }));
        
        // Apply theme immediately
        if (typeof window !== 'undefined') {
          const root = document.documentElement;
          
          if (theme === 'system') {
            const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
            root.classList.toggle('dark', prefersDark);
          } else {
            root.classList.toggle('dark', theme === 'dark');
          }
        }
      },

      setLanguage: (language) =>
        set((state) => ({ ...state, language })),

      // Layout actions
      toggleSidebar: () =>
        set((state) => ({
          ...state,
          sidebarCollapsed: !state.sidebarCollapsed,
        })),

      setSidebarCollapsed: (collapsed) =>
        set((state) => ({ ...state, sidebarCollapsed: collapsed })),

      setWelcomeMessage: (show) =>
        set((state) => ({ ...state, showWelcomeMessage: show })),

      // Preferences actions
      setPreferences: (newPreferences) =>
        set((state) => ({
          ...state,
          preferences: { ...state.preferences, ...newPreferences },
        })),

      setNotifications: (newNotifications) =>
        set((state) => ({
          ...state,
          notifications: { ...state.notifications, ...newNotifications },
        })),

      setAccessibility: (newAccessibility) =>
        set((state) => ({
          ...state,
          accessibility: { ...state.accessibility, ...newAccessibility },
        })),

      setLoading: (loading) =>
        set((state) => ({ ...state, isLoading: loading })),

      // Modal actions
      openModal: (modal) =>
        set((state) => ({
          ...state,
          modals: { ...state.modals, [modal]: true },
        })),

      closeModal: (modal) =>
        set((state) => ({
          ...state,
          modals: { ...state.modals, [modal]: false },
        })),

      closeAllModals: () =>
        set((state) => ({
          ...state,
          modals: defaultModals,
        })),

      // Utility actions
      resetToDefaults: () =>
        set(() => ({
          theme: 'system',
          language: 'en',
          sidebarCollapsed: false,
          showWelcomeMessage: true,
          preferences: defaultPreferences,
          notifications: defaultNotifications,
          accessibility: defaultAccessibility,
          isLoading: false,
          modals: defaultModals,
        })),

      applySystemTheme: () => {
        const { theme } = get();
        if (theme === 'system' && typeof window !== 'undefined') {
          const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
          document.documentElement.classList.toggle('dark', prefersDark);
        }
      },
    }),
    {
      name: 'smart-alarm-ui',
      storage: createJSONStorage(() => localStorage),
      partialize: (state) => ({
        theme: state.theme,
        language: state.language,
        sidebarCollapsed: state.sidebarCollapsed,
        showWelcomeMessage: state.showWelcomeMessage,
        preferences: state.preferences,
        notifications: state.notifications,
        accessibility: state.accessibility,
      }),
      // Apply theme on rehydration
      onRehydrateStorage: () => (state) => {
        if (state) {
          state.applySystemTheme();
          
          // Listen for system theme changes
          if (typeof window !== 'undefined') {
            const mediaQuery = window.matchMedia('(prefers-color-scheme: dark)');
            mediaQuery.addEventListener('change', () => {
              if (state.theme === 'system') {
                state.applySystemTheme();
              }
            });
          }
        }
      },
    }
  )
);

// Selective hooks to prevent unnecessary re-renders
export const useTheme = () => useUIStore((state) => state.theme);
export const useLanguage = () => useUIStore((state) => state.language);
export const useSidebarState = () => 
  useUIStore((state) => ({ 
    collapsed: state.sidebarCollapsed, 
    toggle: state.toggleSidebar 
  }));

export const usePreferences = () => useUIStore((state) => state.preferences);
export const useNotificationSettings = () => useUIStore((state) => state.notifications);
export const useAccessibilitySettings = () => useUIStore((state) => state.accessibility);
export const useUILoading = () => useUIStore((state) => state.isLoading);

// Modal hooks
export const useModal = (modal: keyof UIState['modals']) =>
  useUIStore((state) => ({
    isOpen: state.modals[modal],
    open: () => state.openModal(modal),
    close: () => state.closeModal(modal),
  }));

export const useModals = () => useUIStore((state) => state.modals);

// Action hooks
export const useUIActions = () =>
  useUIStore((state) => ({
    setTheme: state.setTheme,
    setLanguage: state.setLanguage,
    setSidebarCollapsed: state.setSidebarCollapsed,
    setWelcomeMessage: state.setWelcomeMessage,
    setPreferences: state.setPreferences,
    setNotifications: state.setNotifications,
    setAccessibility: state.setAccessibility,
    setLoading: state.setLoading,
    resetToDefaults: state.resetToDefaults,
  }));

// System theme detection hook
export const useSystemTheme = () => {
  const { theme, setTheme } = useUIStore((state) => ({
    theme: state.theme,
    setTheme: state.setTheme,
  }));

  // Helper to get effective theme (resolves 'system' to actual theme)
  const getEffectiveTheme = (): 'light' | 'dark' => {
    if (theme === 'system') {
      return typeof window !== 'undefined' && 
             window.matchMedia('(prefers-color-scheme: dark)').matches
        ? 'dark'
        : 'light';
    }
    return theme;
  };

  return {
    theme,
    effectiveTheme: getEffectiveTheme(),
    setTheme,
    isSystem: theme === 'system',
  };
};