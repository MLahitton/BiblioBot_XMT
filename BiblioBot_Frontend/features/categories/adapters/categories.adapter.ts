import type {
  Category,
  CategoryApiResponse,
} from "../types/category.types";

export function mapCategoryApiToCategory(
  category: CategoryApiResponse,
): Category {
  return {
    id: category.id,
    name: category.name,
    description: category.description,
    icon: category.icon_url,
    slug: category.slug,
    totalBooks: category.total_books,
  };
}

export function mapCategoriesApiToCategories(
  categories: CategoryApiResponse[],
): Category[] {
  return categories.map(mapCategoryApiToCategory);
}
