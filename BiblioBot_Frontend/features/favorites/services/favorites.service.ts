import type { Book } from "@/features/books/types/book.types";
import { createBookSlug } from "@/features/books/adapters/books.adapter";
import { getStoredSession } from "@/features/auth/services/auth-storage";
import { API_ENDPOINTS } from "@/lib/api/endpoints";
import { apiDelete, apiGet, apiPost } from "@/lib/api/api-client";
import type { FavoriteBook } from "../types/favorite.types";

type FavoriteBookApiResponse = {
  bookId: string;
  title: string;
  author?: string | null;
  category?: string | null;
  description?: string | null;
  coverUrl?: string | null;
  price: number;
  totalStock: number;
  addedAtUtc: string;
};

type FavoriteBookStatusApiResponse = {
  bookId: string;
  isFavorite: boolean;
};

export const FAVORITES_UPDATED_EVENT = "bibliobot:favorites-updated";

export class FavoritesAuthError extends Error {
  constructor() {
    super("Debes iniciar sesion para guardar favoritos.");
    this.name = "FavoritesAuthError";
  }
}

function getAccessToken(): string {
  const token = getStoredSession()?.accessToken;

  if (!token) {
    throw new FavoritesAuthError();
  }

  return token;
}

function notifyFavoritesUpdated(favorites: FavoriteBook[]): void {
  if (typeof window === "undefined") return;

  window.dispatchEvent(
    new CustomEvent<FavoriteBook[]>(FAVORITES_UPDATED_EVENT, {
      detail: favorites,
    }),
  );
}

function mapFavoriteApiToBook(favorite: FavoriteBookApiResponse): FavoriteBook {
  const title = favorite.title.trim() || "Libro sin titulo";
  const author = favorite.author?.trim() || "Autor no registrado";
  const category = favorite.category?.trim() || "Sin categoria";

  return {
    id: favorite.bookId,
    title,
    author,
    category,
    price: Number(favorite.price),
    rating: 0,
    reviewCount: 0,
    image: favorite.coverUrl?.trim() || "/images/books/book-01.svg",
    badge: undefined,
    description:
      favorite.description?.trim() ||
      "Este libro forma parte de tus favoritos guardados en Webook.",
    stock: favorite.totalStock,
    slug: createBookSlug({ id: favorite.bookId, title }),
    savedAt: favorite.addedAtUtc,
  };
}

export async function getFavorites(): Promise<FavoriteBook[]> {
  const token = getAccessToken();
  const favorites = await apiGet<FavoriteBookApiResponse[]>(API_ENDPOINTS.favorites.list, {
    token,
  });
  const mappedFavorites = favorites.map(mapFavoriteApiToBook);

  notifyFavoritesUpdated(mappedFavorites);
  return mappedFavorites;
}

export async function getFavoriteCount(): Promise<number> {
  return (await getFavorites()).length;
}

export async function isFavoriteBook(bookId: string): Promise<boolean> {
  const token = getAccessToken();
  const status = await apiGet<FavoriteBookStatusApiResponse>(
    API_ENDPOINTS.favorites.status(bookId),
    { token },
  );

  return status.isFavorite;
}

export async function addFavorite(book: Book): Promise<FavoriteBook[]> {
  const token = getAccessToken();
  await apiPost<FavoriteBookApiResponse, undefined>(
    API_ENDPOINTS.favorites.item(book.id),
    undefined,
    { token },
  );

  return getFavorites();
}

export async function removeFavorite(bookId: string): Promise<FavoriteBook[]> {
  const token = getAccessToken();
  await apiDelete<void>(API_ENDPOINTS.favorites.item(bookId), { token });

  return getFavorites();
}

export async function toggleFavorite(book: Book): Promise<FavoriteBook[]> {
  return (await isFavoriteBook(book.id))
    ? removeFavorite(book.id)
    : addFavorite(book);
}

export async function clearFavorites(): Promise<FavoriteBook[]> {
  const token = getAccessToken();
  const favorites = await getFavorites();
  await Promise.all(
    favorites.map((favorite) =>
      apiDelete<void>(API_ENDPOINTS.favorites.item(favorite.id), { token }),
    ),
  );

  notifyFavoritesUpdated([]);
  return [];
}
