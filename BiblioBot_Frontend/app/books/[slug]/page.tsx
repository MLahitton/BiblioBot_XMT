import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { Header } from "@/components/layout/Header";
import { BookDetailPage } from "@/features/books/components/BookDetailPage";
import {
  getBookBySlug,
  getBooks,
} from "@/features/books/services/books.service";
import { BiblioBotChatWidget } from "@/features/home/components/BiblioBotChatWidget";
import { ChatProvider } from "@/features/home/components/ChatContext";
import { PageShell } from "@/features/home/components/PageShell";

type BookPageProps = {
  params: Promise<{ slug: string }>;
};

export async function generateStaticParams() {
  const books = await getBooks();
  return books.map((book) => ({ slug: book.slug }));
}

export async function generateMetadata({
  params,
}: BookPageProps): Promise<Metadata> {
  const { slug } = await params;
  const book = await getBookBySlug(slug);

  if (!book) {
    return {
      title: "Libro no encontrado | Webook",
    };
  }

  return {
    title: `${book.title} | Webook`,
    description: book.description,
  };
}

export default async function BookPage({ params }: BookPageProps) {
  const { slug } = await params;
  const [book, books] = await Promise.all([getBookBySlug(slug), getBooks()]);

  if (!book) {
    notFound();
  }

  const relatedBooks = books
    .filter(
      (candidate) =>
        candidate.id !== book.id && candidate.category === book.category,
    )
    .slice(0, 3);

  return (
    <ChatProvider>
      <PageShell>
        <Header />
        <BookDetailPage book={book} relatedBooks={relatedBooks} />
        <BiblioBotChatWidget />
      </PageShell>
    </ChatProvider>
  );
}
