import { apiGet } from "@/lib/api/api-client";
import { API_ENDPOINTS } from "@/lib/api/endpoints";
import {
  extractBookIdFromSlug,
  mapBookApiToBook,
  mapBooksApiToBooks,
} from "../adapters/books.adapter";
import type { Book } from "../types/book.types";
import type { BookApiResponse, PagedBooksApiResponse } from "../types/book.types";

function getCommercialScore(book: Book): number {
  return (book.purchasedCount ?? 0) * 4 + (book.favoriteCount ?? 0) * 2;
}

export async function getBooks(): Promise<Book[]> {
  const response = await apiGet<PagedBooksApiResponse>(API_ENDPOINTS.books, {
    query: {
      pageNumber: 1,
      pageSize: 100,
    },
  });

  return mapBooksApiToBooks(response.items);
}

export async function getFeaturedBooks(): Promise<Book[]> {
  const books = await getBooks();
  return books
    .filter((book) => book.stock > 0)
    .sort((current, next) =>
      getCommercialScore(next) - getCommercialScore(current) ||
      next.rating - current.rating ||
      next.reviewCount - current.reviewCount ||
      current.title.localeCompare(next.title),
    )
    .slice(0, 12);
}

export async function getBookBySlug(slug: string): Promise<Book | undefined> {
  const id = extractBookIdFromSlug(slug);
  if (!id) return undefined;

  const response = await apiGet<BookApiResponse>(API_ENDPOINTS.bookById(id));
  return mapBookApiToBook(response);
}

export async function getBooksByCategory(categorySlug: string): Promise<Book[]> {
  const books = await getBooks();
  const normalizedCategory = categorySlug.toLowerCase();

  return books.filter(
    (book) =>
      book.category.toLowerCase().replaceAll(" ", "-") === normalizedCategory,
  );
}

export async function searchBooks(query: string): Promise<Book[]> {
  const normalizedQuery = query.trim();

  if (normalizedQuery.length < 2) {
    return getBooks();
  }

  const response = await apiGet<PagedBooksApiResponse>(API_ENDPOINTS.bookSearch, {
    query: {
      q: normalizedQuery,
      pageNumber: 1,
      pageSize: 100,
    },
  });

  return mapBooksApiToBooks(response.items);
}
