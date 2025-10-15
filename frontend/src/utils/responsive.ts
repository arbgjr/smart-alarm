// Responsive Design Utilities
import { useState, useEffect } from 'react';

export interface BreakpointConfig {
  sm: number;
  md: number;
  lg: number;
  xl: number;
  '2xl': number;
}

export const defaultBreakpoints: BreakpointConfig = {
  sm: 640,
  md: 768,
  lg: 1024,
  xl: 1280,
  '2xl': 1536
};

export type BreakpointKey = keyof BreakpointConfig;

export interface ViewportInfo {
  width: number;
  height: number;
  isMobile: boolean;
  isTablet: boolean;
  isDesktop: boolean;
  currentBreakpoint: BreakpointKey;
  orientation: 'portrait' | 'landscape';
  isTouch: boolean;
  pixelRatio: number;
}

export function useViewport(breakpoints: BreakpointConfig = defaultBreakpoints): ViewportInfo {
  const [viewport, setViewport] = useState<ViewportInfo>(() => {
    if (typeof window === 'undefined') {
      return {
        width: 1024,
        height: 768,
        isMobile: false,
        isTablet: false,
        isDesktop: true,
        currentBreakpoint: 'lg',
        orientation: 'landscape',
        isTouch: false,
        pixelRatio: 1
      };
    }

    return calculateViewport(window.innerWidth, window.innerHeight, breakpoints);
  });

  useEffect(() => {
    const handleResize = () => {
      setViewport(calculateViewport(window.innerWidth, window.innerHeight, breakpoints));
    };

    const handleOrientationChange = () => {
      // Delay to ensure dimensions are updated
      setTimeout(() => {
        setViewport(calculateViewport(window.innerWidth, window.innerHeight, breakpoints));
      }, 100);
    };

    window.addEventListener('resize', handleResize);
    window.addEventListener('orientationchange', handleOrientationChange);

    return () => {
      window.removeEventListener('resize', handleResize);
      window.removeEventListener('orientationchange', handleOrientationChange);
    };
  }, [breakpoints]);

  return viewport;
}

function calculateViewport(width: number, height: number, breakpoints: BreakpointConfig): ViewportInfo {
  const currentBreakpoint = getCurrentBreakpoint(width, breakpoints);
  const isMobile = width < breakpoints.md;
  const isTablet = width >= breakpoints.md && width < breakpoints.lg;
  const isDesktop = width >= breakpoints.lg;
  const orientation = width > height ? 'landscape' : 'portrait';
  const isTouch = 'ontouchstart' in window || navigator.maxTouchPoints > 0;
  const pixelRatio = window.devicePixelRatio || 1;

  return {
    width,
    height,
    isMobile,
    isTablet,
    isDesktop,
    currentBreakpoint,
    orientation,
    isTouch,
    pixelRatio
  };
}

function getCurrentBreakpoint(width: number, breakpoints: BreakpointConfig): BreakpointKey {
  if (width >= breakpoints['2xl']) return '2xl';
  if (width >= breakpoints.xl) return 'xl';
  if (width >= breakpoints.lg) return 'lg';
  if (width >= breakpoints.md) return 'md';
  return 'sm';
}

export function useMediaQuery(query: string): boolean {
  const [matches, setMatches] = useState(() => {
    if (typeof window === 'undefined') return false;
    return window.matchMedia(query).matches;
  });

  useEffect(() => {
    const mediaQuery = window.matchMedia(query);
    const handler = (event: MediaQueryListEvent) => setMatches(event.matches);

    mediaQuery.addEventListener('change', handler);
    return () => mediaQuery.removeEventListener('change', handler);
  }, [query]);

  return matches;
}

export function useBreakpoint(breakpoint: BreakpointKey, breakpoints: BreakpointConfig = defaultBreakpoints): boolean {
  return useMediaQuery(`(min-width: ${breakpoints[breakpoint]}px)`);
}

export function useIsMobile(): boolean {
  return useMediaQuery('(max-width: 767px)');
}

export function useIsTablet(): boolean {
  return useMediaQuery('(min-width: 768px) and (max-width: 1023px)');
}

export function useIsDesktop(): boolean {
  return useMediaQuery('(min-width: 1024px)');
}

export function usePrefersDarkMode(): boolean {
  return useMediaQuery('(prefers-color-scheme: dark)');
}

export function usePrefersReducedMotion(): boolean {
  return useMediaQuery('(prefers-reduced-motion: reduce)');
}

export function useDeviceOrientation(): 'portrait' | 'landscape' {
  const [orientation, setOrientation] = useState<'portrait' | 'landscape'>(() => {
    if (typeof window === 'undefined') return 'landscape';
    return window.innerWidth > window.innerHeight ? 'landscape' : 'portrait';
  });

  useEffect(() => {
    const handleOrientationChange = () => {
      setOrientation(window.innerWidth > window.innerHeight ? 'landscape' : 'portrait');
    };

    window.addEventListener('resize', handleOrientationChange);
    window.addEventListener('orientationchange', handleOrientationChange);

    return () => {
      window.removeEventListener('resize', handleOrientationChange);
      window.removeEventListener('orientationchange', handleOrientationChange);
    };
  }, []);

  return orientation;
}

// Responsive class utilities
export const responsiveClasses = {
  // Container classes
  container: {
    sm: 'max-w-sm mx-auto px-4',
    md: 'max-w-md mx-auto px-4',
    lg: 'max-w-lg mx-auto px-6',
    xl: 'max-w-xl mx-auto px-6',
    '2xl': 'max-w-2xl mx-auto px-8',
    full: 'max-w-7xl mx-auto px-4 sm:px-6 lg:px-8'
  },

  // Grid classes
  grid: {
    responsive: 'grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4',
    cards: 'grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6',
    dashboard: 'grid grid-cols-1 lg:grid-cols-3 gap-6'
  },

  // Flex classes
  flex: {
    responsive: 'flex flex-col md:flex-row',
    center: 'flex items-center justify-center',
    between: 'flex items-center justify-between',
    stack: 'flex flex-col space-y-4'
  },

  // Text classes
  text: {
    responsive: 'text-sm md:text-base lg:text-lg',
    heading: 'text-xl md:text-2xl lg:text-3xl font-bold',
    subheading: 'text-lg md:text-xl lg:text-2xl font-semibold'
  },

  // Spacing classes
  spacing: {
    section: 'py-8 md:py-12 lg:py-16',
    component: 'p-4 md:p-6 lg:p-8',
    gap: 'space-y-4 md:space-y-6 lg:space-y-8'
  }
};

// Utility functions for responsive design
export function getResponsiveValue<T>(
  values: Partial<Record<BreakpointKey, T>>,
  currentBreakpoint: BreakpointKey,
  fallback: T
): T {
  const breakpointOrder: BreakpointKey[] = ['sm', 'md', 'lg', 'xl', '2xl'];
  const currentIndex = breakpointOrder.indexOf(currentBreakpoint);

  // Look for the value at current breakpoint or the closest smaller one
  for (let i = currentIndex; i >= 0; i--) {
    const bp = breakpointOrder[i];
    if (values[bp] !== undefined) {
      return values[bp]!;
    }
  }

  return fallback;
}

export function clamp(value: number, min: number, max: number): number {
  return Math.min(Math.max(value, min), max);
}

export function remToPx(rem: number): number {
  return rem * parseFloat(getComputedStyle(document.documentElement).fontSize);
}

export function pxToRem(px: number): number {
  return px / parseFloat(getComputedStyle(document.documentElement).fontSize);
}
