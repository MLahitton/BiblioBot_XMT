import Image from "next/image";
import { BookCard } from "@/features/books/components/BookCard";
import type { Book } from "@/features/books/types/book.types";
import { landingCopy } from "../data/landing-copy.data";

type BenefitsSectionProps = {
  books: Book[];
};

export function BenefitsSection({ books }: BenefitsSectionProps) {
  return (
    <section id="beneficios" className="px-6 py-16 lg:px-10">
      <div className="mb-6 flex items-center justify-between gap-4">
        <div className="flex items-center gap-4">
          <Image 
            src="/images/biblioBot/cutouts/pose2_Webook-cutout.png" 
            alt="BiblioBot" 
            width={698} 
            height={908} 
            className="h-24 w-auto object-contain drop-shadow-[0_12px_18px_rgba(53,30,28,0.16)]"
          />
          <h2 className="text-3xl font-black text-foreground sm:text-4xl">
            {landingCopy.benefits.title}
          </h2>
        </div>
        <div className="hidden items-center gap-2 sm:flex">
          <button
            type="button"
            className="flex h-9 w-9 items-center justify-center rounded-full border border-border bg-paper text-lg font-bold transition hover:bg-card focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent"
            aria-label="Anterior"
          >
            &larr;
          </button>
          <button
            type="button"
            className="flex h-9 w-9 items-center justify-center rounded-full border border-border bg-paper text-lg font-bold transition hover:bg-card focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent"
            aria-label="Siguiente"
          >
            &rarr;
          </button>
        </div>
      </div>
      <div className="grid gap-6 sm:grid-cols-2 lg:grid-cols-4">
        {books.map((book, index) => (
          <BookCard key={book.id} book={book} revealDelay={index * 0.05} />
        ))}
      </div>
    </section>
  );
}
