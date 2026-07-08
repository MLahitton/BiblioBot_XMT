import Image from "next/image";
import Link from "next/link";
import type { Book } from "../types/book.types";
import { defaultPriceLocale, priceFormatOptions } from "@/constants/currency";

type SortOption = "relevance" | "price_asc" | "price_desc" | "newest" | "oldest";

type SearchFilters = {
  category: string;
  minPrice: string;
  maxPrice: string;
  minYear: string;
  maxYear: string;
};

type SearchResultsPageProps = {
  books: Book[];
  query: string;
  sort: SortOption;
  filters: SearchFilters;
  error?: string | null;
};

type RankedBook = {
  book: Book;
  catalogIndex: number;
  score: number;
};

type CategoryOption = {
  slug: string;
  name: string;
  total: number;
};

const sortOptions: { value: SortOption; label: string }[] = [
  { value: "relevance", label: "Más relevantes" },
  { value: "price_asc", label: "Menor precio" },
  { value: "price_desc", label: "Mayor precio" },
  { value: "newest", label: "Más nuevos" },
  { value: "oldest", label: "Más antiguos" },
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

function slugify(value: string) {
  return normalizeText(value)
    .replace(/[^a-z0-9]+/g, "-")
    .replace(/^-+|-+$/g, "");
}

function parsePriceFilter(value: string): number | null {
  const normalized = value.replace(/[^\d]/g, "");
  if (!normalized) return null;

  const parsed = Number.parseInt(normalized, 10);
  return Number.isNaN(parsed) ? null : parsed;
}

function parseYearFilter(value: string): number | null {
  const normalized = value.replace(/[^\d]/g, "").slice(0, 4);
  if (!normalized) return null;

  const parsed = Number.parseInt(normalized, 10);
  return Number.isNaN(parsed) ? null : parsed;
}

function formatFilterValue(value: string, type: "price" | "year") {
  const parsed = type === "price" ? parsePriceFilter(value) : parseYearFilter(value);
  if (parsed === null) return "";

  return type === "price"
    ? priceFormatter.format(parsed)
    : String(parsed);
}

function getSearchScore(book: Book, query: string) {
  const normalizedQuery = normalizeText(query.trim());
  if (!normalizedQuery) return 1;

  const title = normalizeText(book.title);
  const author = normalizeText(book.author);
  const category = normalizeText(book.category);
  const description = normalizeText(book.description);
  const publisher = normalizeText(book.publisher ?? "");
  const slug = normalizeText(book.slug.replaceAll("-", " "));

  let score = 0;
  if (title === normalizedQuery) score += 120;
  if (title.startsWith(normalizedQuery)) score += 80;
  if (title.includes(normalizedQuery)) score += 55;
  if (author.includes(normalizedQuery)) score += 35;
  if (category.includes(normalizedQuery)) score += 30;
  if (publisher.includes(normalizedQuery)) score += 18;
  if (slug.includes(normalizedQuery)) score += 24;
  if (description.includes(normalizedQuery)) score += 12;

  return score;
}

function matchesFilters(book: Book, filters: SearchFilters) {
  const minPrice = parsePriceFilter(filters.minPrice);
  const maxPrice = parsePriceFilter(filters.maxPrice);
  const minYear = parseYearFilter(filters.minYear);
  const maxYear = parseYearFilter(filters.maxYear);

  if (filters.category && slugify(book.category) !== filters.category) {
    return false;
  }

  if (minPrice !== null && book.price < minPrice) {
    return false;
  }

  if (maxPrice !== null && book.price > maxPrice) {
    return false;
  }

  if (minYear !== null || maxYear !== null) {
    if (!book.publicationYear) return false;
    if (minYear !== null && book.publicationYear < minYear) return false;
    if (maxYear !== null && book.publicationYear > maxYear) return false;
  }

  return true;
}

function getSortedResults(
  books: Book[],
  query: string,
  sort: SortOption,
  filters: SearchFilters,
) {
  const rankedBooks = books
    .map((book, catalogIndex) => ({
      book,
      catalogIndex,
      score: getSearchScore(book, query),
    }))
    .filter((item) => (!query.trim() || item.score > 0) && matchesFilters(item.book, filters));

  return rankedBooks.sort((a, b) => {
    if (sort === "price_asc") return a.book.price - b.book.price || a.catalogIndex - b.catalogIndex;
    if (sort === "price_desc") return b.book.price - a.book.price || a.catalogIndex - b.catalogIndex;
    if (sort === "newest") {
      return (b.book.publicationYear ?? 0) - (a.book.publicationYear ?? 0) || a.catalogIndex - b.catalogIndex;
    }
    if (sort === "oldest") {
      return (a.book.publicationYear ?? 9999) - (b.book.publicationYear ?? 9999) || a.catalogIndex - b.catalogIndex;
    }

    return b.score - a.score || b.book.rating - a.book.rating || a.catalogIndex - b.catalogIndex;
  });
}

function getCategoryOptions(books: Book[]): CategoryOption[] {
  const options = new Map<string, CategoryOption>();

  books.forEach((book) => {
    const categoryName = book.category.trim();
    if (!categoryName || normalizeText(categoryName) === "sin categoria") return;

    const slug = slugify(categoryName);
    const current = options.get(slug);

    options.set(slug, {
      slug,
      name: current?.name ?? categoryName,
      total: (current?.total ?? 0) + 1,
    });
  });

  return [...options.values()].sort((current, next) =>
    next.total - current.total || current.name.localeCompare(next.name),
  );
}

function getSearchHref(
  query: string,
  sort: SortOption,
  filters: SearchFilters,
  overrides: Partial<SearchFilters & { sort: SortOption }> = {},
) {
  const params = new URLSearchParams();
  const nextFilters = { ...filters, ...overrides };
  const nextSort = overrides.sort ?? sort;

  if (query.trim()) params.set("q", query.trim());
  if (nextSort !== "relevance") params.set("sort", nextSort);
  if (nextFilters.category) params.set("category", nextFilters.category);
  if (nextFilters.minPrice.trim()) params.set("minPrice", nextFilters.minPrice.trim());
  if (nextFilters.maxPrice.trim()) params.set("maxPrice", nextFilters.maxPrice.trim());
  if (nextFilters.minYear.trim()) params.set("minYear", nextFilters.minYear.trim());
  if (nextFilters.maxYear.trim()) params.set("maxYear", nextFilters.maxYear.trim());

  const queryString = params.toString();
  return queryString ? `/search?${queryString}` : "/search";
}

function getActiveFilterLabels(filters: SearchFilters, categoryOptions: CategoryOption[]) {
  const activeCategory = categoryOptions.find((category) => category.slug === filters.category);
  const labels: string[] = [];

  if (activeCategory) labels.push(activeCategory.name);
  if (filters.minPrice.trim()) labels.push(`Precio desde ${formatFilterValue(filters.minPrice, "price")}`);
  if (filters.maxPrice.trim()) labels.push(`Precio hasta ${formatFilterValue(filters.maxPrice, "price")}`);
  if (filters.minYear.trim()) labels.push(`Año desde ${formatFilterValue(filters.minYear, "year")}`);
  if (filters.maxYear.trim()) labels.push(`Año hasta ${formatFilterValue(filters.maxYear, "year")}`);

  return labels.filter(Boolean);
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
          <span aria-hidden="true">.</span>
          <span>{book.stock > 0 ? "En stock" : "Agotado"}</span>
          {book.publicationYear ? (
            <>
              <span aria-hidden="true">.</span>
              <span>{book.publicationYear}</span>
            </>
          ) : null}
          {book.publisher ? (
            <>
              <span aria-hidden="true">.</span>
              <span>{book.publisher}</span>
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

export function SearchResultsPage({
  books,
  query,
  sort,
  filters,
  error,
}: SearchResultsPageProps) {
  const categoryOptions = getCategoryOptions(books);
  const results = getSortedResults(books, query, sort, filters);
  const hasQuery = query.trim().length > 0;
  const activeCategory = categoryOptions.find((category) => category.slug === filters.category);
  const activeFilters = getActiveFilterLabels(filters, categoryOptions);

  return (
    <main className="min-h-screen bg-background px-5 pb-16 pt-36 text-foreground sm:px-8 md:pt-24 lg:px-12">
      <div className="mx-auto max-w-6xl">
        <section className="border-b border-border/70 pb-6">
          <p className="text-xs font-black uppercase tracking-widest text-muted">
            {activeCategory && !hasQuery ? "Libros por categoría" : "Resultados de búsqueda"}
          </p>
          <h1 className="mt-2 text-3xl font-black leading-tight sm:text-4xl">
            {hasQuery
              ? `Búsqueda: "${query}"`
              : activeCategory
                ? `Categoría: "${activeCategory.name}"`
                : "Todos los libros"}
          </h1>
          <p className="mt-2 text-sm font-semibold text-muted">
            {results.length} producto{results.length === 1 ? "" : "s"} encontrado
            {results.length === 1 ? "" : "s"}
          </p>

          {activeFilters.length > 0 ? (
            <div className="mt-4 flex flex-wrap gap-2">
              {activeFilters.map((label) => (
                <span
                  key={label}
                  className="rounded-full border border-[rgba(53,30,28,0.16)] bg-paper px-3 py-1 text-xs font-black text-foreground"
                >
                  {label}
                </span>
              ))}
              <Link
                href={getSearchHref(query, sort, {
                  category: "",
                  minPrice: "",
                  maxPrice: "",
                  minYear: "",
                  maxYear: "",
                })}
                className="rounded-full border border-[rgba(53,30,28,0.18)] px-3 py-1 text-xs font-black text-muted transition hover:border-accent hover:text-accent"
              >
                Limpiar filtros
              </Link>
            </div>
          ) : null}
        </section>

        <div className="mt-6 flex flex-col gap-4 lg:flex-row lg:items-start">
          <aside className="lg:sticky lg:top-32 lg:w-72 lg:shrink-0">
            <div className="border border-border bg-paper p-4">
              <p className="text-xs font-black uppercase tracking-widest text-muted">
                Ordenar por
              </p>
              <div className="mt-3 grid gap-2">
                {sortOptions.map((option) => (
                  <Link
                    key={option.value}
                    href={getSearchHref(query, sort, filters, { sort: option.value })}
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

              <div className="mt-6 border-t border-border/70 pt-5">
                <p className="text-xs font-black uppercase tracking-widest text-muted">
                  Categoría
                </p>
                <div className="mt-3 grid gap-2">
                  <Link
                    href={getSearchHref(query, sort, filters, { category: "" })}
                    className={`flex items-center justify-between rounded-full border px-4 py-2 text-sm font-black transition ${
                      !filters.category
                        ? "border-foreground bg-foreground text-paper"
                        : "border-border bg-background text-foreground hover:border-accent hover:text-accent"
                    }`}
                  >
                    Todas
                    <span className="text-xs opacity-70">{books.length}</span>
                  </Link>
                  {categoryOptions.map((category) => (
                    <Link
                      key={category.slug}
                      href={getSearchHref(query, sort, filters, { category: category.slug })}
                      className={`flex items-center justify-between rounded-full border px-4 py-2 text-sm font-black transition ${
                        filters.category === category.slug
                          ? "border-foreground bg-foreground text-paper"
                          : "border-border bg-background text-foreground hover:border-accent hover:text-accent"
                      }`}
                    >
                      <span className="min-w-0 truncate">{category.name}</span>
                      <span className="ml-3 text-xs opacity-70">{category.total}</span>
                    </Link>
                  ))}
                </div>
              </div>

              <form action="/search" method="get" className="mt-6 border-t border-border/70 pt-5">
                {query.trim() ? <input type="hidden" name="q" value={query.trim()} /> : null}
                {sort !== "relevance" ? <input type="hidden" name="sort" value={sort} /> : null}
                {filters.category ? <input type="hidden" name="category" value={filters.category} /> : null}

                <p className="text-xs font-black uppercase tracking-widest text-muted">
                  Precio
                </p>
                <div className="mt-3 grid grid-cols-2 gap-2">
                  <label className="grid gap-1 text-[0.64rem] font-black uppercase tracking-widest text-muted">
                    Mínimo
                    <input
                      name="minPrice"
                      inputMode="numeric"
                      defaultValue={filters.minPrice}
                      placeholder="COP"
                      className="h-10 min-w-0 rounded-full border border-border bg-background px-3 text-sm font-bold normal-case tracking-normal text-foreground outline-none focus:border-accent"
                    />
                  </label>
                  <label className="grid gap-1 text-[0.64rem] font-black uppercase tracking-widest text-muted">
                    Máximo
                    <input
                      name="maxPrice"
                      inputMode="numeric"
                      defaultValue={filters.maxPrice}
                      placeholder="COP"
                      className="h-10 min-w-0 rounded-full border border-border bg-background px-3 text-sm font-bold normal-case tracking-normal text-foreground outline-none focus:border-accent"
                    />
                  </label>
                </div>

                <p className="mt-5 text-xs font-black uppercase tracking-widest text-muted">
                  Año de lanzamiento
                </p>
                <div className="mt-3 grid grid-cols-2 gap-2">
                  <label className="grid gap-1 text-[0.64rem] font-black uppercase tracking-widest text-muted">
                    Desde
                    <input
                      name="minYear"
                      inputMode="numeric"
                      defaultValue={filters.minYear}
                      placeholder="2020"
                      className="h-10 min-w-0 rounded-full border border-border bg-background px-3 text-sm font-bold normal-case tracking-normal text-foreground outline-none focus:border-accent"
                    />
                  </label>
                  <label className="grid gap-1 text-[0.64rem] font-black uppercase tracking-widest text-muted">
                    Hasta
                    <input
                      name="maxYear"
                      inputMode="numeric"
                      defaultValue={filters.maxYear}
                      placeholder="2026"
                      className="h-10 min-w-0 rounded-full border border-border bg-background px-3 text-sm font-bold normal-case tracking-normal text-foreground outline-none focus:border-accent"
                    />
                  </label>
                </div>

                <button
                  type="submit"
                  className="mt-4 h-10 w-full rounded-full bg-foreground px-4 text-xs font-black text-paper transition hover:bg-accent focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent"
                >
                  Aplicar filtros
                </button>
              </form>
            </div>
          </aside>

          <section className="min-w-0 flex-1">
            {error ? (
              <div className="border border-red-200 bg-red-50 p-8 text-center">
                <h2 className="text-xl font-black text-red-700">
                  No pudimos consultar el catálogo
                </h2>
                <p className="mt-2 text-sm font-semibold text-red-700/80">
                  {error}
                </p>
              </div>
            ) : results.length > 0 ? (
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
                  Prueba ajustando categoría, precio, año o palabras de búsqueda.
                </p>
              </div>
            )}
          </section>
        </div>
      </div>
    </main>
  );
}
