import type { Metadata } from "next";
import { Header } from "@/components/layout/Header";
import { SearchResultsPage } from "@/features/books/components/SearchResultsPage";
import { getBooks, searchBooks } from "@/features/books/services/books.service";
import type { Book } from "@/features/books/types/book.types";
import { BiblioBotChatWidget } from "@/features/home/components/BiblioBotChatWidget";
import { ChatProvider } from "@/features/home/components/ChatContext";
import { PageShell } from "@/features/home/components/PageShell";
import type { ChatbotPageContext } from "@/features/home/types/chat.types";

type SearchPageProps = {
  searchParams: Promise<{
    q?: string;
    sort?: string;
    category?: string;
    minPrice?: string;
    maxPrice?: string;
    minYear?: string;
    maxYear?: string;
  }>;
};

const validSorts = new Set([
  "relevance",
  "price_asc",
  "price_desc",
  "newest",
  "oldest",
]);

type SearchFilters = {
  category: string;
  minPrice: string;
  maxPrice: string;
  minYear: string;
  maxYear: string;
};

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

function parseNumberFilter(value: string): number | null {
  const normalized = value.replace(/[^\d]/g, "");
  if (!normalized) return null;

  const parsed = Number.parseInt(normalized, 10);
  return Number.isNaN(parsed) ? null : parsed;
}

function matchesChatContextFilters(book: Book, filters: SearchFilters) {
  const minPrice = parseNumberFilter(filters.minPrice);
  const maxPrice = parseNumberFilter(filters.maxPrice);
  const minYear = parseNumberFilter(filters.minYear);
  const maxYear = parseNumberFilter(filters.maxYear);

  if (filters.category && slugify(book.category) !== filters.category) {
    return false;
  }

  if (minPrice !== null && book.price < minPrice) return false;
  if (maxPrice !== null && book.price > maxPrice) return false;
  if (minYear !== null && (!book.publicationYear || book.publicationYear < minYear)) return false;
  if (maxYear !== null && (!book.publicationYear || book.publicationYear > maxYear)) return false;

  return true;
}

function toChatbotBookContext(book: Book) {
  return {
    id: book.id,
    title: book.title,
    authors: [book.author].filter(Boolean),
    categories: [book.category].filter(Boolean),
    price: book.price,
    available: book.stock > 0,
  };
}

function buildSearchChatPageContext(
  books: Book[],
  query: string,
  sort: string,
  filters: SearchFilters,
): ChatbotPageContext {
  const activeCategory = filters.category
    ? books.find((book) => slugify(book.category) === filters.category)?.category ?? filters.category
    : undefined;
  const activeFilters: Record<string, string> = {};

  if (activeCategory) activeFilters.category = activeCategory;
  if (filters.minPrice.trim()) activeFilters.minPrice = filters.minPrice.trim();
  if (filters.maxPrice.trim()) activeFilters.maxPrice = filters.maxPrice.trim();
  if (filters.minYear.trim()) activeFilters.minYear = filters.minYear.trim();
  if (filters.maxYear.trim()) activeFilters.maxYear = filters.maxYear.trim();
  if (sort !== "relevance") activeFilters.sort = sort;

  return {
    route: "/search",
    pageTitle: query.trim() ? `Busqueda: ${query.trim()}` : activeCategory ? `Categoria: ${activeCategory}` : "Catalogo",
    searchQuery: query.trim() || undefined,
    activeCategory,
    activeFilters,
    visibleBooks: books
      .filter((book) => matchesChatContextFilters(book, filters))
      .slice(0, 10)
      .map(toChatbotBookContext),
  };
}

export const metadata: Metadata = {
  title: "Buscar libros | Webook",
  description: "Busca libros por titulo, autor o categoria en Webook.",
};

export default async function SearchPage({ searchParams }: SearchPageProps) {
  const params = await searchParams;
  const query = params.q ?? "";
  let books: Book[] = [];
  let dataError: string | null = null;

  try {
    books = query.trim().length >= 2 ? await searchBooks(query) : await getBooks();
  } catch (error) {
    dataError = error instanceof Error ? error.message : "No se pudo conectar con la API.";
  }

  const sort = validSorts.has(params.sort ?? "")
    ? (params.sort as "relevance" | "price_asc" | "price_desc" | "newest" | "oldest")
    : "relevance";
  const filters = {
    category: params.category ?? "",
    minPrice: params.minPrice ?? "",
    maxPrice: params.maxPrice ?? "",
    minYear: params.minYear ?? "",
    maxYear: params.maxYear ?? "",
  };
  const chatPageContext = buildSearchChatPageContext(books, query, sort, filters);

  return (
    <ChatProvider>
      <PageShell>
        <Header />
        <SearchResultsPage
          books={books}
          query={query}
          sort={sort}
          filters={filters}
          error={dataError}
        />
        <BiblioBotChatWidget pageContext={chatPageContext} />
      </PageShell>
    </ChatProvider>
  );
}
