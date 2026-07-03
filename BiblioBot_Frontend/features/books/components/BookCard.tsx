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
  const stockLabel = book.stock > 0 ? `${book.stock} disponibles` : "Preventa";

  return (
    <motion.article
      className="group flex h-full flex-col overflow-hidden rounded-xl border border-border bg-card p-4 shadow-2xl shadow-black/20 backdrop-blur-xl transition-colors duration-300 hover:border-accent/50 hover:bg-white/[0.1]"
      initial={shouldReduceMotion ? { opacity: 1 } : { opacity: 0, y: 26, scale: 0.98 }}
      whileInView={
        shouldReduceMotion ? { opacity: 1 } : { opacity: 1, y: 0, scale: 1 }
      }
      whileHover={shouldReduceMotion ? undefined : { y: -6 }}
      transition={{ delay: revealDelay, duration: 0.66, ease: [0.22, 1, 0.36, 1] }}
      viewport={{ once: true, amount: 0.22, margin: "0px 0px -8% 0px" }}
    >
      <div className="relative aspect-[3/4] overflow-hidden rounded-lg bg-card-solid">
        <Image
          src={book.image}
          alt={`Portada de ${book.title}`}
          fill
          className="object-cover transition duration-500 group-hover:scale-105"
          sizes="(max-width: 768px) 70vw, 220px"
        />
        {book.badge ? (
          <span className="absolute left-3 top-3 rounded-full border border-border bg-background/[0.82] px-3 py-1 text-xs font-medium text-accent-soft backdrop-blur">
            {book.badge}
          </span>
        ) : null}
      </div>
      <div className="mt-4 flex flex-1 flex-col space-y-3">
        <div>
          <p className="text-xs uppercase tracking-[0.16em] text-accent">
            {book.category}
          </p>
          <h3 className="mt-1 text-lg font-semibold leading-tight text-foreground">
            {book.title}
          </h3>
          <p className="text-sm text-muted">{book.author}</p>
        </div>
        <p className="line-clamp-2 text-sm leading-6 text-muted">
          {book.description}
        </p>
        <div className="mt-auto flex items-center justify-between gap-3 border-t border-border pt-4">
          <BookPrice price={book.price} previousPrice={book.previousPrice} />
          <span className="rounded-full bg-accent/10 px-2.5 py-1 text-sm font-medium text-accent-soft">
            {book.rating.toFixed(1)}
          </span>
        </div>
        <div className="flex items-center justify-between gap-3">
          <p className="text-xs text-muted">{stockLabel}</p>
          <button
            type="button"
            className="rounded-full border border-border px-3 py-2 text-xs font-semibold text-foreground transition hover:border-accent/60 hover:bg-accent/10 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent"
          >
            Ver detalle
          </button>
        </div>
      </div>
    </motion.article>
  );
}
