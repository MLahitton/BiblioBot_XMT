import type { Category } from "@/features/categories/types/category.types";
import { CategoryCard } from "@/features/categories/components/CategoryCard";
import { landingCopy } from "../data/landing-copy.data";
import { ScrollReveal } from "./ScrollReveal";

type CategorySectionProps = {
  categories: Category[];
};

export function CategorySection({ categories }: CategorySectionProps) {
  return (
    <section id="categorias" className="px-6 py-[72px]">
      <div className="mx-auto max-w-6xl">
        <ScrollReveal className="max-w-2xl">
          <p className="text-sm font-medium uppercase tracking-[0.2em] text-accent">
            {landingCopy.categories.eyebrow}
          </p>
          <h2 className="mt-3 text-3xl font-semibold leading-tight text-foreground sm:text-4xl">
            {landingCopy.categories.title}
          </h2>
          <p className="mt-3 text-muted">
            {landingCopy.categories.description}
          </p>
        </ScrollReveal>
        <div className="mt-8 grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
          {categories.map((category, index) => (
            <CategoryCard
              key={category.id}
              category={category}
              revealDelay={index * 0.08}
            />
          ))}
        </div>
      </div>
    </section>
  );
}
