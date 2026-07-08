import { apiGet } from "@/lib/api/api-client";
import { API_ENDPOINTS } from "@/lib/api/endpoints";
import type { Book } from "@/features/books/types/book.types";
import { mapCategoriesApiToCategories } from "../adapters/categories.adapter";
import type { Category } from "../types/category.types";
import type { CategoryApiResponse } from "../types/category.types";

export async function getCategories(): Promise<Category[]> {
  const response = await apiGet<CategoryApiResponse[]>(API_ENDPOINTS.categories);
  return mapCategoriesApiToCategories(response);
}

export async function getCategoryBySlug(
  slug: string,
): Promise<Category | undefined> {
  const categories = await getCategories();
  return categories.find((category) => category.slug === slug);
}

export function getCategoriesWithVisibleBooks(
  categories: Category[],
  books: Book[],
): Category[] {
  const bookCountsByCategory = new Map<string, number>();

  books.forEach((book) => {
    const categoryName = book.category.trim().toLowerCase();
    if (!categoryName || categoryName === "sin categoria") return;

    bookCountsByCategory.set(
      categoryName,
      (bookCountsByCategory.get(categoryName) ?? 0) + 1,
    );
  });

  return categories
    .map((category) => ({
      ...category,
      totalBooks: bookCountsByCategory.get(category.name.trim().toLowerCase()) ?? category.totalBooks,
    }))
    .filter((category) => category.totalBooks > 0)
    .sort((current, next) => next.totalBooks - current.totalBooks || current.name.localeCompare(next.name));
}
