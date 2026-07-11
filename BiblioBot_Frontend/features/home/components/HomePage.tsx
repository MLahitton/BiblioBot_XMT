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
import type { ChatbotPageContext } from "../types/chat.types";

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

  const chatPageContext: ChatbotPageContext = {
    route: "/",
    pageTitle: "Inicio",
    visibleBooks: featuredBooks.slice(0, 10).map(toChatbotBookContext),
  };

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
        <BiblioBotChatWidget pageContext={chatPageContext} />
      </PageShell>
    </ChatProvider>
  );
}
