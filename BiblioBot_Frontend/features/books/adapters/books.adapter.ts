import type { Book, BookApiResponse } from "../types/book.types";

export function mapBookApiToBook(book: BookApiResponse): Book {
  return {
    id: book.id,
    title: book.title,
    author: book.author,
    category: book.category,
    price: book.price,
    previousPrice: book.previous_price,
    rating: book.rating,
    image: book.image_url,
    badge: book.badge,
    description: book.description,
    stock: book.stock,
    slug: book.slug,
  };
}

export function mapBooksApiToBooks(books: BookApiResponse[]): Book[] {
  return books.map(mapBookApiToBook);
}
