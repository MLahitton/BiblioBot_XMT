import type { Book } from "../types/book.types";
import { BookCard } from "./BookCard";

type BookGridProps = {
  books: Book[];
};

export function BookGrid({ books }: BookGridProps) {
  return (
    <div className="grid gap-5 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
      {books.map((book, index) => (
        <BookCard key={book.id} book={book} revealDelay={index * 0.06} />
      ))}
    </div>
  );
}
