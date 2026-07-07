import type { Book } from "@/features/books/types/book.types";
import type { FavoriteBook } from "../types/favorite.types";

const FAVORITES_STORAGE_KEY = "bibliobot.favorites";

export const FAVORITES_UPDATED_EVENT = "bibliobot:favorites-updated";

function readFavoritesFromStorage(): FavoriteBook[] {
  if (typeof window === "undefined") return [];

  const rawFavorites = window.localStorage.getItem(FAVORITES_STORAGE_KEY);
  if (!rawFavorites) return [];

  try {
    const favorites = JSON.parse(rawFavorites) as FavoriteBook[];
    return Array.isArray(favorites) ? favorites : [];
  } catch {
    window.localStorage.removeItem(FAVORITES_STORAGE_KEY);
    return [];
  }
}

function writeFavoritesToStorage(favorites: FavoriteBook[]): void {
  if (typeof window === "undefined") return;

  window.localStorage.setItem(FAVORITES_STORAGE_KEY, JSON.stringify(favorites));
  window.dispatchEvent(
    new CustomEvent<FavoriteBook[]>(FAVORITES_UPDATED_EVENT, {
      detail: favorites,
    }),
  );
}

export function getFavorites(): FavoriteBook[] {
  return readFavoritesFromStorage();
}

export function getFavoriteCount(): number {
  return getFavorites().length;
}

export function isFavoriteBook(bookId: string): boolean {
  return getFavorites().some((favorite) => favorite.id === bookId);
}

export function addFavorite(book: Book): FavoriteBook[] {
  const favorites = getFavorites();
  const existingFavorite = favorites.find((favorite) => favorite.id === book.id);

  if (existingFavorite) {
    return favorites;
  }

  const nextFavorites = [
    {
      ...book,
      savedAt: new Date().toISOString(),
    },
    ...favorites,
  ];

  writeFavoritesToStorage(nextFavorites);
  return nextFavorites;
}

export function removeFavorite(bookId: string): FavoriteBook[] {
  const nextFavorites = getFavorites().filter((favorite) => favorite.id !== bookId);
  writeFavoritesToStorage(nextFavorites);
  return nextFavorites;
}

export function toggleFavorite(book: Book): FavoriteBook[] {
  return isFavoriteBook(book.id) ? removeFavorite(book.id) : addFavorite(book);
}

export function clearFavorites(): FavoriteBook[] {
  writeFavoritesToStorage([]);
  return [];
}
