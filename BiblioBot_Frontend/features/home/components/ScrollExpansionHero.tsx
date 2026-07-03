"use client";

import { useEffect, useRef, useState, ReactNode } from "react";
import Image from "next/image";
import { motion } from "framer-motion";
import { landingCopy } from "../data/landing-copy.data";

interface ScrollExpansionHeroProps {
  children: ReactNode;
}

export function ScrollExpansionHero({ children }: ScrollExpansionHeroProps) {
  const [scrollProgress, setScrollProgress] = useState<number>(0);
  const [showContent, setShowContent] = useState<boolean>(false);
  const [mediaFullyExpanded, setMediaFullyExpanded] = useState<boolean>(false);
  const [touchStartY, setTouchStartY] = useState<number>(0);
  const [isMobileState, setIsMobileState] = useState<boolean>(false);

  const sectionRef = useRef<HTMLDivElement | null>(null);

  useEffect(() => {
    const handleWheel = (e: WheelEvent) => {
      // If we are fully expanded, and user scrolls UP while at the top of the page, shrink it back
      if (mediaFullyExpanded && e.deltaY < 0 && window.scrollY <= 5) {
        setMediaFullyExpanded(false);
        e.preventDefault();
      } else if (!mediaFullyExpanded) {
        // Hijack scroll to progress the expansion animation
        e.preventDefault();
        const scrollDelta = e.deltaY * 0.0012;
        const newProgress = Math.min(Math.max(scrollProgress + scrollDelta, 0), 1);
        setScrollProgress(newProgress);

        if (newProgress >= 1) {
          setMediaFullyExpanded(true);
          setShowContent(true);
        } else if (newProgress < 0.75) {
          setShowContent(false);
        }
      }
    };

    const handleTouchStart = (e: TouchEvent) => {
      setTouchStartY(e.touches[0].clientY);
    };

    const handleTouchMove = (e: TouchEvent) => {
      if (!touchStartY) return;

      const touchY = e.touches[0].clientY;
      const deltaY = touchStartY - touchY;

      if (mediaFullyExpanded && deltaY < -20 && window.scrollY <= 5) {
        setMediaFullyExpanded(false);
        e.preventDefault();
      } else if (!mediaFullyExpanded) {
        e.preventDefault();
        const scrollFactor = deltaY < 0 ? 0.008 : 0.005;
        const scrollDelta = deltaY * scrollFactor;
        const newProgress = Math.min(Math.max(scrollProgress + scrollDelta, 0), 1);
        setScrollProgress(newProgress);

        if (newProgress >= 1) {
          setMediaFullyExpanded(true);
          setShowContent(true);
        } else if (newProgress < 0.75) {
          setShowContent(false);
        }
        setTouchStartY(touchY);
      }
    };

    const handleTouchEnd = (): void => {
      setTouchStartY(0);
    };

    const handleScroll = (): void => {
      // Prevent scrolling down if not fully expanded
      if (!mediaFullyExpanded) {
        window.scrollTo(0, 0);
      }
    };

    window.addEventListener("wheel", handleWheel as unknown as EventListener, { passive: false });
    window.addEventListener("scroll", handleScroll as EventListener);
    window.addEventListener("touchstart", handleTouchStart as unknown as EventListener, { passive: false });
    window.addEventListener("touchmove", handleTouchMove as unknown as EventListener, { passive: false });
    window.addEventListener("touchend", handleTouchEnd as EventListener);

    return () => {
      window.removeEventListener("wheel", handleWheel as unknown as EventListener);
      window.removeEventListener("scroll", handleScroll as EventListener);
      window.removeEventListener("touchstart", handleTouchStart as unknown as EventListener);
      window.removeEventListener("touchmove", handleTouchMove as unknown as EventListener);
      window.removeEventListener("touchend", handleTouchEnd as EventListener);
    };
  }, [scrollProgress, mediaFullyExpanded, touchStartY]);

  useEffect(() => {
    const checkIfMobile = (): void => {
      setIsMobileState(window.innerWidth < 768);
    };
    checkIfMobile();
    window.addEventListener("resize", checkIfMobile);
    return () => window.removeEventListener("resize", checkIfMobile);
  }, []);

  // Calculate dynamic dimensions for the expanding image
  // Starts small in the center, expands to fill the screen
  const mediaWidth = isMobileState 
    ? 300 + scrollProgress * 1000 
    : 500 + scrollProgress * 1500;
  const mediaHeight = isMobileState 
    ? 300 + scrollProgress * 600 
    : 400 + scrollProgress * 800;

  // The 3D text fades out and moves up as it expands
  const textTranslateY = scrollProgress * -100;
  const textOpacity = 1 - (scrollProgress * 1.5);

  return (
    <div ref={sectionRef} className="transition-colors duration-700 ease-in-out overflow-x-hidden">
      <section className="relative flex flex-col items-center justify-start min-h-[100dvh]">
        <div className="relative w-full flex flex-col items-center min-h-[100dvh]">
          
          {/* Background behind the expanding image */}
          <motion.div
            className="absolute inset-0 z-0 h-full bg-background"
            initial={{ opacity: 1 }}
            animate={{ opacity: 1 - scrollProgress * 0.5 }}
          />

          <div className="container mx-auto flex flex-col items-center justify-start relative z-10">
            {/* Expansion Stage */}
            <div className="flex flex-col items-center justify-center w-full h-[100dvh] relative">
              
              <div
                className="absolute z-0 top-1/2 left-1/2 transform -translate-x-1/2 -translate-y-1/2 overflow-hidden rounded-2xl shadow-[0_20px_50px_rgba(0,0,0,0.3)] transition-none"
                style={{
                  width: `${mediaWidth}px`,
                  height: `${mediaHeight}px`,
                  maxWidth: scrollProgress >= 0.99 ? "100vw" : "95vw",
                  maxHeight: scrollProgress >= 0.99 ? "100vh" : "85vh",
                  borderRadius: scrollProgress >= 0.99 ? "0px" : "16px",
                }}
              >
                <div className="relative w-full h-full">
                  <Image
                    src="/images/generated/hero-library-realistic.webp"
                    alt="Biblioteca moderna luminosa"
                    fill
                    priority
                    className="object-cover object-[center_35%]"
                    sizes="100vw"
                  />
                  {/* Subtle gradient overlay to ensure text readability */}
                  <div className="absolute inset-0 bg-[linear-gradient(90deg,rgba(245,244,237,0.18),rgba(160,201,203,0.1)_35%,rgba(255,96,55,0.08)_68%,rgba(115,54,53,0.12))]" />
                  
                  {/* The 3D Books Text overlaying the image */}
                  <motion.div 
                    className="absolute inset-0 flex items-center justify-center"
                    style={{ 
                      y: textTranslateY,
                      opacity: textOpacity > 0 ? textOpacity : 0,
                      pointerEvents: textOpacity > 0 ? "auto" : "none"
                    }}
                  >
                    <div className="hero-wordmark-stage text-[4.8rem] font-black leading-[0.72] sm:text-[9rem] md:text-[12rem] lg:text-[15rem]">
                      <h1 className="hero-wordmark select-none" data-text="Books">Books</h1>
                      <span className="hero-wordmark-sheen" aria-hidden="true">Books</span>
                    </div>
                  </motion.div>
                  
                  {/* Darken the image slightly as it fully expands to prepare for content */}
                  <motion.div
                    className="absolute inset-0 bg-background/80"
                    initial={{ opacity: 0 }}
                    animate={{ opacity: scrollProgress > 0.8 ? (scrollProgress - 0.8) * 5 : 0 }}
                    style={{ pointerEvents: "none" }}
                  />
                </div>
              </div>
              
              {/* Scroll Indicator */}
              <motion.div 
                className="absolute bottom-10 flex flex-col items-center justify-center gap-2"
                style={{ opacity: textOpacity > 0 ? textOpacity : 0 }}
              >
                <span className="text-sm font-bold tracking-widest text-foreground uppercase">Desliza para explorar</span>
                <div className="w-[1px] h-12 bg-foreground/30 overflow-hidden relative">
                  <motion.div 
                    className="w-full h-full bg-foreground absolute top-0"
                    animate={{ y: ["-100%", "100%"] }}
                    transition={{ repeat: Infinity, duration: 1.5, ease: "linear" }}
                  />
                </div>
              </motion.div>

            </div>

            {/* Revealed Content */}
            <motion.section
              className="flex flex-col w-full relative z-20"
              initial={{ opacity: 0, y: 50 }}
              animate={{ opacity: showContent ? 1 : 0, y: showContent ? 0 : 50 }}
              transition={{ duration: 0.7, ease: "easeOut" }}
              style={{ pointerEvents: showContent ? "auto" : "none" }}
            >
              {/* Search Card */}
              <div className="w-full px-6 lg:px-10 -mt-24 sm:-mt-32">
                <div className="relative mx-auto flex max-w-[960px] flex-col gap-5 rounded-[22px] border border-border bg-paper px-5 py-5 shadow-[0_18px_44px_var(--shadow-warm)] sm:flex-row sm:items-center sm:justify-between sm:px-6">
                  <div>
                    <p className="text-xl font-extrabold leading-tight text-foreground">
                      {landingCopy.hero.title}
                    </p>
                    <p className="mt-1 max-w-md text-xs font-medium text-muted">
                      {landingCopy.hero.subtitle}
                    </p>
                  </div>
                  <form className="flex min-w-0 flex-1 items-center gap-2 sm:max-w-sm">
                    <label className="sr-only" htmlFor="home-search">Buscar en Webook</label>
                    <input
                      id="home-search"
                      type="search"
                      placeholder="Buscar en Webook"
                      className="h-9 min-w-0 flex-1 rounded-full border border-border bg-card px-4 text-xs font-semibold text-foreground outline-none transition placeholder:text-muted focus:border-accent"
                    />
                    <button
                      type="button"
                      className="h-9 rounded-full bg-foreground px-5 text-xs font-bold text-paper shadow-[0_10px_22px_rgba(53,30,28,0.14)] transition hover:bg-accent"
                    >
                      Buscar
                    </button>
                  </form>
                </div>
              </div>

              {/* The rest of the page passed as children */}
              <div className="w-full bg-transparent pt-12 pb-8">
                {children}
              </div>
            </motion.section>
          </div>
        </div>
      </section>
    </div>
  );
}
