"use client";

import { useCallback, useEffect, useState } from "react";
import { useReducedMotion } from "framer-motion";
import Image from "next/image";
import { ContainerScroll } from "@/components/ui/container-scroll-animation";
import { landingCopy } from "../data/landing-copy.data";

const heroSlides = [
  {
    id: "library",
    src: "/images/generated/hero-library-realistic.webp",
    alt: "Biblioteca moderna luminosa con estanterias y sofa claro",
  },
  {
    id: "reading-lounge",
    src: "/images/generated/hero-reading-lounge.webp",
    alt: "Sala de lectura moderna con estanterias, plantas y sofa claro",
  },
  {
    id: "bookstore-corner",
    src: "/images/generated/hero-bookstore-corner.webp",
    alt: "Rincon de libreria calido con libros curados y mesa baja",
  },
  {
    id: "private-library",
    src: "/images/generated/hero-private-library.webp",
    alt: "Biblioteca privada elegante con escritorio y estanterias oscuras",
  },
];

function ArrowIcon({ direction }: { direction: "previous" | "next" }) {
  return (
    <svg
      aria-hidden="true"
      className="h-4 w-4"
      fill="none"
      viewBox="0 0 24 24"
    >
      <path
        d={direction === "previous" ? "M15 5l-7 7 7 7" : "M9 5l7 7-7 7"}
        stroke="currentColor"
        strokeLinecap="round"
        strokeLinejoin="round"
        strokeWidth="2.4"
      />
    </svg>
  );
}

export function HeroSection() {
  const shouldReduceMotion = useReducedMotion();
  const [activeSlide, setActiveSlide] = useState(0);
  const [isPaused, setIsPaused] = useState(false);

  const goToPreviousSlide = useCallback(() => {
    setActiveSlide((currentSlide) =>
      currentSlide === 0 ? heroSlides.length - 1 : currentSlide - 1,
    );
  }, []);

  const goToNextSlide = useCallback(() => {
    setActiveSlide((currentSlide) => (currentSlide + 1) % heroSlides.length);
  }, []);

  useEffect(() => {
    if (shouldReduceMotion || isPaused) {
      return;
    }

    const intervalId = window.setInterval(goToNextSlide, 5500);

    return () => window.clearInterval(intervalId);
  }, [goToNextSlide, isPaused, shouldReduceMotion]);

  return (
    <section className="flex flex-col overflow-hidden bg-background">
      <ContainerScroll
        titleComponent={
          <div className="flex flex-col items-center justify-center pb-8 sm:pb-12">
            <div className="relative flex items-center justify-center pt-4">
              <div className="relative isolate">
                <div className="hero-wordmark-stage text-[4.8rem] font-black leading-[0.72] sm:text-[9rem] md:text-[12rem] lg:text-[15rem]">
                  <h1 className="hero-wordmark select-none" data-text="WeBooks">
                    WeBooks
                  </h1>
                  <span className="hero-wordmark-sheen" aria-hidden="true">
                    WeBooks
                  </span>
                </div>
              </div>
            </div>
            <h2 className="mt-8 max-w-lg text-center text-xl font-extrabold leading-tight text-foreground sm:text-2xl">
              {landingCopy.hero.title}
            </h2>
            <p className="mt-2 max-w-md text-center text-sm font-medium text-muted">
              {landingCopy.hero.subtitle}
            </p>
          </div>
        }
      >
        <div
          className="relative h-full w-full"
          onMouseEnter={() => setIsPaused(true)}
          onMouseLeave={() => setIsPaused(false)}
        >
          {heroSlides.map((slide, index) => (
            <Image
              key={slide.id}
              src={slide.src}
              alt={slide.alt}
              fill
              priority={index === 0}
              className={`object-cover object-center transition duration-700 ease-out ${
                activeSlide === index
                  ? "scale-100 opacity-100"
                  : "scale-[1.025] opacity-0"
              }`}
              sizes="(max-width: 1180px) 100vw, 1180px"
            />
          ))}

          <div className="absolute inset-0 bg-gradient-to-t from-black/26 via-black/4 to-transparent" />
          <div className="absolute inset-y-0 left-0 w-1/3 bg-gradient-to-r from-black/20 to-transparent" />
          <div className="absolute inset-y-0 right-0 w-1/3 bg-gradient-to-l from-black/12 to-transparent" />

          <button
            type="button"
            className="absolute left-3 top-1/2 hidden h-10 w-10 -translate-y-1/2 items-center justify-center rounded-full border border-white/25 bg-black/18 text-white shadow-[0_12px_28px_rgba(0,0,0,0.2)] backdrop-blur-md transition hover:bg-black/28 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent sm:flex"
            aria-label="Imagen anterior"
            onClick={goToPreviousSlide}
          >
            <ArrowIcon direction="previous" />
          </button>

          <button
            type="button"
            className="absolute right-3 top-1/2 hidden h-10 w-10 -translate-y-1/2 items-center justify-center rounded-full border border-white/25 bg-black/18 text-white shadow-[0_12px_28px_rgba(0,0,0,0.2)] backdrop-blur-md transition hover:bg-black/28 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent sm:flex"
            aria-label="Imagen siguiente"
            onClick={goToNextSlide}
          >
            <ArrowIcon direction="next" />
          </button>

          <div className="absolute inset-x-0 bottom-4 flex justify-center px-4 sm:bottom-8">
            <form className="flex w-full max-w-lg items-center gap-2 rounded-full border border-white/20 bg-white/10 p-2 shadow-2xl backdrop-blur-md transition-all hover:bg-white/15">
              <label className="sr-only" htmlFor="home-search">
                Buscar en Webook
              </label>
              <input
                id="home-search"
                type="search"
                placeholder="Buscar libros curados..."
                className="h-10 min-w-0 flex-1 rounded-full bg-transparent px-4 text-sm font-medium text-white outline-none placeholder:text-white/70"
              />
              <button
                type="button"
                className="h-10 rounded-full bg-accent px-6 text-sm font-bold text-white shadow-lg transition hover:bg-accent-soft"
              >
                Buscar
              </button>
            </form>
          </div>

          <div className="absolute bottom-20 right-5 flex items-center gap-2 sm:bottom-9 sm:right-8">
            {heroSlides.map((slide, index) => (
              <button
                key={slide.id}
                type="button"
                className={`h-2.5 rounded-full border border-white/40 transition-all focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent ${
                  activeSlide === index
                    ? "w-7 bg-white"
                    : "w-2.5 bg-white/35 hover:bg-white/70"
                }`}
                aria-label={`Ir a imagen ${index + 1}`}
                aria-current={activeSlide === index}
                onClick={() => setActiveSlide(index)}
              />
            ))}
          </div>
        </div>
      </ContainerScroll>
    </section>
  );
}
