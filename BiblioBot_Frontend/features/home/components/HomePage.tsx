import { Header } from "@/components/layout/Header";
import { getFeaturedBooks } from "@/features/books/services/books.service";
import { getCategories } from "@/features/categories/services/categories.service";
import { BiblioBotChatWidget } from "./BiblioBotChatWidget";
import { ChatProvider } from "./ChatContext";
import { ParallaxBookExperience } from "./ParallaxBookExperience";
import { PageShell } from "./PageShell";

export async function HomePage() {
  const [featuredBooks, categories] = await Promise.all([
    getFeaturedBooks(),
    getCategories(),
  ]);

  return (
    <ChatProvider>
      <PageShell>
        <Header />
        <div className="pt-20">
          <ParallaxBookExperience books={featuredBooks} categories={categories} />
        </div>
        <BiblioBotChatWidget />
      </PageShell>
    </ChatProvider>
  );
}
