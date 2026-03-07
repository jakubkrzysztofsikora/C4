import { useReveal } from "@/hooks/useReveal";
import styles from "./HowItWorks.module.css";

const STEPS = [
  {
    number: "01",
    title: "Connect",
    description: "Link your Azure subscription with a single OAuth flow. No agents to install, no infrastructure to manage.",
    icon: (
      <svg width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round"><path d="M10 13a5 5 0 0 0 7.54.54l3-3a5 5 0 0 0-7.07-7.07l-1.72 1.71"/><path d="M14 11a5 5 0 0 0-7.54-.54l-3 3a5 5 0 0 0 7.07 7.07l1.71-1.71"/></svg>
    ),
  },
  {
    number: "02",
    title: "Discover",
    description: "C4 scans your environment via Azure Resource Graph, mapping every resource and dependency automatically.",
    icon: (
      <svg width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round"><circle cx="11" cy="11" r="8"/><path d="m21 21-4.3-4.3"/></svg>
    ),
  },
  {
    number: "03",
    title: "Visualize",
    description: "Interactive C4 diagrams appear with live telemetry overlays. Zoom, filter, and explore your architecture in real time.",
    icon: (
      <svg width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round"><rect x="3" y="3" width="18" height="18" rx="2"/><path d="M3 9h18"/><path d="M9 21V9"/></svg>
    ),
  },
];

export function HowItWorks() {
  const ref = useReveal();

  return (
    <section id="how-it-works" className={`section ${styles.section}`} ref={ref} aria-labelledby="how-heading">
      <div className="container">
        <div className="section-header reveal">
          <h2 id="how-heading" className="section-title">
            Up and Running in{" "}
            <span className="gradient-text">Minutes</span>
          </h2>
          <p className="section-subtitle">
            No agents, no complex setup. Connect your Azure environment and start
            visualizing immediately.
          </p>
        </div>

        <div className={styles.steps}>
          {STEPS.map((step, i) => (
            <div key={step.number} className={`${styles.step} reveal reveal-delay-${i + 1}`}>
              <div className={styles.stepNumber}>{step.number}</div>
              <div className={styles.stepIcon}>{step.icon}</div>
              <h3 className={styles.stepTitle}>{step.title}</h3>
              <p className={styles.stepDesc}>{step.description}</p>
              {i < STEPS.length - 1 && (
                <div className={styles.connector} aria-hidden="true">
                  <svg width="40" height="12" viewBox="0 0 40 12" fill="none">
                    <path d="M0 6h32m0 0l-6-5m6 5l-6 5" stroke="var(--border-hover)" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round"/>
                  </svg>
                </div>
              )}
            </div>
          ))}
        </div>
      </div>
    </section>
  );
}
