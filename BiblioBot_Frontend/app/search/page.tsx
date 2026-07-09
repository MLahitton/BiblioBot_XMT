import type { Metadata } from "next";
import { Header } from "@/components/layout/Header";
import { SearchResultsPage } from "@/features/books/components/SearchResultsPage";
import { getBooks, searchBooks } from "@/features/books/services/books.service";
import type { Book } from "@/features/books/types/book.types";
import { BiblioBotChatWidget } from "@/features/home/components/BiblioBotChatWidget";
import { ChatProvider } from "@/features/home/components/ChatContext";
import { PageShell } from "@/features/home/components/PageShell";

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

  return (
    <ChatProvider>
      <PageShell>
        <Header />
        <SearchResultsPage
          books={books}
          query={query}
          sort={sort}
          filters={{
            category: params.category ?? "",
            minPrice: params.minPrice ?? "",
            maxPrice: params.maxPrice ?? "",
            minYear: params.minYear ?? "",
            maxYear: params.maxYear ?? "",
          }}
          error={dataError}
        />
        <BiblioBotChatWidget />
      </PageShell>
    </ChatProvider>
  );
}
