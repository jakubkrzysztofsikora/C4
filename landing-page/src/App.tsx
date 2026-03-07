import { Navbar } from "@/components/Navbar";
import { Footer } from "@/components/Footer";
import { Hero } from "@/sections/Hero";
import { Features } from "@/sections/Features";
import { HowItWorks } from "@/sections/HowItWorks";
import { Trust } from "@/sections/Trust";
import { Pricing } from "@/sections/Pricing";
import { useTheme } from "@/hooks/useTheme";

export function App() {
  const { theme, toggle } = useTheme();

  return (
    <>
      <a href="#main-content" className="skip-link">
        Skip to main content
      </a>
      <Navbar theme={theme} onToggleTheme={toggle} />
      <main id="main-content">
        <Hero />
        <Features />
        <Trust />
        <HowItWorks />
        <Pricing />
      </main>
      <Footer />
    </>
  );
}
