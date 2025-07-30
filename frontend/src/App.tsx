import React from 'react';
import { Routes, Route, Navigate } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ReactQueryDevtools } from '@tanstack/react-query-devtools';
import { LoginForm } from './components/molecules/LoginForm/LoginForm';
import { RegisterForm } from './components/molecules/RegisterForm/RegisterForm';
import { ProtectedRoute, PublicRoute } from './components/molecules/ProtectedRoute/ProtectedRoute';
import { ComponentShowcase } from './components/organisms/ComponentShowcase';
import { useAuth } from './hooks/useAuth';
import { LoadingSpinner } from './components/atoms/LoadingSpinner/LoadingSpinner';

// Create React Query client
const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: 1,
      refetchOnWindowFocus: false,
      staleTime: 5 * 60 * 1000, // 5 minutes
    },
  },
});

// Dashboard placeholder component
const Dashboard: React.FC = () => {
  const { user, logout } = useAuth();

  return (
    <div className="min-h-screen bg-neutral-50">
      <div className="container mx-auto p-8">
        <div className="flex justify-between items-center mb-8">
          <h1 className="text-3xl font-bold text-neutral-900">
            Smart Alarm Dashboard
          </h1>
          <button
            onClick={() => logout()}
            className="text-neutral-600 hover:text-neutral-800 text-sm underline"
          >
            Sair
          </button>
        </div>

        <div className="bg-white rounded-lg shadow p-6">
          <h2 className="text-xl font-semibold mb-4">Bem-vindo, {user?.name}!</h2>
          <p className="text-neutral-600 mb-4">
            Email: {user?.email}
          </p>
          <p className="text-neutral-500">
            Dashboard em desenvolvimento... 🚧
          </p>
        </div>
      </div>
    </div>
  );
};

// App Routes Component
const AppRoutes: React.FC = () => {
  const { user, isLoading } = useAuth();

  if (isLoading) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-neutral-50">
        <LoadingSpinner size="lg" />
      </div>
    );
  }

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
    <QueryClientProvider client={queryClient}>
      <div className="min-h-screen bg-neutral-50">
        <AppRoutes />
      </div>
      <ReactQueryDevtools initialIsOpen={false} />
    </QueryClientProvider>
  );
}

export default App;
