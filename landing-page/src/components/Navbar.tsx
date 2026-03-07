import { useState, useEffect } from "react";
import styles from "./Navbar.module.css";

interface NavbarProps {
  theme: "dark" | "light";
  onToggleTheme: () => void;
}

const NAV_LINKS = [
  { label: "Features", href: "#features" },
  { label: "How It Works", href: "#how-it-works" },
  { label: "Pricing", href: "#pricing" },
];

export function Navbar({ theme, onToggleTheme }: NavbarProps) {
  const [scrolled, setScrolled] = useState(false);
  const [menuOpen, setMenuOpen] = useState(false);

  useEffect(() => {
    const onScroll = () => setScrolled(window.scrollY > 20);
    window.addEventListener("scroll", onScroll, { passive: true });
    return () => window.removeEventListener("scroll", onScroll);
  }, []);

  return (
    <nav
      className={`${styles.navbar} ${scrolled ? styles.scrolled : ""}`}
      role="navigation"
      aria-label="Main navigation"
    >
      <div className={`container ${styles.inner}`}>
        <a href="#" className={styles.logo} aria-label="C4 Home">
          <svg width="36" height="36" viewBox="0 0 32 32" fill="none" aria-hidden="true">
            <rect width="32" height="32" rx="8" fill="var(--accent)" />
            <text x="5" y="24" fontFamily="Inter, system-ui" fontWeight="800" fontSize="22" fill="var(--bg)">C4</text>
          </svg>
          <span className={styles.logoText}>C4</span>
        </a>

        <div className={`${styles.links} ${menuOpen ? styles.open : ""}`}>
          {NAV_LINKS.map((link) => (
            <a
              key={link.href}
              href={link.href}
              className={styles.link}
              onClick={() => setMenuOpen(false)}
            >
              {link.label}
            </a>
          ))}
          <a href="#pricing" className="btn btn-primary btn-sm" onClick={() => setMenuOpen(false)}>
            Get Started
          </a>
        </div>

        <div className={styles.actions}>
          <button
            onClick={onToggleTheme}
            className={styles.themeToggle}
            aria-label={`Switch to ${theme === "dark" ? "light" : "dark"} mode`}
          >
            {theme === "dark" ? (
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><circle cx="12" cy="12" r="5"/><line x1="12" y1="1" x2="12" y2="3"/><line x1="12" y1="21" x2="12" y2="23"/><line x1="4.22" y1="4.22" x2="5.64" y2="5.64"/><line x1="18.36" y1="18.36" x2="19.78" y2="19.78"/><line x1="1" y1="12" x2="3" y2="12"/><line x1="21" y1="12" x2="23" y2="12"/><line x1="4.22" y1="19.78" x2="5.64" y2="18.36"/><line x1="18.36" y1="5.64" x2="19.78" y2="4.22"/></svg>
            ) : (
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M21 12.79A9 9 0 1 1 11.21 3 7 7 0 0 0 21 12.79z"/></svg>
            )}
          </button>

          <button
            className={styles.burger}
            onClick={() => setMenuOpen(!menuOpen)}
            aria-label={menuOpen ? "Close menu" : "Open menu"}
            aria-expanded={menuOpen}
          >
            <span className={`${styles.burgerLine} ${menuOpen ? styles.burgerOpen : ""}`} />
            <span className={`${styles.burgerLine} ${menuOpen ? styles.burgerOpen : ""}`} />
            <span className={`${styles.burgerLine} ${menuOpen ? styles.burgerOpen : ""}`} />
          </button>
        </div>
      </div>
    </nav>
  );
}
