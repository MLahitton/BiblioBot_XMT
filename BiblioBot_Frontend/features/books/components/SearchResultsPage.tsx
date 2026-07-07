import Image from "next/image";
import Link from "next/link";
import type { Book } from "../types/book.types";
import { defaultPriceLocale, priceFormatOptions } from "@/constants/currency";

type SortOption = "relevance" | "price_asc" | "price_desc" | "newest" | "oldest";

type SearchResultsPageProps = {
  books: Book[];
  query: string;
  sort: SortOption;
};

type RankedBook = {
  book: Book;
  catalogIndex: number;
  score: number;
};

const sortOptions: { value: SortOption; label: string }[] = [
  { value: "relevance", label: "Mas relevantes" },
  { value: "price_asc", label: "Menor precio" },
  { value: "price_desc", label: "Mayor precio" },
  { value: "newest", label: "Mas nuevos" },
  { value: "oldest", label: "Mas antiguos" },
];

const priceFormatter = new Intl.NumberFormat(
  defaultPriceLocale,
  priceFormatOptions,
);

function normalizeText(value: string) {
  return value
    .toLowerCase()
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "");
}

function getSearchScore(book: Book, query: string) {
  const normalizedQuery = normalizeText(query.trim());
  if (!normalizedQuery) return 1;

  const title = normalizeText(book.title);
  const author = normalizeText(book.author);
  const category = normalizeText(book.category);
  const description = normalizeText(book.description);
  const slug = normalizeText(book.slug.replaceAll("-", " "));

  let score = 0;
  if (title === normalizedQuery) score += 120;
  if (title.startsWith(normalizedQuery)) score += 80;
  if (title.includes(normalizedQuery)) score += 55;
  if (author.includes(normalizedQuery)) score += 35;
  if (category.includes(normalizedQuery)) score += 30;
  if (slug.includes(normalizedQuery)) score += 24;
  if (description.includes(normalizedQuery)) score += 12;

  return score;
}

function getSortedResults(books: Book[], query: string, sort: SortOption) {
  const rankedBooks = books
    .map((book, catalogIndex) => ({
      book,
      catalogIndex,
      score: getSearchScore(book, query),
    }))
    .filter((item) => !query.trim() || item.score > 0);

  return rankedBooks.sort((a, b) => {
    if (sort === "price_asc") return a.book.price - b.book.price;
    if (sort === "price_desc") return b.book.price - a.book.price;
    if (sort === "newest") return b.catalogIndex - a.catalogIndex;
    if (sort === "oldest") return a.catalogIndex - b.catalogIndex;

    return b.score - a.score || b.book.rating - a.book.rating;
  });
}

function getSortHref(query: string, sort: SortOption) {
  const params = new URLSearchParams();
  if (query.trim()) params.set("q", query.trim());
  params.set("sort", sort);
  return `/search?${params.toString()}`;
}

function ResultCard({ item }: { item: RankedBook }) {
  const { book } = item;

  return (
    <Link
      href={`/books/${book.slug}`}
      className="group grid gap-5 border border-border bg-paper p-4 transition hover:border-accent/40 hover:bg-card focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent sm:grid-cols-[112px_1fr_auto] sm:items-center"
    >
      <div className="relative h-44 w-28 overflow-hidden rounded-[8px] border border-white/60 bg-card shadow-[8px_14px_26px_rgba(53,30,28,0.18),inset_5px_0_0_rgba(0,0,0,0.14)] sm:h-40 sm:w-28">
        <Image
          src={book.image}
          alt={`Portada de ${book.title}`}
          fill
          className="object-cover transition duration-500 group-hover:scale-[1.03]"
          sizes="112px"
        />
        <span className="absolute inset-y-0 left-0 w-4 bg-gradient-to-r from-black/35 via-black/10 to-transparent" />
      </div>

      <div className="min-w-0">
        <p className="text-xs font-black uppercase tracking-widest text-accent">
          {book.category}
        </p>
        <h2 className="mt-2 text-xl font-black leading-7 text-foreground">
          {book.title}
        </h2>
        <p className="mt-1 text-sm font-bold text-muted">Por {book.author}</p>
        <p className="mt-3 max-w-2xl text-sm font-semibold leading-6 text-foreground/75">
          {book.description}
        </p>
        <div className="mt-3 flex flex-wrap items-center gap-2 text-xs font-bold text-muted">
          <span>{book.rating.toFixed(1)} estrellas</span>
          <span aria-hidden="true">·</span>
          <span>{book.stock > 0 ? "En stock" : "Agotado"}</span>
          {book.badge ? (
            <>
              <span aria-hidden="true">·</span>
              <span>{book.badge}</span>
            </>
          ) : null}
        </div>
      </div>

      <div className="sm:text-right">
        <p className="text-2xl font-black text-foreground">
          {priceFormatter.format(book.price)}
        </p>
        {book.previousPrice ? (
          <p className="mt-1 text-sm font-semibold text-muted line-through">
            {priceFormatter.format(book.previousPrice)}
          </p>
        ) : null}
        <span className="mt-4 inline-flex rounded-full bg-foreground px-4 py-2 text-xs font-black text-paper transition group-hover:bg-accent">
          Ver detalle
        </span>
      </div>
    </Link>
  );
}

export function SearchResultsPage({ books, query, sort }: SearchResultsPageProps) {
  const results = getSortedResults(books, query, sort);
  const hasQuery = query.trim().length > 0;

  return (
    <main className="min-h-screen bg-background px-5 pb-16 pt-24 text-foreground sm:px-8 lg:px-12">
      <div className="mx-auto max-w-6xl">
        <section className="border-b border-border/70 pb-6">
          <p className="text-xs font-black uppercase tracking-widest text-muted">
            Resultados de busqueda
          </p>
          <h1 className="mt-2 text-3xl font-black leading-tight sm:text-4xl">
            {hasQuery ? `Busqueda: "${query}"` : "Todos los libros"}
          </h1>
          <p className="mt-2 text-sm font-semibold text-muted">
            {results.length} producto{results.length === 1 ? "" : "s"} encontrado
            {results.length === 1 ? "" : "s"}
          </p>
        </section>

        <div className="mt-6 flex flex-col gap-4 lg:flex-row lg:items-start">
          <aside className="lg:sticky lg:top-32 lg:w-64 lg:shrink-0">
            <div className="border border-border bg-paper p-4">
              <p className="text-xs font-black uppercase tracking-widest text-muted">
                Ordenar por
              </p>
              <div className="mt-3 grid gap-2">
                {sortOptions.map((option) => (
                  <Link
                    key={option.value}
                    href={getSortHref(query, option.value)}
                    className={`rounded-full border px-4 py-2 text-sm font-black transition focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent ${
                      sort === option.value
                        ? "border-foreground bg-foreground text-paper"
                        : "border-border bg-background text-foreground hover:border-accent hover:text-accent"
                    }`}
                  >
                    {option.label}
                  </Link>
                ))}
              </div>
            </div>
          </aside>

          <section className="min-w-0 flex-1">
            {results.length > 0 ? (
              <div className="grid gap-4">
                {results.map((item) => (
                  <ResultCard key={item.book.id} item={item} />
                ))}
              </div>
            ) : (
              <div className="border border-border bg-paper p-8 text-center">
                <h2 className="text-xl font-black text-foreground">
                  No encontramos productos
                </h2>
                <p className="mt-2 text-sm font-semibold text-muted">
                  Prueba buscando por titulo, autor o categoria.
                </p>
              </div>
            )}
          </section>
        </div>
      </div>
    </main>
  );
}
