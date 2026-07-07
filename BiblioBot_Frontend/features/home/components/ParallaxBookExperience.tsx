import type { Book } from "@/features/books/types/book.types";
import type { Category } from "@/features/categories/types/category.types";
import { BenefitsSection } from "./BenefitsSection";
import { CategorySection } from "./CategorySection";
import { FeaturedBooksSection } from "./FeaturedBooksSection";
import { FinalCtaSection } from "./FinalCtaSection";
import { HeroSection } from "./HeroSection";

type ParallaxBookExperienceProps = {
  books: Book[];
  categories: Category[];
  dataError?: string | null;
};

export function ParallaxBookExperience({
  books,
  categories,
  dataError,
}: ParallaxBookExperienceProps) {
  return (
    <main>
      <HeroSection />
      <div className="relative z-10 w-full bg-background pb-8">
        <div className="grid gap-8 px-6 py-8 lg:grid-cols-[190px_minmax(0,1fr)] lg:px-10">
          <CategorySection categories={categories} totalBooks={books.length} />
          <FeaturedBooksSection books={books} error={dataError} />
        </div>
        <BenefitsSection books={books.slice(0, 4)} />
        <FinalCtaSection />
      </div>
    </main>
  );
}
