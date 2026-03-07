import { useReveal } from "@/hooks/useReveal";
import styles from "./Trust.module.css";

const STATS = [
  { value: "1000+", label: "Azure Resources Discovered" },
  { value: "< 5 min", label: "Time to First Diagram" },
  { value: "99.9%", label: "Platform Uptime" },
  { value: "Real-time", label: "Telemetry Updates" },
];

const INTEGRATIONS = [
  { name: "Azure", color: "#0078d4" },
  { name: "Terraform", color: "#7B42BC" },
  { name: "Bicep", color: "#0078d4" },
  { name: "App Insights", color: "#68217A" },
  { name: "Defender", color: "#0078d4" },
];

export function Trust() {
  const ref = useReveal();

  return (
    <section className="section" ref={ref} aria-labelledby="trust-heading">
      <div className="container">
        <div className="section-header reveal">
          <h2 id="trust-heading" className="section-title">
            Built for{" "}
            <span className="gradient-text">Enterprise Scale</span>
          </h2>
          <p className="section-subtitle">
            Trusted by platform engineering, DevOps, and SRE teams to visualize
            complex cloud architectures.
          </p>
        </div>

        <div className={`${styles.stats} reveal reveal-delay-1`}>
          {STATS.map((stat) => (
            <div key={stat.label} className={styles.stat}>
              <div className={styles.statValue}>{stat.value}</div>
              <div className={styles.statLabel}>{stat.label}</div>
            </div>
          ))}
        </div>

        <div className={`${styles.integrations} reveal reveal-delay-2`}>
          <p className={styles.integrationsLabel}>Integrates with your stack</p>
          <div className={styles.badges}>
            {INTEGRATIONS.map((integration) => (
              <div key={integration.name} className={styles.badge}>
                <span
                  className={styles.badgeDot}
                  style={{ background: integration.color }}
                />
                {integration.name}
              </div>
            ))}
          </div>
        </div>
      </div>
    </section>
  );
}
