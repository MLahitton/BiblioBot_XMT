import Image from "next/image";
import type { Category } from "@/features/categories/types/category.types";
import { CategoryCard } from "@/features/categories/components/CategoryCard";
import { landingCopy } from "../data/landing-copy.data";

type CategorySectionProps = {
  categories: Category[];
  totalBooks: number;
};

export function CategorySection({ categories, totalBooks }: CategorySectionProps) {
  return (
    <aside id="categorias" className="lg:sticky lg:top-24 lg:h-fit">
      <h2 className="text-lg font-extrabold text-foreground">
        {landingCopy.categories.title}
      </h2>
      <nav className="mt-4 space-y-1" aria-label="Categorías">
        <a
          href="#destacados"
          className="flex items-center gap-3 rounded-xl bg-[#FF6037]/14 px-3 py-2.5 text-sm font-extrabold text-foreground shadow-[0_8px_18px_rgba(255,96,55,0.12)] transition hover:bg-[#FF6037]/22"
        >
          <span className="flex h-6 w-6 items-center justify-center rounded-md border border-[#FF6037]/20 bg-accent">
            <Image
              src="/icons/category.svg"
              alt=""
              width={14}
              height={14}
              className="invert"
            />
          </span>
          <span className="flex-1">Todos los libros</span>
          <span className="rounded-md bg-accent px-1.5 py-0.5 text-[0.62rem] font-black text-paper">
            {totalBooks}
          </span>
        </a>
        {categories.length > 0 ? (
          categories.slice(0, 6).map((category, index) => (
            <CategoryCard
              key={category.id}
              category={category}
              revealDelay={index * 0.04}
            />
          ))
        ) : (
          <p className="px-3 py-2 text-xs font-bold text-muted">
            No hay categorias disponibles.
          </p>
        )}
      </nav>
    </aside>
  );
}
