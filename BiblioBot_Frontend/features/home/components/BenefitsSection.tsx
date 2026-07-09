import { BookGrid } from "@/features/books/components/BookGrid";
import type { Book } from "@/features/books/types/book.types";
import { landingCopy } from "../data/landing-copy.data";

type BenefitsSectionProps = {
  books: Book[];
};

export function BenefitsSection({ books }: BenefitsSectionProps) {
  return (
    <section id="beneficios" className="px-6 py-16 lg:px-10">
      <div className="mb-6 flex items-end justify-between gap-4">
        <h2 className="max-w-2xl text-3xl font-black text-foreground sm:text-4xl">
          {landingCopy.benefits.title}
        </h2>
        <div className="hidden items-center gap-2 sm:flex">
          <button
            type="button"
            className="flex h-9 w-9 items-center justify-center rounded-full border border-border bg-paper text-lg font-bold transition hover:bg-card focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent"
            aria-label="Anterior"
          >
            &larr;
          </button>
          <button
            type="button"
            className="flex h-9 w-9 items-center justify-center rounded-full border border-border bg-paper text-lg font-bold transition hover:bg-card focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent"
            aria-label="Siguiente"
          >
            &rarr;
          </button>
        </div>
      </div>
      <BookGrid books={books} />
    </section>
  );
}
