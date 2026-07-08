import { Header } from "@/components/layout/Header";
import { getFeaturedBooks } from "@/features/books/services/books.service";
import type { Book } from "@/features/books/types/book.types";
import {
  getCategories,
  getCategoriesWithVisibleBooks,
} from "@/features/categories/services/categories.service";
import type { Category } from "@/features/categories/types/category.types";
import { BiblioBotChatWidget } from "./BiblioBotChatWidget";
import { ChatProvider } from "./ChatContext";
import { ParallaxBookExperience } from "./ParallaxBookExperience";
import { PageShell } from "./PageShell";

export async function HomePage() {
  let dataError: string | null = null;
  let featuredBooks: Book[] = [];
  let categories: Category[] = [];

  try {
    [featuredBooks, categories] = await Promise.all([
      getFeaturedBooks(),
      getCategories(),
    ]);
  } catch (error) {
    dataError = error instanceof Error ? error.message : "No se pudo conectar con la API.";
  }

  return (
    <ChatProvider>
      <PageShell>
        <Header />
        <div className="pt-36 md:pt-20">
          <ParallaxBookExperience
            books={featuredBooks}
            categories={getCategoriesWithVisibleBooks(categories, featuredBooks)}
            dataError={dataError}
          />
        </div>
        <BiblioBotChatWidget />
      </PageShell>
    </ChatProvider>
  );
}
