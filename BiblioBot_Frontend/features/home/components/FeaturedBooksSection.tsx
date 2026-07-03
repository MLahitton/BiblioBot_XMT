import { BookGrid } from "@/features/books/components/BookGrid";
import type { Book } from "@/features/books/types/book.types";
import { landingCopy } from "../data/landing-copy.data";
import { ScrollReveal } from "./ScrollReveal";

type FeaturedBooksSectionProps = {
  books: Book[];
};

export function FeaturedBooksSection({ books }: FeaturedBooksSectionProps) {
  return (
    <section id="destacados" className="px-6 py-[72px]">
      <div className="mx-auto max-w-6xl">
        <ScrollReveal className="mb-8 flex flex-col gap-5 md:flex-row md:items-end md:justify-between">
          <div className="max-w-2xl">
            <p className="text-sm font-medium uppercase tracking-[0.2em] text-accent">
              {landingCopy.featuredBooks.eyebrow}
            </p>
            <h2 className="mt-3 text-3xl font-semibold leading-tight text-foreground sm:text-4xl">
              {landingCopy.featuredBooks.title}
            </h2>
            <p className="mt-3 text-muted">
              {landingCopy.featuredBooks.description}
            </p>
          </div>
          <a
            href="#categorias"
            className="w-fit rounded-full border border-border px-5 py-2.5 text-sm font-semibold text-foreground transition hover:border-accent/60 hover:bg-white/[0.08] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent"
          >
            Ver categorias
          </a>
        </ScrollReveal>
        <BookGrid books={books} />
      </div>
    </section>
  );
}
