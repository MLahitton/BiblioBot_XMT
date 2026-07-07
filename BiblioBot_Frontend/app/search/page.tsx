import type { Metadata } from "next";
import { Header } from "@/components/layout/Header";
import { SearchResultsPage } from "@/features/books/components/SearchResultsPage";
import { getBooks } from "@/features/books/services/books.service";
import { BiblioBotChatWidget } from "@/features/home/components/BiblioBotChatWidget";
import { ChatProvider } from "@/features/home/components/ChatContext";
import { PageShell } from "@/features/home/components/PageShell";

type SearchPageProps = {
  searchParams: Promise<{
    q?: string;
    sort?: string;
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
  const books = await getBooks();
  const query = params.q ?? "";
  const sort = validSorts.has(params.sort ?? "")
    ? (params.sort as "relevance" | "price_asc" | "price_desc" | "newest" | "oldest")
    : "relevance";

  return (
    <ChatProvider>
      <PageShell>
        <Header />
        <SearchResultsPage books={books} query={query} sort={sort} />
        <BiblioBotChatWidget />
      </PageShell>
    </ChatProvider>
  );
}
