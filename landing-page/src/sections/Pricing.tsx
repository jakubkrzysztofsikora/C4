import { useReveal } from "@/hooks/useReveal";
import styles from "./Pricing.module.css";

interface PricingTier {
  name: string;
  price: string;
  period: string;
  description: string;
  features: string[];
  cta: string;
  highlighted: boolean;
}

const TIERS: PricingTier[] = [
  {
    name: "Starter",
    price: "Free",
    period: "",
    description: "For individuals exploring their Azure architecture",
    features: [
      "1 Azure subscription",
      "Up to 50 resources",
      "C4 context & container diagrams",
      "Manual refresh",
      "SVG export",
      "Community support",
    ],
    cta: "Start Free",
    highlighted: false,
  },
  {
    name: "Professional",
    price: "$49",
    period: "/month per user",
    description: "For teams that need real-time visibility and AI insights",
    features: [
      "Unlimited subscriptions",
      "Unlimited resources",
      "All C4 diagram levels",
      "Live telemetry overlays",
      "AI threat analysis (STRIDE)",
      "IaC drift detection",
      "PDF & SVG export",
      "Priority support",
    ],
    cta: "Start Free Trial",
    highlighted: true,
  },
  {
    name: "Enterprise",
    price: "Custom",
    period: "",
    description: "For organizations with advanced security and compliance needs",
    features: [
      "Everything in Professional",
      "SSO & RBAC",
      "Audit logging",
      "Dedicated infrastructure",
      "Custom integrations",
      "SLA guarantee",
      "Onboarding & training",
      "Dedicated support engineer",
    ],
    cta: "Contact Sales",
    highlighted: false,
  },
];

export function Pricing() {
  const ref = useReveal();

  return (
    <section id="pricing" className={`section ${styles.section}`} ref={ref} aria-labelledby="pricing-heading">
      <div className="container">
        <div className="section-header reveal">
          <h2 id="pricing-heading" className="section-title">
            Simple,{" "}
            <span className="gradient-text">Transparent Pricing</span>
          </h2>
          <p className="section-subtitle">
            Start free. Scale as your team and architecture grow.
          </p>
        </div>

        <div className={styles.grid}>
          {TIERS.map((tier, i) => (
            <div
              key={tier.name}
              className={`${styles.card} ${tier.highlighted ? styles.highlighted : ""} reveal reveal-delay-${i + 1}`}
            >
              {tier.highlighted && (
                <div className={styles.popularBadge}>Most Popular</div>
              )}
              <div className={styles.cardHeader}>
                <h3 className={styles.tierName}>{tier.name}</h3>
                <div className={styles.priceRow}>
                  <span className={styles.price}>{tier.price}</span>
                  {tier.period && <span className={styles.period}>{tier.period}</span>}
                </div>
                <p className={styles.tierDesc}>{tier.description}</p>
              </div>
              <ul className={styles.features}>
                {tier.features.map((feature) => (
                  <li key={feature} className={styles.feature}>
                    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="var(--accent-2)" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true"><polyline points="20 6 9 17 4 12"/></svg>
                    {feature}
                  </li>
                ))}
              </ul>
              <button
                className={`btn ${tier.highlighted ? "btn-primary" : "btn-secondary"} ${styles.tierCta}`}
              >
                {tier.cta}
              </button>
            </div>
          ))}
        </div>
      </div>
    </section>
  );
}
