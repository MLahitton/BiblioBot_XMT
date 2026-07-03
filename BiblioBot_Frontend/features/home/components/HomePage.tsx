import { Header } from "@/components/layout/Header";
import { getFeaturedBooks } from "@/features/books/services/books.service";
import { getCategories } from "@/features/categories/services/categories.service";
import { ParallaxBookExperience } from "./ParallaxBookExperience";

export async function HomePage() {
  const [featuredBooks, categories] = await Promise.all([
    getFeaturedBooks(),
    getCategories(),
  ]);

  return (
    <div className="page-shell min-h-screen w-full bg-background text-foreground">
      <Header />
      <div className="pt-20">
        <ParallaxBookExperience books={featuredBooks} categories={categories} />
      </div>
    </div>
  );
}
