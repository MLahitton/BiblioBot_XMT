export const API_ENDPOINTS = {
  books: "/books",
  bookBySlug: (slug: string) => `/books/${slug}`,
  categories: "/categories",
} as const;
