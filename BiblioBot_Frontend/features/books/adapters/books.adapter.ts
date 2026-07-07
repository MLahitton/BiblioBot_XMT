import type { Book, BookApiResponse } from "../types/book.types";

const fallbackCover = "/images/books/book-01.svg";

export function slugifyBookTitle(value: string): string {
  return value
    .toLowerCase()
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "")
    .replace(/[^a-z0-9]+/g, "-")
    .replace(/^-+|-+$/g, "")
    .slice(0, 80);
}

export function createBookSlug(book: Pick<BookApiResponse, "id" | "title">): string {
  const titleSlug = slugifyBookTitle(book.title) || "libro";
  return `${titleSlug}-${book.id}`;
}

export function extractBookIdFromSlug(slug: string): string | null {
  const match = slug.match(/[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i);
  return match?.[0] ?? null;
}

export function mapBookApiToBook(book: BookApiResponse): Book {
  const author = book.authors.length > 0 ? book.authors.join(", ") : "Autor no registrado";
  const category = book.categories.length > 0 ? book.categories[0] : "Sin categoria";

  return {
    id: book.id,
    title: book.title,
    author,
    category,
    price: book.price,
    rating: 0,
    image: book.imageUrl?.trim() || fallbackCover,
    badge: book.totalStock > 0 ? "Disponible" : undefined,
    description:
      book.description?.trim() ||
      "Este libro forma parte del catalogo activo de BiblioBot.",
    stock: book.totalStock,
    slug: createBookSlug(book),
    isbn: book.isbn,
    publisher: book.publisherName,
  };
}

export function mapBooksApiToBooks(books: BookApiResponse[]): Book[] {
  return books.map(mapBookApiToBook);
}
