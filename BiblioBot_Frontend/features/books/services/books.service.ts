import { booksMock } from "../data/books.mock";
import type { Book } from "../types/book.types";

export async function getBooks(): Promise<Book[]> {
  return [...booksMock];
}

export async function getFeaturedBooks(): Promise<Book[]> {
  return booksMock.filter((book) => book.badge !== undefined || book.rating >= 4.8);
}

export async function getBookBySlug(slug: string): Promise<Book | undefined> {
  return booksMock.find((book) => book.slug === slug);
}

export async function getBooksByCategory(categorySlug: string): Promise<Book[]> {
  const normalizedCategory = categorySlug.toLowerCase();

  return booksMock.filter(
    (book) =>
      book.category.toLowerCase().replaceAll(" ", "-") === normalizedCategory,
  );
}
