import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom';
import { useState } from 'react';
import { DashboardPage } from './features/dashboard/DashboardPage';
import { Layout } from './shared/components/Layout';
import { AuthPage } from './features/auth/AuthPage';
import { OrganizationProjectsPage } from './features/organization/OrganizationProjectsPage';
import { SubscriptionWizardPage } from './features/subscription/SubscriptionWizardPage';
import { DiagramPage } from './features/diagram/DiagramPage';

function ProtectedRoute({ token, children }: { token: string | null; children: JSX.Element }) {
  return token ? children : <Navigate to="/login" replace />;
}

export function App() {
  const [token, setToken] = useState<string | null>(null);

  const onLogin = (value: string) => {
    setToken(value);
  };

  return (
    <BrowserRouter>
      <Routes>
        <Route path="/login" element={<AuthPage onLogin={onLogin} />} />
        <Route element={<Layout />} path="/">
          <Route index element={<ProtectedRoute token={token}><DashboardPage /></ProtectedRoute>} />
          <Route path="organizations" element={<ProtectedRoute token={token}><OrganizationProjectsPage /></ProtectedRoute>} />
          <Route path="subscriptions" element={<ProtectedRoute token={token}><SubscriptionWizardPage /></ProtectedRoute>} />
          <Route path="diagram" element={<ProtectedRoute token={token}><DiagramPage /></ProtectedRoute>} />
        </Route>
      </Routes>
    </BrowserRouter>
  );
}
