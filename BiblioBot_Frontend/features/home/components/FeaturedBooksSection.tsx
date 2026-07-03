import { BookGrid } from "@/features/books/components/BookGrid";
import type { Book } from "@/features/books/types/book.types";
import { landingCopy } from "../data/landing-copy.data";

type FeaturedBooksSectionProps = {
  books: Book[];
};

export function FeaturedBooksSection({ books }: FeaturedBooksSectionProps) {
  return (
    <section id="destacados">
      <div className="mb-6 flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <h2 className="text-2xl font-extrabold text-foreground">
          {landingCopy.featuredBooks.title}
        </h2>
        <p className="max-w-sm text-xs font-semibold leading-5 text-muted">
          {landingCopy.featuredBooks.description}
        </p>
      </div>
      <BookGrid books={books} />
    </section>
  );
}
