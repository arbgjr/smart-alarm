import React, { Component, ErrorInfo, ReactNode } from 'react';
import { AlertTriangle, RefreshCw, Home } from 'lucide-react';

interface Props {
  children: ReactNode;
  fallback?: ReactNode;
  onError?: (error: Error, errorInfo: ErrorInfo) => void;
}

interface State {
  hasError: boolean;
  error: Error | null;
  errorInfo: ErrorInfo | null;
}

export class ErrorBoundary extends Component<Props, State> {
  constructor(props: Props) {
    super(props);
    this.state = {
      hasError: false,
      error: null,
      errorInfo: null
    };
  }

  static getDerivedStateFromError(error: Error): State {
    // Update state so the next render will show the fallback UI
    return {
      hasError: true,
      error,
      errorInfo: null
    };
  }

  componentDidCatch(error: Error, errorInfo: ErrorInfo) {
    // Log error details
    console.error('ErrorBoundary caught an error:', error, errorInfo);

    // Update state with error info
    this.setState({
      error,
      errorInfo
    });

    // Call custom error handler if provided
    if (this.props.onError) {
      this.props.onError(error, errorInfo);
    }

    // In production, you might want to log this to an error reporting service
    // Example: Sentry.captureException(error, { contexts: { errorInfo } });
  }

  handleRetry = () => {
    this.setState({
      hasError: false,
      error: null,
      errorInfo: null
    });
  };

  handleGoHome = () => {
    window.location.href = '/dashboard';
  };

  render() {
    if (this.state.hasError) {
      // Custom fallback UI provided
      if (this.props.fallback) {
        return this.props.fallback;
      }

      // Default error UI
      return (
        <div className="min-h-screen bg-gray-50 flex flex-col justify-center py-12 sm:px-6 lg:px-8">
          <div className="sm:mx-auto sm:w-full sm:max-w-md">
            <div className="bg-white py-8 px-4 shadow sm:rounded-lg sm:px-10">
              <div className="flex flex-col items-center text-center">
                <div className="mx-auto flex h-12 w-12 items-center justify-center rounded-full bg-red-100">
                  <AlertTriangle className="h-6 w-6 text-red-600" aria-hidden="true" />
                </div>

                <div className="mt-3">
                  <h3 className="text-lg font-medium text-gray-900">
                    Oops! Algo deu errado
                  </h3>
                  <div className="mt-2">
                    <p className="text-sm text-gray-500">
                      Ocorreu um erro inesperado. Nossa equipe foi notificada e está trabalhando para resolver o problema.
                    </p>
                  </div>
                </div>

                {/* Error details (only in development) */}
                {process.env.NODE_ENV === 'development' && this.state.error && (
                  <div className="mt-4 w-full">
                    <details className="text-left">
                      <summary className="text-sm text-gray-700 cursor-pointer hover:text-gray-900">
                        Detalhes do erro (desenvolvimento)
                      </summary>
                      <div className="mt-2 p-3 bg-red-50 rounded-md">
                        <pre className="text-xs text-red-800 whitespace-pre-wrap overflow-auto max-h-32">
                          {this.state.error.toString()}
                          {this.state.errorInfo && this.state.errorInfo.componentStack}
                        </pre>
                      </div>
                    </details>
                  </div>
                )}

                <div className="mt-6 flex flex-col sm:flex-row gap-3 w-full">
                  <button
                    type="button"
                    onClick={this.handleRetry}
                    className="flex-1 inline-flex justify-center items-center px-4 py-2 border border-transparent text-sm font-medium rounded-md text-white bg-indigo-600 hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500 transition-colors"
                  >
                    <RefreshCw className="h-4 w-4 mr-2" />
                    Tentar novamente
                  </button>

                  <button
                    type="button"
                    onClick={this.handleGoHome}
                    className="flex-1 inline-flex justify-center items-center px-4 py-2 border border-gray-300 text-sm font-medium rounded-md text-gray-700 bg-white hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500 transition-colors"
                  >
                    <Home className="h-4 w-4 mr-2" />
                    Voltar ao início
                  </button>
                </div>

                <div className="mt-4">
                  <p className="text-xs text-gray-400">
                    Se o problema persistir, entre em contato com o suporte.
                  </p>
                </div>
              </div>
            </div>
          </div>
        </div>
      );
    }

    return this.props.children;
  }
}

// Hook version for functional components that need error boundary functionality
export const useErrorHandler = () => {
  const handleError = (error: Error, errorInfo?: ErrorInfo) => {
    console.error('Unhandled error:', error, errorInfo);

    // In production, log to error reporting service
    // Example: Sentry.captureException(error, { contexts: { errorInfo } });
  };

  return handleError;
};

// Higher-order component for easy wrapping
export const withErrorBoundary = <P extends object>(
  Component: React.ComponentType<P>,
  fallback?: ReactNode
) => {
  const WrappedComponent = (props: P) => (
    <ErrorBoundary fallback={fallback}>
      <Component {...props} />
    </ErrorBoundary>
  );

  WrappedComponent.displayName = `withErrorBoundary(${Component.displayName || Component.name})`;
  return WrappedComponent;
};

export default ErrorBoundary;
