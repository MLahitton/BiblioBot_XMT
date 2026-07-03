import { landingCopy } from "../data/landing-copy.data";
import { ScrollReveal } from "./ScrollReveal";

export function FinalCtaSection() {
  return (
    <section className="px-6 py-[88px]">
      <ScrollReveal className="relative mx-auto max-w-5xl overflow-hidden rounded-3xl border border-border bg-card-solid px-6 py-14 text-center shadow-2xl shadow-black/25 sm:px-12">
        <div className="absolute inset-0 bg-[radial-gradient(circle_at_50%_0%,rgba(215,167,79,0.22),transparent_45%)]" />
        <div className="absolute inset-x-10 top-0 h-px bg-gradient-to-r from-transparent via-accent/70 to-transparent" />
        <div className="relative">
          <p className="text-sm font-medium uppercase tracking-[0.2em] text-accent">
            Webook premium
          </p>
          <h2 className="mx-auto mt-4 max-w-3xl text-3xl font-semibold leading-tight text-foreground sm:text-5xl">
            {landingCopy.finalCta.title}
          </h2>
          <p className="mx-auto mt-5 max-w-2xl text-base leading-7 text-muted">
            {landingCopy.finalCta.description}
          </p>
          <a
            href="#destacados"
            className="mt-8 inline-flex rounded-full bg-accent px-7 py-3 text-sm font-semibold text-background transition hover:bg-accent-soft focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent"
          >
            {landingCopy.finalCta.action}
          </a>
        </div>
      </ScrollReveal>
    </section>
  );
}
