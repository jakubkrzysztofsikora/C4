import styles from "./Footer.module.css";

const FOOTER_LINKS = {
  Product: [
    { label: "Features", href: "#features" },
    { label: "Pricing", href: "#pricing" },
    { label: "How It Works", href: "#how-it-works" },
  ],
  Resources: [
    { label: "Documentation", href: "#" },
    { label: "API Reference", href: "#" },
    { label: "Changelog", href: "#" },
  ],
  Company: [
    { label: "About", href: "#" },
    { label: "Blog", href: "#" },
    { label: "Contact", href: "#" },
  ],
  Legal: [
    { label: "Privacy Policy", href: "#" },
    { label: "Terms of Service", href: "#" },
    { label: "Security", href: "#" },
  ],
};

export function Footer() {
  return (
    <footer className={styles.footer} role="contentinfo">
      <div className={`container ${styles.inner}`}>
        <div className={styles.brand}>
          <div className={styles.logo}>
            <svg width="32" height="32" viewBox="0 0 32 32" fill="none" aria-hidden="true">
              <rect width="32" height="32" rx="8" fill="var(--accent)" />
              <text x="5" y="24" fontFamily="Inter, system-ui" fontWeight="800" fontSize="22" fill="var(--bg)">C4</text>
            </svg>
            <span className={styles.logoText}>C4</span>
          </div>
          <p className={styles.tagline}>
            Dynamic architecture visualization for cloud-native teams.
          </p>
        </div>

        <div className={styles.columns}>
          {Object.entries(FOOTER_LINKS).map(([title, links]) => (
            <div key={title} className={styles.column}>
              <h4 className={styles.columnTitle}>{title}</h4>
              <ul className={styles.columnLinks}>
                {links.map((link) => (
                  <li key={link.label}>
                    <a href={link.href} className={styles.columnLink}>
                      {link.label}
                    </a>
                  </li>
                ))}
              </ul>
            </div>
          ))}
        </div>

        <div className={styles.bottom}>
          <p className={styles.copyright}>
            &copy; {new Date().getFullYear()} C4 Dynamic Architecture. All rights reserved.
          </p>
        </div>
      </div>
    </footer>
  );
}
