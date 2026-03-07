import { useReveal } from "@/hooks/useReveal";
import styles from "./Features.module.css";

const FEATURES = [
  {
    icon: (
      <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><circle cx="11" cy="11" r="8"/><path d="m21 21-4.3-4.3"/><path d="M11 8v6"/><path d="M8 11h6"/></svg>
    ),
    title: "Auto-Discovery",
    description: "Connect your Azure subscription and instantly map every resource, dependency, and relationship using Azure Resource Graph.",
    accent: "var(--accent)",
  },
  {
    icon: (
      <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><rect x="3" y="3" width="7" height="7"/><rect x="14" y="3" width="7" height="7"/><rect x="14" y="14" width="7" height="7"/><rect x="3" y="14" width="7" height="7"/></svg>
    ),
    title: "C4 Model Diagrams",
    description: "Automatically generate context, container, and component diagrams following the C4 model standard. Interactive, zoomable, and always up to date.",
    accent: "var(--accent-2)",
  },
  {
    icon: (
      <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><polyline points="22 12 18 12 15 21 9 3 6 12 2 12"/></svg>
    ),
    title: "Live Telemetry",
    description: "Overlay real-time metrics from Application Insights directly on your diagrams. See health, latency, and throughput at a glance.",
    accent: "var(--accent)",
  },
  {
    icon: (
      <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z"/><path d="m9 12 2 2 4-4"/></svg>
    ),
    title: "AI Threat Analysis",
    description: "Get AI-powered STRIDE threat assessments for your architecture. Identify security risks before they become vulnerabilities.",
    accent: "var(--accent-2)",
  },
  {
    icon: (
      <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M14.5 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V7.5L14.5 2z"/><polyline points="14 2 14 8 20 8"/><path d="M12 18v-6"/><path d="m9 15 3 3 3-3"/></svg>
    ),
    title: "IaC Drift Detection",
    description: "Compare live infrastructure against your Bicep and Terraform definitions. Spot configuration drift instantly with visual diffs.",
    accent: "var(--accent)",
  },
  {
    icon: (
      <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"/><polyline points="7 10 12 15 17 10"/><line x1="12" y1="15" x2="12" y2="3"/></svg>
    ),
    title: "Export & Share",
    description: "Export publication-ready diagrams as SVG or PDF. Share with stakeholders, embed in documentation, or include in architecture reviews.",
    accent: "var(--accent-2)",
  },
];

export function Features() {
  const ref = useReveal();

  return (
    <section id="features" className="section" ref={ref} aria-labelledby="features-heading">
      <div className="container">
        <div className="section-header reveal">
          <h2 id="features-heading" className="section-title">
            Everything You Need to{" "}
            <span className="gradient-text">Understand Your Architecture</span>
          </h2>
          <p className="section-subtitle">
            From auto-discovery to AI analysis, C4 gives your team complete visibility
            into your cloud infrastructure.
          </p>
        </div>

        <div className={styles.grid}>
          {FEATURES.map((feature, i) => (
            <div
              key={feature.title}
              className={`${styles.card} reveal reveal-delay-${i + 1}`}
            >
              <div className={styles.iconWrap} style={{ color: feature.accent }}>
                {feature.icon}
              </div>
              <h3 className={styles.cardTitle}>{feature.title}</h3>
              <p className={styles.cardDescription}>{feature.description}</p>
            </div>
          ))}
        </div>
      </div>
    </section>
  );
}
