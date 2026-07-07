import { BookGrid } from "@/features/books/components/BookGrid";
import type { Book } from "@/features/books/types/book.types";
import { landingCopy } from "../data/landing-copy.data";

type FeaturedBooksSectionProps = {
  books: Book[];
  error?: string | null;
};

export function FeaturedBooksSection({ books, error }: FeaturedBooksSectionProps) {
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
      {error ? (
        <div className="border border-red-200 bg-red-50 p-6 text-sm font-bold text-red-700">
          No pudimos cargar libros desde el backend. {error}
        </div>
      ) : books.length > 0 ? (
        <BookGrid books={books} />
      ) : (
        <div className="border border-border bg-paper p-6 text-sm font-bold text-muted">
          No hay libros activos para mostrar por ahora.
        </div>
      )}
    </section>
  );
}
