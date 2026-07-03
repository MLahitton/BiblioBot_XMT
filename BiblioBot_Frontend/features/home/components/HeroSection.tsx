"use client";

import Image from "next/image";
import { landingCopy } from "../data/landing-copy.data";
import { ContainerScroll } from "@/components/ui/container-scroll-animation";

export function HeroSection() {
  return (
    <section className="flex flex-col overflow-hidden bg-background">
      <ContainerScroll
        titleComponent={
          <div className="flex flex-col items-center justify-center pb-8 sm:pb-12">
            <div className="relative flex items-center justify-center pt-4">
              <div className="relative isolate">
                <div className="hero-wordmark-stage text-[4.8rem] font-black leading-[0.72] sm:text-[9rem] md:text-[12rem] lg:text-[15rem]">
                  <h1 className="hero-wordmark select-none" data-text="Books">
                    Books
                  </h1>
                  <span className="hero-wordmark-sheen" aria-hidden="true">
                    Books
                  </span>
                </div>
              </div>
            </div>
            {/* Subtitle / Call to action */}
            <h2 className="mt-8 max-w-lg text-center text-xl font-extrabold leading-tight text-foreground sm:text-2xl">
              {landingCopy.hero.title}
            </h2>
            <p className="mt-2 max-w-md text-center text-sm font-medium text-muted">
              {landingCopy.hero.subtitle}
            </p>
          </div>
        }
      >
        <div className="relative h-full w-full">
          <Image
            src="/images/generated/hero-library-realistic.webp"
            alt="Biblioteca moderna luminosa con estanterías y sofá claro"
            fill
            priority
            className="object-cover object-center"
            sizes="(max-width: 1180px) 100vw, 1180px"
          />
          {/* Subtle gradient overlay to ensure the search bar pops */}
          <div className="absolute inset-0 bg-gradient-to-t from-black/40 via-transparent to-black/10" />
          
          <div className="absolute inset-x-0 bottom-4 flex justify-center px-4 sm:bottom-8">
            <form className="flex w-full max-w-lg items-center gap-2 rounded-full border border-white/20 bg-white/10 p-2 backdrop-blur-md shadow-2xl transition-all hover:bg-white/15">
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
        </div>
      </ContainerScroll>
    </section>
  );
}
