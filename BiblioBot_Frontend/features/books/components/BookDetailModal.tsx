"use client";

import { useEffect, useRef } from "react";
import { createPortal } from "react-dom";
import Image from "next/image";
import { motion, AnimatePresence } from "framer-motion";
import type { Book } from "../types/book.types";
import { defaultPriceLocale, priceFormatOptions } from "@/constants/currency";

type BookDetailModalProps = {
  book: Book | null;
  relatedBooks?: Book[];
  onClose: () => void;
};

const priceFormatter = new Intl.NumberFormat(
  defaultPriceLocale,
  priceFormatOptions,
);

function StarRating({ rating }: { rating: number }) {
  return (
    <div className="flex items-center gap-0.5">
      {[1, 2, 3, 4, 5].map((star) => {
        const filled = rating >= star;
        const half = !filled && rating >= star - 0.5;
        return (
          <svg
            key={star}
            className={`h-4 w-4 ${filled || half ? "text-[#d09a3f]" : "text-border"}`}
            viewBox="0 0 20 20"
            fill="currentColor"
          >
            {half ? (
              <>
                <defs>
                  <linearGradient id={`half-modal-${star}`}>
                    <stop offset="50%" stopColor="currentColor" />
                    <stop offset="50%" stopColor="transparent" />
                  </linearGradient>
                </defs>
                <path
                  fill={`url(#half-modal-${star})`}
                  d="M9.049 2.927c.3-.921 1.603-.921 1.902 0l1.07 3.292a1 1 0 00.95.69h3.462c.969 0 1.371 1.24.588 1.81l-2.8 2.034a1 1 0 00-.364 1.118l1.07 3.292c.3.921-.755 1.688-1.54 1.118l-2.8-2.034a1 1 0 00-1.175 0l-2.8 2.034c-.784.57-1.838-.197-1.539-1.118l1.07-3.292a1 1 0 00-.364-1.118L2.98 8.72c-.783-.57-.38-1.81.588-1.81h3.461a1 1 0 00.951-.69l1.07-3.292z"
                />
              </>
            ) : (
              <path d="M9.049 2.927c.3-.921 1.603-.921 1.902 0l1.07 3.292a1 1 0 00.95.69h3.462c.969 0 1.371 1.24.588 1.81l-2.8 2.034a1 1 0 00-.364 1.118l1.07 3.292c.3.921-.755 1.688-1.54 1.118l-2.8-2.034a1 1 0 00-1.175 0l-2.8 2.034c-.784.57-1.838-.197-1.539-1.118l1.07-3.292a1 1 0 00-.364-1.118L2.98 8.72c-.783-.57-.38-1.81.588-1.81h3.461a1 1 0 00.951-.69l1.07-3.292z" />
            )}
          </svg>
        );
      })}
    </div>
  );
}

export function BookDetailModal({ book, onClose }: BookDetailModalProps) {
  const panelRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const handleKey = (e: KeyboardEvent) => {
      if (e.key === "Escape") onClose();
    };
    window.addEventListener("keydown", handleKey);
    return () => window.removeEventListener("keydown", handleKey);
  }, [onClose]);

  useEffect(() => {
    if (book) {
      document.body.style.overflow = "hidden";
    } else {
      document.body.style.overflow = "";
    }
    return () => { document.body.style.overflow = ""; };
  }, [book]);

  const modalContent = (
    <AnimatePresence>
      {book && (
        <div className="fixed inset-0 z-[9999] isolate flex items-center justify-center px-4 py-6 sm:px-6 sm:py-12">
          {/* Backdrop */}
          <motion.div
            key="backdrop"
            className="fixed inset-0 z-[-1] bg-foreground/30 backdrop-blur-md"
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={{ opacity: 0 }}
            transition={{ duration: 0.3 }}
            onClick={onClose}
          />

          {/* Centered Modal Panel */}
          <motion.div
            key="panel"
            ref={panelRef}
            role="dialog"
            aria-modal="true"
            aria-label={`Detalles de ${book.title}`}
            className="relative flex w-full max-w-4xl flex-col overflow-hidden rounded-[28px] border border-white/60 bg-[#fdfbf7] shadow-[0_32px_80px_rgba(53,30,28,0.25)] sm:h-[80vh] sm:flex-row"
            initial={{ scale: 0.95, opacity: 0, y: 20 }}
            animate={{ scale: 1, opacity: 1, y: 0 }}
            exit={{ scale: 0.95, opacity: 0, y: 20 }}
            transition={{ type: "spring", damping: 32, stiffness: 320 }}
          >
            {/* Close button */}
            <button
              type="button"
              aria-label="Cerrar"
              onClick={onClose}
              className="absolute right-5 top-5 z-50 flex h-10 w-10 items-center justify-center rounded-full bg-black/5 text-foreground/70 backdrop-blur-md transition-all hover:bg-black/10 hover:text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent"
            >
              <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24">
                <path d="M6.75 6.75l10.5 10.5M17.25 6.75l-10.5 10.5" stroke="currentColor" strokeLinecap="round" strokeWidth="2.5" />
              </svg>
            </button>

            {/* --- Left Column: Book Cover (fixed on desktop) --- */}
            <div className="relative shrink-0 overflow-hidden bg-gradient-to-b from-[#f2ead8] to-[#fdfbf7] px-8 py-10 sm:w-[360px] sm:py-16">
              <div className="absolute -left-20 -top-20 z-0 h-64 w-64 rounded-full bg-accent/10 blur-[80px]" />
              
              <div className="relative z-10 mx-auto w-[180px] sm:w-[220px]">
                <div className="group relative w-full [perspective:1200px]">
                  {/* Projected shadow */}
                  <div className="absolute -bottom-4 left-1/2 h-6 w-[85%] -translate-x-1/2 rounded-[100%] bg-black/25 blur-xl transition-all duration-500" />
                  
                  {/* Cover wrapper */}
                  <div className="relative h-[270px] w-full origin-left overflow-hidden rounded-[10px] border border-white/60 bg-card shadow-[12px_24px_48px_rgba(53,30,28,0.3),inset_6px_0_0_rgba(0,0,0,0.15)] transition-transform duration-500 sm:h-[330px]">
                    <Image
                      src={book.image}
                      alt={`Portada de ${book.title}`}
                      fill
                      className="object-cover"
                      sizes="(max-width: 640px) 180px, 220px"
                      priority
                    />
                    {/* Lighting effects */}
                    <span className="absolute inset-y-0 left-0 w-6 bg-gradient-to-r from-black/40 via-black/10 to-transparent" />
                    <span className="absolute inset-y-0 right-0 w-2 bg-gradient-to-l from-white/50 to-transparent" />
                    <span className="absolute inset-x-4 top-0 h-px bg-white/50" />
                    
                    {book.badge && (
                      <span className="absolute bottom-4 left-1/2 max-w-[85%] -translate-x-1/2 rounded-full border border-white/40 bg-paper/95 px-3 py-1.5 text-center text-[0.6rem] font-black uppercase tracking-[0.1em] text-foreground shadow-sm">
                        {book.badge}
                      </span>
                    )}
                  </div>
                </div>
              </div>
            </div>

            {/* --- Right Column: Scrollable Info --- */}
            <div className="flex flex-1 flex-col overflow-y-auto bg-[#fdfbf7] px-8 pb-10 pt-8 sm:px-12 sm:pt-16 [scrollbar-color:rgba(53,30,28,0.18)_transparent] [scrollbar-width:thin]">
              
              {/* Category */}
              <span className="mb-4 inline-flex self-start rounded-full border border-accent/20 bg-accent/5 px-2.5 py-1 text-[0.65rem] font-black uppercase tracking-widest text-accent">
                {book.category}
              </span>
              
              <h2 className="text-3xl font-black leading-[1.15] tracking-tight text-foreground sm:text-4xl">
                {book.title}
              </h2>
              
              <p className="mt-2 text-base font-bold text-muted">
                Por <span className="text-coffee">{book.author}</span>
              </p>

              {/* Ratings */}
              <div className="mt-4 flex items-center gap-4">
                <div className="flex items-center gap-1.5 rounded-full bg-white px-3 py-1.5 shadow-sm">
                  <span className="text-sm font-black text-foreground">{book.rating.toFixed(1)}</span>
                  <StarRating rating={book.rating} />
                </div>
                <span className="text-xs font-bold text-muted underline decoration-border underline-offset-4 cursor-pointer hover:text-foreground transition">
                  Leer {book.stock} reseñas
                </span>
              </div>

              {/* Price & Stock Row */}
              <div className="mt-8 flex items-end gap-6 border-b border-border/50 pb-8">
                <div className="flex flex-col">
                  <span className="mb-1 text-[0.65rem] font-bold uppercase tracking-widest text-muted">Precio de lista</span>
                  <div className="flex items-end gap-3">
                    <span className="text-4xl font-black leading-none text-foreground">
                      {priceFormatter.format(book.price)}
                    </span>
                    {book.previousPrice && (
                      <span className="mb-1 text-base font-semibold text-muted line-through decoration-muted/50">
                        {priceFormatter.format(book.previousPrice)}
                      </span>
                    )}
                  </div>
                </div>
                
                {book.stock > 0 ? (
                  <span className="mb-1 flex items-center gap-1.5 rounded-full border border-[#d8c9ac] bg-[#f7f3eb] px-3 py-1 text-xs font-extrabold text-[#8c6b32]">
                    <span className="h-1.5 w-1.5 rounded-full bg-[#5ba85b]" />
                    En stock
                  </span>
                ) : (
                  <span className="mb-1 rounded-full border border-border bg-card px-3 py-1 text-xs font-bold text-muted">
                    Agotado
                  </span>
                )}
              </div>

              {/* Synopsis */}
              <div className="mt-8">
                <h3 className="text-[0.65rem] font-black uppercase tracking-widest text-muted">Sinopsis</h3>
                <p className="mt-3 text-[0.95rem] font-medium leading-relaxed text-foreground/80">
                  {book.description}
                </p>
              </div>

              {/* Primary Actions */}
              <div className="mt-10 flex flex-col gap-3 sm:flex-row">
                <button
                  type="button"
                  className="group relative flex h-14 flex-1 items-center justify-center overflow-hidden rounded-full bg-foreground px-8 font-black text-paper shadow-[0_8px_20px_rgba(53,30,28,0.25)] transition-all hover:-translate-y-0.5 hover:shadow-[0_12px_28px_rgba(53,30,28,0.35)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent"
                >
                  <span className="relative z-10">Agregar al carrito</span>
                  <div className="absolute inset-0 z-0 bg-gradient-to-r from-accent to-[#a0c9cb] opacity-0 transition-opacity duration-300 group-hover:opacity-100" />
                </button>
                
                <button
                  type="button"
                  className="flex h-14 flex-1 items-center justify-center rounded-full border-2 border-border bg-transparent px-8 font-black text-foreground transition hover:border-foreground hover:bg-card focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent"
                >
                  Leer fragmento
                </button>
                
                <button
                  type="button"
                  aria-label="Agregar a lista de deseos"
                  className="flex h-14 w-14 shrink-0 items-center justify-center rounded-full border-2 border-border bg-transparent text-muted transition hover:border-red-200 hover:bg-red-50 hover:text-red-500 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-red-500"
                >
                  <svg className="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth="2.5">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M4.318 6.318a4.5 4.5 0 000 6.364L12 20.364l7.682-7.682a4.5 4.5 0 00-6.364-6.364L12 7.636l-1.318-1.318a4.5 4.5 0 00-6.364 0z" />
                  </svg>
                </button>
              </div>

              {/* Spacer to allow scrolling past the buttons smoothly */}
              <div className="h-10 shrink-0" />
            </div>
          </motion.div>
        </div>
      )}
    </AnimatePresence>
  );

  if (typeof document === "undefined") return null;
  return createPortal(modalContent, document.body);
}
