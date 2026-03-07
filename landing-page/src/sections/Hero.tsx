import { useReveal } from "@/hooks/useReveal";
import styles from "./Hero.module.css";

export function Hero() {
  const ref = useReveal();

  return (
    <section className={styles.hero} ref={ref} aria-labelledby="hero-heading">
      <div className={styles.bgEffect} aria-hidden="true">
        <div className={styles.gridOverlay} />
        <div className={styles.glowOrb1} />
        <div className={styles.glowOrb2} />
      </div>

      <div className={`container ${styles.content}`}>
        <div className={`${styles.badge} reveal`}>
          <span className={styles.badgeDot} />
          Now with AI-Powered STRIDE Analysis
        </div>

        <h1 id="hero-heading" className={`${styles.heading} reveal reveal-delay-1`}>
          See Your Azure Architecture{" "}
          <span className="gradient-text">Come Alive</span>
        </h1>

        <p className={`${styles.subheading} reveal reveal-delay-2`}>
          Auto-discover resources, generate interactive C4-model diagrams, and overlay
          real-time telemetry — all in minutes, not months.
        </p>

        <div className={`${styles.ctas} reveal reveal-delay-3`}>
          <a href="#pricing" className="btn btn-primary">
            Start Free Trial
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true"><path d="M5 12h14"/><path d="m12 5 7 7-7 7"/></svg>
          </a>
          <a href="#how-it-works" className="btn btn-secondary">
            See How It Works
          </a>
        </div>

        <div className={`${styles.heroVisual} reveal reveal-delay-4`}>
          <div className={styles.diagramMock}>
            <div className={styles.diagramHeader}>
              <span className={styles.dot} style={{ background: "#e74c3c" }} />
              <span className={styles.dot} style={{ background: "#f39c12" }} />
              <span className={styles.dot} style={{ background: "#2e8f5e" }} />
              <span className={styles.diagramTitle}>C4 Dynamic Architecture</span>
            </div>
            <div className={styles.diagramBody}>
              <div className={styles.diagramNodes}>
                <div className={`${styles.node} ${styles.nodeSystem}`}>
                  <div className={styles.nodeLabel}>System Context</div>
                  <div className={styles.nodeInner}>
                    <div className={`${styles.node} ${styles.nodeContainer}`}>
                      <div className={styles.nodeLabel}>API Gateway</div>
                      <div className={styles.nodeHealth}>
                        <span className={styles.healthDot} /> 99.9%
                      </div>
                    </div>
                    <div className={`${styles.node} ${styles.nodeContainer}`}>
                      <div className={styles.nodeLabel}>Auth Service</div>
                      <div className={styles.nodeHealth}>
                        <span className={styles.healthDot} /> 99.7%
                      </div>
                    </div>
                    <div className={`${styles.node} ${styles.nodeContainer}`}>
                      <div className={styles.nodeLabel}>Order Service</div>
                      <div className={styles.nodeHealth}>
                        <span className={`${styles.healthDot} ${styles.healthWarning}`} /> 94.2%
                      </div>
                    </div>
                    <div className={`${styles.node} ${styles.nodeDb}`}>
                      <div className={styles.nodeLabel}>PostgreSQL</div>
                      <div className={styles.nodeHealth}>
                        <span className={styles.healthDot} /> 100%
                      </div>
                    </div>
                  </div>
                </div>
              </div>
              <div className={styles.diagramOverlay}>
                <div className={styles.telemetryLine} />
                <div className={styles.telemetryPulse} />
              </div>
            </div>
          </div>
        </div>
      </div>
    </section>
  );
}
