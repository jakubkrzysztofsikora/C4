import { useState, useEffect, useCallback } from "react";

type Theme = "dark" | "light";

function getInitialTheme(): Theme {
  const stored = localStorage.getItem("c4-landing-theme");
  if (stored === "dark" || stored === "light") return stored;
  if (window.matchMedia("(prefers-color-scheme: light)").matches) return "light";
  return "dark";
}

export function useTheme() {
  const [theme, setTheme] = useState<Theme>(getInitialTheme);

  useEffect(() => {
    document.documentElement.setAttribute("data-theme", theme);
    localStorage.setItem("c4-landing-theme", theme);
  }, [theme]);

  const toggle = useCallback(() => {
    setTheme((t) => (t === "dark" ? "light" : "dark"));
  }, []);

  return { theme, toggle } as const;
}
