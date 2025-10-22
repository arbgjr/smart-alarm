import React, { useEffect } from 'react';
import { Routes, Route, Navigate } from 'react-router-dom';
import { ReactQueryDevtools } from '@tanstack/react-query-devtools';
import { LoginForm } from './components/molecules/LoginForm/LoginForm';
import { RegisterForm } from './components/molecules/RegisterForm/RegisterForm';
import { ProtectedRoute, PublicRoute } from './components/molecules/ProtectedRoute/ProtectedRoute';
import { ComponentShowcase } from './components/organisms/ComponentShowcase';
import { Dashboard } from './pages/Dashboard';
import { AlarmsPage } from './pages/Alarms';
import { RoutinesPage } from './pages/Routines';
import { SettingsPage } from './pages/Settings';
import { PWAInstallPrompt } from './components/PWA/PWAInstallPrompt';
import { useAuth } from './hooks/useAuth';
import { LoadingSpinner } from './components/molecules/Loading';
import { ErrorBoundary } from './components/molecules/ErrorBoundary/ErrorBoundary';
import { useViewport } from './utils/responsive';

// App Routes Component
const AppRoutes: React.FC = () => {
  const { user, isLoading } = useAuth();
  const viewport = useViewport();

  // DEBUG: Log auth state
  useEffect(() => {
    console.log('[AppRoutes] Auth state:', { user: !!user, isLoading, hasUser: user ? `${user.name} (${user.email})` : 'none' });
  }, [user, isLoading]);

  useEffect(() => {
    // Set CSS custom properties for viewport dimensions
    document.documentElement.style.setProperty('--viewport-width', `${viewport.width}px`);
    document.documentElement.style.setProperty('--viewport-height', `${viewport.height}px`);
    document.documentElement.style.setProperty('--is-mobile', viewport.isMobile ? '1' : '0');
    document.documentElement.style.setProperty('--is-touch', viewport.isTouch ? '1' : '0');

    // Add responsive classes to body
    document.body.classList.remove('mobile', 'tablet', 'desktop', 'touch', 'no-touch');

    if (viewport.isMobile) document.body.classList.add('mobile');
    else if (viewport.isTablet) document.body.classList.add('tablet');
    else document.body.classList.add('desktop');

    if (viewport.isTouch) document.body.classList.add('touch');
    else document.body.classList.add('no-touch');
  }, [viewport]);

  if (isLoading) {
    console.log('[AppRoutes] Showing loading state');
    return (
      <div className="min-h-screen flex items-center justify-center bg-neutral-50">
        <LoadingSpinner size="lg" />
      </div>
    );
  }

  console.log('[AppRoutes] Rendering routes, user:', user ? 'authenticated' : 'not authenticated');

  return (
    <Routes>
      {/* Public Routes - Only accessible when NOT authenticated */}
      <Route
        path="/login"
        element={
          <PublicRoute>
            <LoginForm />
          </PublicRoute>
        }
      />
      <Route
        path="/register"
        element={
          <PublicRoute>
            <RegisterForm />
          </PublicRoute>
        }
      />

      {/* Development Showcase - Always accessible */}
      <Route
        path="/showcase"
        element={<ComponentShowcase />}
      />

      {/* Protected Routes - Only accessible when authenticated */}
      <Route
        path="/dashboard"
        element={
          <ProtectedRoute>
            <Dashboard />
          </ProtectedRoute>
        }
      />
      <Route
        path="/alarms"
        element={
          <ProtectedRoute>
            <AlarmsPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/routines"
        element={
          <ProtectedRoute>
            <RoutinesPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/settings"
        element={
          <ProtectedRoute>
            <SettingsPage />
          </ProtectedRoute>
        }
      />

      {/* Root redirect based on authentication */}
      <Route
        path="/"
        element={
          user ? <Navigate to="/dashboard" replace /> : <Navigate to="/login" replace />
        }
      />

      {/* Catch all - redirect to appropriate page */}
      <Route
        path="*"
        element={
          user ? <Navigate to="/dashboard" replace /> : <Navigate to="/login" replace />
        }
      />
    </Routes>
  );
};

// Main App Component
function App() {
  return (
    <ErrorBoundary>
      <div className="min-h-screen bg-neutral-50">
        <AppRoutes />
        <PWAInstallPrompt />
      </div>
      <ReactQueryDevtools initialIsOpen={false} />
    </ErrorBoundary>
  );
}

export default App;
