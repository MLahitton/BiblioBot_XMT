import { getStoredSession } from "@/features/auth/services/auth-storage";
import { apiGet, apiPost } from "@/lib/api/api-client";
import { API_ENDPOINTS } from "@/lib/api/endpoints";

export type BookReview = {
  id: string;
  bookId: string;
  userId: string;
  userFullName: string;
  rating: number;
  comment: string;
  isVerifiedPurchase: boolean;
  createdAt: string;
  updatedAt?: string | null;
};

export type BookReviewsSummary = {
  bookId: string;
  averageRating: number;
  reviewCount: number;
  items: BookReview[];
};

export class BookReviewAuthError extends Error {
  constructor() {
    super("Debes iniciar sesion para escribir una resena.");
    this.name = "BookReviewAuthError";
  }
}

export async function getBookReviews(bookId: string): Promise<BookReviewsSummary> {
  return apiGet<BookReviewsSummary>(API_ENDPOINTS.bookReviews(bookId));
}

export async function saveBookReview(
  bookId: string,
  payload: { rating: number; comment: string },
): Promise<BookReviewsSummary> {
  const token = getStoredSession()?.accessToken;

  if (!token) {
    throw new BookReviewAuthError();
  }

  return apiPost<BookReviewsSummary, { rating: number; comment: string }>(
    API_ENDPOINTS.bookReviews(bookId),
    payload,
    { token },
  );
}
