import Image from "next/image";
import { landingCopy } from "../data/landing-copy.data";
import { ScrollReveal } from "./ScrollReveal";

export function HeroSection() {
  return (
    <section className="relative min-h-[calc(100svh-80px)] overflow-hidden px-6 pb-[72px] pt-16 sm:pb-24 sm:pt-[88px]">
      <div className="absolute inset-0 opacity-[0.08]">
        <Image
          src="/images/hero/paper-texture.svg"
          alt=""
          fill
          className="object-cover"
          priority
        />
      </div>
      <div className="absolute left-1/2 top-12 h-80 w-80 -translate-x-1/2 rounded-full bg-accent/10 blur-3xl" />
      <div className="relative mx-auto grid max-w-6xl items-center gap-10 lg:grid-cols-[minmax(0,1fr)_minmax(390px,470px)]">
        <ScrollReveal className="max-w-3xl" y={22}>
          <p className="text-sm font-medium uppercase tracking-[0.2em] text-accent">
            {landingCopy.hero.eyebrow}
          </p>
          <h1 className="mt-5 max-w-3xl text-5xl font-semibold leading-[1.02] text-foreground sm:text-6xl lg:text-7xl">
            {landingCopy.hero.title}
          </h1>
          <p className="mt-6 max-w-2xl text-lg leading-8 text-muted">
            {landingCopy.hero.subtitle}
          </p>
          <div className="mt-9 flex flex-wrap gap-3">
            <a
              href="#destacados"
              className="rounded-full bg-accent px-6 py-3 text-sm font-semibold text-background transition hover:bg-accent-soft focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent"
            >
              {landingCopy.hero.primaryAction}
            </a>
            <a
              href="#categorias"
              className="rounded-full border border-border px-6 py-3 text-sm font-semibold text-foreground transition hover:border-accent/60 hover:bg-white/[0.08] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent"
            >
              {landingCopy.hero.secondaryAction}
            </a>
          </div>
          <dl className="mt-10 grid max-w-lg grid-cols-3 gap-4 border-t border-border pt-6">
            <div>
              <dt className="text-2xl font-semibold text-foreground">500+</dt>
              <dd className="mt-1 text-xs text-muted">titulos curados</dd>
            </div>
            <div>
              <dt className="text-2xl font-semibold text-foreground">4.8</dt>
              <dd className="mt-1 text-xs text-muted">rating promedio</dd>
            </div>
            <div>
              <dt className="text-2xl font-semibold text-foreground">24h</dt>
              <dd className="mt-1 text-xs text-muted">seleccion agil</dd>
            </div>
          </dl>
        </ScrollReveal>
        <div className="min-h-[360px] sm:min-h-[460px] lg:min-h-[560px]" />
      </div>
    </section>
  );
}
