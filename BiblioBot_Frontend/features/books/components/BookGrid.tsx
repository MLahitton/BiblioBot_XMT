"use client";

import { motion, useReducedMotion } from "framer-motion";
import Image from "next/image";
import Link from "next/link";
import type { Book } from "../types/book.types";
import { BookPrice } from "./BookPrice";

type BookGridProps = {
  books: Book[];
};

const shelfTitles = [
  "Leyendo ahora",
  "Siguiente en la repisa",
  "Favoritos terminados",
];

function splitIntoShelves(books: Book[]): Book[][] {
  if (books.length <= 5) return [books];
  const mid = Math.ceil(books.length / 2);
  return [books.slice(0, mid), books.slice(mid)];
}

function getShelfTitle(shelfIndex: number, shelfCount: number): string {
  if (shelfCount === 1) return "Seleccion recomendada";
  return shelfTitles[shelfIndex] ?? `Repisa ${shelfIndex + 1}`;
}

function BookCover({
  book,
  revealDelay,
}: {
  book: Book;
  revealDelay: number;
}) {
  const shouldReduceMotion = useReducedMotion();

  return (
    <Link
      href={`/books/${book.slug}`}
      aria-label={`Ver detalles de ${book.title}`}
      className="group relative block w-[108px] shrink-0 cursor-pointer rounded-[9px] outline-none focus-visible:ring-2 focus-visible:ring-accent sm:w-[124px]"
    >
      <motion.div
        className="relative"
        initial={shouldReduceMotion ? { opacity: 1 } : { opacity: 0, y: 16 }}
        whileInView={shouldReduceMotion ? { opacity: 1 } : { opacity: 1, y: 0 }}
        transition={{ delay: revealDelay, duration: 0.42, ease: "easeOut" }}
        viewport={{ once: true, amount: 0.2 }}
      >
        <div className="absolute -bottom-1 left-1/2 h-3 w-20 -translate-x-1/2 rounded-full bg-black/20 blur-md transition-all duration-500 group-hover:w-16 group-hover:blur-lg group-hover:opacity-70" />

        <div className="relative h-[160px] w-full overflow-hidden rounded-[7px] border border-white/50 bg-card shadow-[5px_12px_24px_rgba(53,30,28,0.24),inset_5px_0_0_rgba(0,0,0,0.18)] transition-all duration-500 group-hover:-translate-y-4 group-hover:rotate-[-1.5deg] group-hover:shadow-[8px_20px_38px_rgba(53,30,28,0.32),inset_5px_0_0_rgba(0,0,0,0.15)] sm:h-[184px]">
          <Image
            src={book.image}
            alt={`Portada de ${book.title}`}
            fill
            className="object-cover transition-transform duration-700 group-hover:scale-[1.03]"
            sizes="(max-width: 640px) 108px, 124px"
          />
          <span className="absolute inset-y-0 left-0 w-4 bg-gradient-to-r from-black/38 via-black/12 to-transparent" />
          <span className="absolute inset-y-0 right-0 w-2 bg-gradient-to-l from-white/45 to-transparent" />
          <span className="absolute inset-x-3 top-2 h-px bg-white/38" />
          <span className="absolute inset-0 rounded-[7px] opacity-0 ring-2 ring-inset ring-white/25 transition duration-500 group-hover:opacity-100" />

          {book.badge && (
            <span className="absolute bottom-2.5 left-1/2 max-w-[82%] -translate-x-1/2 whitespace-nowrap rounded-full border border-white/30 bg-paper/92 px-2 py-1 text-center text-[0.52rem] font-black uppercase tracking-[0.08em] text-foreground shadow-sm">
              {book.badge}
            </span>
          )}
        </div>
      </motion.div>
    </Link>
  );
}

function BookInfo({ book }: { book: Book }) {
  return (
    <div className="flex w-[108px] shrink-0 flex-col items-center text-center sm:w-[124px]">
      <span className="inline-flex rounded-full border border-border bg-paper px-2 py-0.5 text-[0.58rem] font-extrabold text-coffee shadow-sm">
        {book.category}
      </span>
      <h3 className="mt-1 line-clamp-2 w-full text-[0.73rem] font-black leading-tight text-foreground">
        {book.title}
      </h3>
      <p className="mt-0.5 w-full truncate text-[0.64rem] font-semibold text-muted">
        {book.author}
      </p>
      <div className="mt-1 flex w-full items-center justify-center gap-0.5">
        {[1, 2, 3, 4, 5].map((s) => (
          <svg
            key={s}
            className={`h-2.5 w-2.5 ${book.rating >= s ? "text-[#d09a3f]" : "text-border"}`}
            viewBox="0 0 20 20"
            fill="currentColor"
            aria-hidden="true"
          >
            <path d="M9.049 2.927c.3-.921 1.603-.921 1.902 0l1.07 3.292a1 1 0 00.95.69h3.462c.969 0 1.371 1.24.588 1.81l-2.8 2.034a1 1 0 00-.364 1.118l1.07 3.292c.3.921-.755 1.688-1.54 1.118l-2.8-2.034a1 1 0 00-1.175 0l-2.8 2.034c-.784.57-1.838-.197-1.539-1.118l1.07-3.292a1 1 0 00-.364-1.118L2.98 8.72c-.783-.57-.38-1.81.588-1.81h3.461a1 1 0 00.951-.69l1.07-3.292z" />
          </svg>
        ))}
        <span className="ml-1 text-[0.58rem] font-bold text-muted">
          {book.rating.toFixed(1)}
        </span>
      </div>
      <div className="mt-1.5 flex w-full justify-center">
        <BookPrice price={book.price} previousPrice={book.previousPrice} />
      </div>
    </div>
  );
}

function ShelfBoard() {
  return (
    <div className="relative mx-0 mt-0">
      <div className="h-px w-full bg-white/70" />
      <div
        className="w-full"
        style={{
          height: "18px",
          background:
            "linear-gradient(180deg, #ecdbb8 0%, #d4bc8a 30%, #bfa06a 68%, #b08e56 100%)",
          boxShadow:
            "0 5px 14px rgba(53,30,28,0.20), inset 0 1px 0 rgba(255,255,255,0.50), inset 0 -1px 0 rgba(0,0,0,0.10)",
        }}
      >
        <span
          className="absolute inset-x-0 top-[5px] h-px opacity-[0.18]"
          style={{
            background:
              "repeating-linear-gradient(90deg,transparent 0px,transparent 20px,rgba(53,30,28,0.8) 20px,rgba(53,30,28,0.8) 21px)",
          }}
        />
        <span
          className="absolute inset-x-0 top-[11px] h-px opacity-[0.10]"
          style={{
            background:
              "repeating-linear-gradient(90deg,transparent 0px,transparent 34px,rgba(53,30,28,0.6) 34px,rgba(53,30,28,0.6) 35px)",
          }}
        />
      </div>
      <div
        style={{
          height: "18px",
          background:
            "linear-gradient(180deg,rgba(60,30,10,0.20) 0%,rgba(53,30,28,0.06) 70%,transparent 100%)",
        }}
      />
      <div
        className="blur-md"
        style={{
          height: "14px",
          background:
            "radial-gradient(ellipse at center, rgba(53,30,28,0.18) 0%, transparent 70%)",
        }}
      />
    </div>
  );
}

export function BookGrid({ books }: BookGridProps) {
  const shelves = splitIntoShelves(books);

  return (
    <div className="space-y-10">
      {shelves.map((shelfBooks, shelfIndex) => (
        <section
          key={shelfBooks.map((b) => b.id).join("-")}
          aria-label={getShelfTitle(shelfIndex, shelves.length)}
        >
          <div className="mb-4 flex items-center justify-between gap-4 px-1">
            <div className="flex items-center gap-2">
              <span className="inline-block h-2.5 w-2.5 rounded-sm bg-gradient-to-br from-[#d8c9ac] to-[#b8996e] shadow-sm" />
              <p className="text-xs font-black uppercase tracking-widest text-muted">
                {getShelfTitle(shelfIndex, shelves.length)}
              </p>
            </div>
            <span className="hidden text-[0.68rem] font-bold text-coffee/60 sm:inline">
              {shelfBooks.length} libros
            </span>
          </div>

          <div className="-mx-3 overflow-x-auto px-3 [scrollbar-color:rgba(53,30,28,0.18)_transparent] [scrollbar-width:thin]">
            <div className="min-w-max pr-4">
              <div className="flex items-end gap-5 px-1 pt-3 sm:gap-7">
                {shelfBooks.map((book, i) => (
                  <BookCover
                    key={book.id}
                    book={book}
                    revealDelay={shelfIndex * 0.06 + i * 0.06}
                  />
                ))}
              </div>

              <ShelfBoard />

              <div className="flex gap-5 px-1 pb-2 pt-4 sm:gap-7">
                {shelfBooks.map((book) => (
                  <BookInfo key={book.id} book={book} />
                ))}
              </div>
            </div>
          </div>
        </section>
      ))}
    </div>
  );
}
