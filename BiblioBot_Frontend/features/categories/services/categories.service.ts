import { categoriesMock } from "../data/categories.mock";
import type { Category } from "../types/category.types";

export async function getCategories(): Promise<Category[]> {
  return categoriesMock;
}

export async function getCategoryBySlug(
  slug: string,
): Promise<Category | undefined> {
  return categoriesMock.find((category) => category.slug === slug);
}
