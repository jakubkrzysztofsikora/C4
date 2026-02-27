import { useCallback, useEffect, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { MdBusiness, MdCloud, MdHub, MdCheckCircle, MdArrowForward, MdSearch } from 'react-icons/md';
import { getJsonOrNull, postJson, deleteJson } from '../../shared/api/client';
import { useDashboard } from './useDashboard';

type SetupStatus = {
  hasOrganization: boolean;
  organizationName: string;
  hasProject: boolean;
  projectId: string;
  projectName: string;
  hasSubscription: boolean;
  subscriptionId: string;
  externalSubscriptionId: string;
  subscriptionName: string;
  loading: boolean;
};

type OrgResponse = {
  organizationId: string;
  name: string;
  projects: ReadonlyArray<{ projectId: string; name: string }>;
};

type SubResponse = {
  subscriptionId: string;
  externalSubscriptionId: string;
  displayName: string;
};

function useSetupStatus(): SetupStatus {
  const [status, setStatus] = useState<SetupStatus>({
    hasOrganization: false,
    organizationName: '',
    hasProject: false,
    projectId: '',
    projectName: '',
    hasSubscription: false,
    subscriptionId: '',
    externalSubscriptionId: '',
    subscriptionName: '',
    loading: true,
  });

  useEffect(() => {
    let cancelled = false;
    async function load() {
      const [org, sub] = await Promise.all([
        getJsonOrNull<OrgResponse>('/api/organizations/current'),
        getJsonOrNull<SubResponse>('/api/discovery/subscriptions/current'),
      ]);
      if (cancelled) return;
      const hasOrg = org !== null;
      const firstProject = org?.projects[0];
      setStatus({
        hasOrganization: hasOrg,
        organizationName: org?.name ?? '',
        hasProject: firstProject !== undefined,
        projectId: firstProject?.projectId ?? '',
        projectName: firstProject?.name ?? '',
        hasSubscription: sub !== null,
        subscriptionId: sub?.subscriptionId ?? '',
        externalSubscriptionId: sub?.externalSubscriptionId ?? '',
        subscriptionName: sub?.displayName ?? '',
        loading: false,
      });
    }
    void load();
    return () => { cancelled = true; };
  }, []);

  return status;
}

function SetupStep({ step, title, description, done, doneLabel, actionLabel, to }: {
  step: number;
  title: string;
  description: string;
  done: boolean;
  doneLabel: string;
  actionLabel: string;
  to: string;
}) {
  return (
    <Link
      to={to}
      style={{
        display: 'flex',
        alignItems: 'center',
        gap: 14,
        padding: '16px 18px',
        background: done ? 'rgba(46,143,94,0.06)' : 'var(--panel-2)',
        border: `1px solid ${done ? 'var(--success)' : 'var(--border)'}`,
        borderRadius: 12,
        textDecoration: 'none',
        color: 'inherit',
        transition: 'all 0.15s ease',
      }}
    >
      {done ? (
        <MdCheckCircle size={28} style={{ color: 'var(--success)', flexShrink: 0 }} />
      ) : (
        <div style={{
          width: 28,
          height: 28,
          borderRadius: '50%',
          border: '2px solid var(--border)',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          fontSize: 13,
          fontWeight: 700,
          color: 'var(--muted)',
          flexShrink: 0,
        }}>
          {step}
        </div>
      )}
      <div style={{ flex: 1, minWidth: 0 }}>
        <div style={{ fontWeight: 600, fontSize: 15 }}>{title}</div>
        <div style={{ fontSize: 13, color: 'var(--muted)', marginTop: 2 }}>
          {done ? doneLabel : description}
        </div>
      </div>
      {!done && <MdArrowForward size={18} style={{ color: 'var(--muted)', flexShrink: 0 }} />}
    </Link>
  );
}

type DiscoverRequest = {
  externalSubscriptionId: string;
  projectId: string;
  organizationId: string | null;
  sources: null;
};

type DiscoverResponse = {
  subscriptionId: string;
  resourcesCount: number;
  status: string;
  escalationLevel: string;
  userActionHint: string;
  dataQualityFailures: number;
};

export function DashboardPage() {
  const setup = useSetupStatus();
  const navigate = useNavigate();
  const [projectIdInput, setProjectIdInput] = useState('');
  const [activeProjectId, setActiveProjectId] = useState<string | undefined>(undefined);
  const [discovering, setDiscovering] = useState(false);
  const { graph, loading, error, graphNotFound, refetch } = useDashboard(activeProjectId);

  const setupComplete = setup.hasOrganization && setup.hasProject && setup.hasSubscription;

  useEffect(() => {
    if (setupComplete && setup.projectId.length > 0 && activeProjectId === undefined) {
      setActiveProjectId(setup.projectId);
    }
  }, [setupComplete, setup.projectId, activeProjectId]);

  useEffect(() => {
    if (!setupComplete && setup.hasProject && setup.projectId.length > 0 && activeProjectId === undefined) {
      setProjectIdInput(setup.projectId);
    }
  }, [setupComplete, setup.hasProject, setup.projectId, activeProjectId]);

  function handleLoadProject() {
    if (!projectIdInput) return;
    setActiveProjectId(projectIdInput);
  }

  const handleDiscover = useCallback(async () => {
    if (!setup.subscriptionId || !setup.projectId) return;
    setDiscovering(true);
    await postJson<DiscoverRequest, DiscoverResponse>(
      `/api/discovery/subscriptions/${setup.subscriptionId}/discover`,
      {
        externalSubscriptionId: setup.externalSubscriptionId,
        projectId: setup.projectId,
        organizationId: null,
        sources: null,
      },
    ).catch(() => undefined);
    setDiscovering(false);
    await refetch(setup.projectId);
  }, [setup.subscriptionId, setup.externalSubscriptionId, setup.projectId, refetch]);

  const handleRediscover = useCallback(async () => {
    if (!setup.subscriptionId || !setup.projectId) return;
    setDiscovering(true);
    await deleteJson(`/api/projects/${setup.projectId}/graph`).catch(() => undefined);
    await postJson<DiscoverRequest, DiscoverResponse>(
      `/api/discovery/subscriptions/${setup.subscriptionId}/discover`,
      {
        externalSubscriptionId: setup.externalSubscriptionId,
        projectId: setup.projectId,
        organizationId: null,
        sources: null,
      },
    ).catch(() => undefined);
    setDiscovering(false);
    await refetch(setup.projectId);
  }, [setup.subscriptionId, setup.externalSubscriptionId, setup.projectId, refetch]);

  if (setup.loading) {
    return (
      <section className="fade-in">
        <h1 style={{ marginTop: 0, marginBottom: 4 }}>Dashboard</h1>
        <div className="card">
          <div style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
            <div className="skeleton" style={{ height: 20, width: '40%' }} />
            <div className="skeleton" style={{ height: 48, borderRadius: 12 }} />
            <div className="skeleton" style={{ height: 48, borderRadius: 12 }} />
            <div className="skeleton" style={{ height: 48, borderRadius: 12 }} />
          </div>
        </div>
      </section>
    );
  }

  return (
    <section className="fade-in">
      <h1 style={{ marginTop: 0, marginBottom: 4 }}>Dashboard</h1>
      <p className="subtle" style={{ marginTop: 0, marginBottom: 20 }}>
        {setupComplete
          ? `${setup.organizationName} \u2014 ${setup.projectName}`
          : 'Complete the setup steps below to start discovering your architecture.'}
      </p>

      {!setupComplete && (
        <div className="card" style={{ marginBottom: 16 }}>
          <h2 style={{ margin: '0 0 4px 0', fontSize: 17 }}>Get Started</h2>
          <p className="subtle" style={{ margin: '0 0 16px 0', fontSize: 14 }}>
            Set up your workspace in three steps.
          </p>
          <div style={{ display: 'flex', flexDirection: 'column', gap: 10 }}>
            <SetupStep
              step={1}
              title="Create Organization"
              description="Register your organization and create a project."
              done={setup.hasOrganization && setup.hasProject}
              doneLabel={`${setup.organizationName} \u2014 ${setup.projectName}`}
              actionLabel="Set up"
              to="/organizations"
            />
            <SetupStep
              step={2}
              title="Connect Azure"
              description="Sign in with Microsoft to link your Azure subscription."
              done={setup.hasSubscription}
              doneLabel={setup.subscriptionName}
              actionLabel="Connect"
              to="/subscriptions"
            />
            <SetupStep
              step={3}
              title="Explore Architecture"
              description="View your C4 architecture diagram and discover resources."
              done={false}
              doneLabel=""
              actionLabel="Explore"
              to={setup.projectId.length > 0 ? `/diagram/${setup.projectId}` : '/diagram'}
            />
          </div>
        </div>
      )}

      {!setupComplete && (
        <div className="card" style={{ marginBottom: 16 }}>
          <div style={{ display: 'flex', gap: 10, alignItems: 'flex-end', flexWrap: 'wrap' }}>
            <div className="form-group" style={{ flex: 1, minWidth: 200 }}>
              <label className="form-label" htmlFor="project-id-input">Project ID</label>
              <input
                className="input"
                id="project-id-input"
                placeholder="Enter project ID to load graph"
                value={projectIdInput}
                onChange={(e) => setProjectIdInput(e.target.value)}
                disabled={loading}
                onKeyDown={(e) => e.key === 'Enter' && handleLoadProject()}
              />
            </div>
            <button
              className="btn btn-primary"
              onClick={handleLoadProject}
              disabled={loading || !projectIdInput}
              style={{ alignSelf: 'flex-end' }}
              type="button"
            >
              {loading ? (
                <>
                  <span className="spinner spinner-sm" />
                  Loading...
                </>
              ) : (
                <>
                  <MdSearch size={16} />
                  Load Project
                </>
              )}
            </button>
          </div>
        </div>
      )}

      {graphNotFound && !loading && activeProjectId !== undefined && (
        <div className="card" style={{ marginBottom: 16 }}>
          <div className="empty-state">
            <MdCloud className="empty-state-icon" />
            <p className="empty-state-title">No architecture graph yet</p>
            <p className="empty-state-description">
              Run discovery to scan your Azure subscription and build the architecture graph for this project.
            </p>
            {setup.subscriptionId && (
              <button
                className="btn btn-primary"
                type="button"
                disabled={discovering}
                onClick={() => void handleDiscover()}
              >
                {discovering ? (
                  <>
                    <span className="spinner spinner-sm" />
                    Discovering...
                  </>
                ) : (
                  'Discover Resources'
                )}
              </button>
            )}
          </div>
        </div>
      )}

      {error !== undefined && (
        <div className="card" style={{ borderColor: 'var(--error)', marginBottom: 16 }}>
          <p style={{ color: 'var(--error)', margin: 0 }}>{error}</p>
        </div>
      )}

      {discovering && (
        <div className="card" style={{ marginBottom: 16 }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
            <span className="spinner spinner-sm" />
            <span>Discovering resources...</span>
          </div>
        </div>
      )}

      {loading && activeProjectId !== undefined && (
        <div className="card">
          <div style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
            <div className="skeleton" style={{ height: 20, width: '40%' }} />
            <div className="skeleton" style={{ height: 14, width: '25%' }} />
            <div style={{ display: 'flex', flexDirection: 'column', gap: 8, marginTop: 8 }}>
              {[1, 2, 3].map(i => (
                <div key={i} className="skeleton" style={{ height: 40, borderRadius: 8 }} />
              ))}
            </div>
          </div>
        </div>
      )}

      {graph !== undefined && !loading && (
        <div className="card fade-in">
          <h2 style={{ marginTop: 0, marginBottom: 4 }}>Project: {graph.projectId}</h2>
          <p className="subtle" style={{ marginTop: 0, marginBottom: 16 }}>
            {graph.nodes.length} node{graph.nodes.length !== 1 ? 's' : ''} &bull; {graph.edges.length} edge{graph.edges.length !== 1 ? 's' : ''}
          </p>
          <div style={{ display: 'flex', gap: 8, marginBottom: 16 }}>
            <button
              className="btn btn-primary btn-sm"
              type="button"
              onClick={() => void navigate(`/diagram/${graph.projectId}`)}
            >
              <MdHub size={14} />
              View Diagram
            </button>
            <button
              className="btn btn-sm"
              type="button"
              disabled={discovering}
              onClick={() => void handleRediscover()}
            >
              {discovering ? (
                <>
                  <span className="spinner spinner-sm" />
                  Rediscovering...
                </>
              ) : (
                <>
                  <MdCloud size={14} />
                  Rediscover
                </>
              )}
            </button>
          </div>
          <h3 style={{ marginTop: 0, marginBottom: 10 }}>Resources</h3>
          <ul style={{ listStyle: 'none', padding: 0, margin: 0, display: 'flex', flexDirection: 'column', gap: 8 }}>
            {graph.nodes.map((node) => (
              <li
                key={node.id}
                style={{
                  padding: '10px 14px',
                  background: 'var(--panel-2)',
                  border: '1px solid var(--border)',
                  borderRadius: 8,
                  display: 'flex',
                  justifyContent: 'space-between',
                  alignItems: 'center',
                  gap: 8,
                  flexWrap: 'wrap',
                }}
              >
                <strong>{node.name}</strong>
                <span className="subtle" style={{ fontSize: 13 }}>
                  {node.level} &bull; {node.externalResourceId}
                </span>
              </li>
            ))}
          </ul>
        </div>
      )}

      {graph === undefined && activeProjectId === undefined && !loading && setupComplete && !discovering && (
        <div className="card">
          <div className="empty-state">
            <MdHub className="empty-state-icon" />
            <p className="empty-state-title">No project loaded</p>
            <p className="empty-state-description">
              Architecture data is still loading. If discovery has not run yet, use the Discover Resources button above.
            </p>
          </div>
        </div>
      )}
    </section>
  );
}
