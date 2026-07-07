import type { Book } from "@/features/books/types/book.types";

export type FavoriteBook = Book & {
  savedAt: string;
};
