import { apiGet } from "@/lib/api/api-client";
import { API_ENDPOINTS } from "@/lib/api/endpoints";
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
