import type {
  Category,
  CategoryApiResponse,
} from "../types/category.types";

export function mapCategoryApiToCategory(
  category: CategoryApiResponse,
): Category {
  const slug = category.name
    .toLowerCase()
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "")
    .replace(/[^a-z0-9]+/g, "-")
    .replace(/^-+|-+$/g, "");

  return {
    id: category.id,
    name: category.name,
    description: `Libros de ${category.name}`,
    icon: "/icons/category.svg",
    slug: slug || category.id,
    totalBooks: 0,
  };
}

export function mapCategoriesApiToCategories(
  categories: CategoryApiResponse[],
): Category[] {
  return categories.map(mapCategoryApiToCategory);
}
