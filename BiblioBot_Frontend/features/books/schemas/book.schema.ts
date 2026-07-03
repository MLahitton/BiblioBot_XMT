import type { Book, BookApiResponse } from "../types/book.types";

export const bookRequiredFields = [
  "id",
  "title",
  "author",
  "category",
  "price",
  "rating",
  "image",
  "description",
  "stock",
  "slug",
] satisfies Array<keyof Book>;

export const bookApiRequiredFields = [
  "id",
  "title",
  "author",
  "category",
  "price",
  "rating",
  "image_url",
  "description",
  "stock",
  "slug",
] satisfies Array<keyof BookApiResponse>;
