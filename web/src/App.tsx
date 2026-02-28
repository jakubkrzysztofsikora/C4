import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom';
import { Component, type ReactNode, type ErrorInfo } from 'react';
import { DashboardPage } from './features/dashboard/DashboardPage';
import { Layout } from './shared/components/Layout';
import { AuthPage } from './features/auth/AuthPage';
import { OrganizationProjectsPage } from './features/organization/OrganizationProjectsPage';
import { SubscriptionWizardPage } from './features/subscription/SubscriptionWizardPage';
import { AzureCallbackPage } from './features/subscription/AzureCallbackPage';
import { DiagramPage } from './features/diagram/DiagramPage';
import { AuthProvider, useAuth } from './shared/auth/AuthContext';
import { SearchProvider } from './shared/search/SearchContext';

class ErrorBoundary extends Component<
  { children: ReactNode },
  { hasError: boolean; errorMessage: string }
> {
  constructor(props: { children: ReactNode }) {
    super(props);
    this.state = { hasError: false, errorMessage: '' };
  }

  static getDerivedStateFromError(error: Error) {
    return { hasError: true, errorMessage: error.message };
  }

  componentDidCatch(error: Error, info: ErrorInfo) {
    console.error('Uncaught error:', error, info.componentStack);
  }

  render() {
    if (this.state.hasError) {
      return (
        <div style={{
          display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center',
          minHeight: '100vh', gap: 16, padding: 32,
          background: 'var(--bg, #0f1b2d)', color: 'var(--text, #e0e6f0)',
        }}>
          <h1 style={{ margin: 0 }}>Something went wrong</h1>
          <p style={{ color: 'var(--subtle, #8896b3)', maxWidth: 400, textAlign: 'center' }}>
            {this.state.errorMessage || 'An unexpected error occurred.'}
          </p>
          <button
            className="btn btn-primary"
            type="button"
            onClick={() => {
              this.setState({ hasError: false, errorMessage: '' });
              window.location.href = '/';
            }}
          >
            Return to Dashboard
          </button>
        </div>
      );
    }
    return this.props.children;
  }
}

function ProtectedRoute({ children }: { children: ReactNode }) {
  const { isAuthenticated } = useAuth();
  return isAuthenticated ? children : <Navigate to="/login" replace />;
}

function AppRoutes() {
  return (
    <Routes>
      <Route path="/login" element={<AuthPage />} />
      <Route element={<Layout />} path="/">
        <Route index element={<ProtectedRoute><DashboardPage /></ProtectedRoute>} />
        <Route path="organizations" element={<ProtectedRoute><OrganizationProjectsPage /></ProtectedRoute>} />
        <Route path="subscriptions" element={<ProtectedRoute><SubscriptionWizardPage /></ProtectedRoute>} />
        <Route path="azure/callback" element={<ProtectedRoute><AzureCallbackPage /></ProtectedRoute>} />
        <Route path="diagram" element={<ProtectedRoute><DiagramPage /></ProtectedRoute>} />
        <Route path="diagram/:projectId" element={<ProtectedRoute><DiagramPage /></ProtectedRoute>} />
      </Route>
    </Routes>
  );
}

export function App() {
  return (
    <ErrorBoundary>
      <BrowserRouter>
        <AuthProvider>
          <SearchProvider>
            <AppRoutes />
          </SearchProvider>
        </AuthProvider>
      </BrowserRouter>
    </ErrorBoundary>
  );
}
