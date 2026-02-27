import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom';
import { type ReactNode } from 'react';
import { DashboardPage } from './features/dashboard/DashboardPage';
import { Layout } from './shared/components/Layout';
import { AuthPage } from './features/auth/AuthPage';
import { OrganizationProjectsPage } from './features/organization/OrganizationProjectsPage';
import { SubscriptionWizardPage } from './features/subscription/SubscriptionWizardPage';
import { AzureCallbackPage } from './features/subscription/AzureCallbackPage';
import { DiagramPage } from './features/diagram/DiagramPage';
import { AuthProvider, useAuth } from './shared/auth/AuthContext';

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
    <BrowserRouter>
      <AuthProvider>
        <AppRoutes />
      </AuthProvider>
    </BrowserRouter>
  );
}
