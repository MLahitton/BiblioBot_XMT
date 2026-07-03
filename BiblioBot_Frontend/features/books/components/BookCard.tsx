"use client";

import { motion, useReducedMotion } from "framer-motion";
import Image from "next/image";
import type { Book } from "../types/book.types";
import { BookPrice } from "./BookPrice";

type BookCardProps = {
  book: Book;
  revealDelay?: number;
};

export function BookCard({ book, revealDelay = 0 }: BookCardProps) {
  const shouldReduceMotion = useReducedMotion();

  return (
    <motion.article
      className="group"
      initial={shouldReduceMotion ? { opacity: 1 } : { opacity: 0, y: 18 }}
      whileInView={shouldReduceMotion ? { opacity: 1 } : { opacity: 1, y: 0 }}
      transition={{ delay: revealDelay, duration: 0.42, ease: "easeOut" }}
      viewport={{ once: true, amount: 0.24 }}
    >
      <div className="relative aspect-[1.08] overflow-hidden rounded-xl bg-card shadow-[inset_0_1px_0_rgba(255,255,255,0.48),0_12px_28px_var(--shadow-soft)]">
        <Image
          src={book.image}
          alt={`Imagen realista de ${book.title}`}
          fill
          className="object-cover transition duration-500 group-hover:scale-[1.035]"
          sizes="(max-width: 768px) 80vw, (max-width: 1180px) 28vw, 280px"
        />
        <span className="absolute right-3 top-3 rounded-full border border-border bg-paper px-3 py-1 text-[0.68rem] font-extrabold text-foreground shadow-sm">
          {book.category}
        </span>
      </div>
      <div className="mt-3">
        <div className="flex items-start justify-between gap-3">
          <div className="min-w-0">
            <h3 className="truncate text-base font-extrabold leading-tight text-foreground">
              {book.title}
            </h3>
            <p className="mt-1 text-xs font-semibold text-muted">
              {book.author}
            </p>
          </div>
          <BookPrice price={book.price} previousPrice={book.previousPrice} />
        </div>
        <div className="mt-2 flex items-center gap-1 text-xs font-semibold text-muted">
          <span className="text-amber-500">&#9733;</span>
          <span>{book.rating.toFixed(1)}</span>
          <span>({book.stock > 0 ? book.stock : 1} reseñas)</span>
        </div>
        <div className="mt-3 grid grid-cols-2 gap-2">
          <button
            type="button"
            className="h-9 rounded-full border border-border bg-paper px-3 text-xs font-extrabold text-foreground transition hover:bg-card focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent"
          >
            Agregar
          </button>
          <button
            type="button"
            className="h-9 rounded-full bg-foreground px-3 text-xs font-extrabold text-paper shadow-[0_10px_22px_rgba(53,30,28,0.16)] transition hover:bg-accent focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent"
          >
            Comprar
          </button>
        </div>
      </div>
    </motion.article>
  );
}
