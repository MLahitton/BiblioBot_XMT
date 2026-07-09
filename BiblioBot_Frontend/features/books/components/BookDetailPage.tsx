"use client";

import Image from "next/image";
import Link from "next/link";
import { useEffect, useState } from "react";
import type { Book } from "../types/book.types";
import { defaultPriceLocale, priceFormatOptions } from "@/constants/currency";
import { CartAuthError, addBookToCart } from "@/features/cart/services/cart.service";
import {
  BookReviewAuthError,
  getBookReviews,
  saveBookReview,
  type BookReviewsSummary,
} from "../services/book-reviews.service";
import {
  FavoritesAuthError,
  isFavoriteBook,
  toggleFavorite,
} from "@/features/favorites/services/favorites.service";

type BookDetailPageProps = {
  book: Book;
  relatedBooks: Book[];
};

const priceFormatter = new Intl.NumberFormat(
  defaultPriceLocale,
  priceFormatOptions,
);

const reviewDateFormatter = new Intl.DateTimeFormat("es-CO", {
  day: "2-digit",
  month: "short",
  year: "numeric",
});

function StarRating({ rating }: { rating: number }) {
  return (
    <div className="flex items-center gap-0.5">
      {[1, 2, 3, 4, 5].map((star) => (
        <svg
          key={star}
          className={`h-4 w-4 ${rating >= star ? "text-[#d09a3f]" : "text-border"}`}
          viewBox="0 0 20 20"
          fill="currentColor"
          aria-hidden="true"
        >
          <path d="M9.049 2.927c.3-.921 1.603-.921 1.902 0l1.07 3.292a1 1 0 00.95.69h3.462c.969 0 1.371 1.24.588 1.81l-2.8 2.034a1 1 0 00-.364 1.118l1.07 3.292c.3.921-.755 1.688-1.54 1.118l-2.8-2.034a1 1 0 00-1.175 0l-2.8 2.034c-.784.57-1.838-.197-1.539-1.118l1.07-3.292a1 1 0 00-.364-1.118L2.98 8.72c-.783-.57-.38-1.81.588-1.81h3.461a1 1 0 00.951-.69l1.07-3.292z" />
        </svg>
      ))}
    </div>
  );
}

export function BookDetailPage({ book, relatedBooks }: BookDetailPageProps) {
  const [cartMessage, setCartMessage] = useState<string | null>(null);
  const [cartError, setCartError] = useState<string | null>(null);
  const [isAddingToCart, setIsAddingToCart] = useState(false);
  const [isFavorite, setIsFavorite] = useState(false);
  const [isUpdatingFavorite, setIsUpdatingFavorite] = useState(false);
  const [reviewsSummary, setReviewsSummary] = useState<BookReviewsSummary | null>(null);
  const [reviewsError, setReviewsError] = useState<string | null>(null);
  const [reviewMessage, setReviewMessage] = useState<string | null>(null);
  const [reviewRating, setReviewRating] = useState(5);
  const [reviewComment, setReviewComment] = useState("");
  const [isLoadingReviews, setIsLoadingReviews] = useState(true);
  const [isSavingReview, setIsSavingReview] = useState(false);

  useEffect(() => {
    let isMounted = true;

    isFavoriteBook(book.id)
      .then((nextIsFavorite) => {
        if (isMounted) {
          setIsFavorite(nextIsFavorite);
        }
      })
      .catch(() => {
        if (isMounted) {
          setIsFavorite(false);
        }
      });

    return () => {
      isMounted = false;
    };
  }, [book.id]);

  useEffect(() => {
    let isMounted = true;
    const timeoutId = window.setTimeout(() => {
      setIsLoadingReviews(true);
      setReviewsError(null);
      setReviewMessage(null);

      getBookReviews(book.id)
        .then((summary) => {
          if (isMounted) {
            setReviewsSummary(summary);
          }
        })
        .catch((error) => {
          if (isMounted) {
            setReviewsError(
              error instanceof Error
                ? error.message
                : "No se pudieron cargar las resenas.",
            );
          }
        })
        .finally(() => {
          if (isMounted) {
            setIsLoadingReviews(false);
          }
        });
    }, 0);

    return () => {
      isMounted = false;
      window.clearTimeout(timeoutId);
    };
  }, [book.id]);

  const displayedRating = reviewsSummary?.averageRating ?? book.rating;
  const displayedReviewCount = reviewsSummary?.reviewCount ?? book.reviewCount;

  const handleAddToCart = async () => {
    setIsAddingToCart(true);
    setCartMessage(null);
    setCartError(null);

    try {
      await addBookToCart(book.id, book.stock);
      setCartMessage("Libro agregado al carrito.");
    } catch (error) {
      setCartError(
        error instanceof CartAuthError
          ? error.message
          : error instanceof Error
            ? error.message
            : "No se pudo agregar el libro al carrito.",
      );
    } finally {
      setIsAddingToCart(false);
    }
  };

  const handleToggleFavorite = async () => {
    setIsUpdatingFavorite(true);
    setCartError(null);
    setCartMessage(null);

    try {
      const favorites = await toggleFavorite(book);
      const nextIsFavorite = favorites.some((favorite) => favorite.id === book.id);
      setIsFavorite(nextIsFavorite);
      setCartMessage(
        nextIsFavorite
          ? "Libro agregado a favoritos."
          : "Libro eliminado de favoritos.",
      );
    } catch (error) {
      setCartError(
        error instanceof FavoritesAuthError
          ? error.message
          : error instanceof Error
            ? error.message
            : "No se pudo actualizar favoritos.",
      );
    } finally {
      setIsUpdatingFavorite(false);
    }
  };

  const handleSaveReview = async () => {
    const comment = reviewComment.trim();
    setReviewsError(null);
    setReviewMessage(null);

    if (comment.length < 5) {
      setReviewsError("La resena debe tener al menos 5 caracteres.");
      return;
    }

    setIsSavingReview(true);

    try {
      const summary = await saveBookReview(book.id, {
        rating: reviewRating,
        comment,
      });
      setReviewsSummary(summary);
      setReviewComment("");
      setReviewRating(5);
      setReviewMessage("Tu resena se guardo correctamente.");
    } catch (error) {
      setReviewsError(
        error instanceof BookReviewAuthError
          ? error.message
          : error instanceof Error
            ? error.message
            : "No se pudo guardar la resena.",
      );
    } finally {
      setIsSavingReview(false);
    }
  };

  return (
    <main className="min-h-screen bg-background px-5 pb-16 pt-36 text-foreground sm:px-8 md:pt-24 lg:px-12">
      <div className="mx-auto max-w-6xl">
        <Link
          href="/#destacados"
          className="mb-6 inline-flex items-center gap-2 rounded-full border border-border bg-paper px-4 py-2 text-xs font-black uppercase tracking-widest text-muted shadow-sm transition hover:border-accent hover:text-accent focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent"
        >
          <span aria-hidden="true">{"<"}</span>
          Volver a destacados
        </Link>

        <section className="grid gap-10 border-y border-border/70 bg-paper/55 py-10 lg:grid-cols-[minmax(280px,420px)_1fr] lg:gap-14 lg:py-14">
          <div className="relative flex items-start justify-center overflow-hidden bg-gradient-to-b from-[#f2ead8] to-[#fdfbf7] px-8 py-10 lg:sticky lg:top-28 lg:min-h-[620px] lg:py-16">
            <div className="absolute -left-20 -top-20 z-0 h-64 w-64 rounded-full bg-accent/10 blur-[80px]" />
            <div className="relative z-10 w-[220px] sm:w-[280px]">
              <div className="relative w-full [perspective:1200px]">
                <div className="absolute -bottom-5 left-1/2 h-8 w-[85%] -translate-x-1/2 rounded-[100%] bg-black/25 blur-xl" />
                <div className="relative h-[330px] w-full origin-left overflow-hidden rounded-[10px] border border-white/60 bg-card shadow-[18px_30px_58px_rgba(53,30,28,0.3),inset_7px_0_0_rgba(0,0,0,0.15)] sm:h-[420px]">
                  <Image
                    src={book.image}
                    alt={`Portada de ${book.title}`}
                    fill
                    className="object-cover"
                    sizes="(max-width: 640px) 220px, 280px"
                    priority
                  />
                  <span className="absolute inset-y-0 left-0 w-7 bg-gradient-to-r from-black/40 via-black/10 to-transparent" />
                  <span className="absolute inset-y-0 right-0 w-2 bg-gradient-to-l from-white/50 to-transparent" />
                  <span className="absolute inset-x-4 top-0 h-px bg-white/50" />
                  {book.badge && (
                    <span className="absolute bottom-5 left-1/2 max-w-[85%] -translate-x-1/2 rounded-full border border-white/40 bg-paper/95 px-4 py-2 text-center text-[0.64rem] font-black uppercase tracking-[0.1em] text-foreground shadow-sm">
                      {book.badge}
                    </span>
                  )}
                </div>
              </div>
            </div>
          </div>

          <div className="px-1 lg:py-4">
            <span className="mb-4 inline-flex rounded-full border border-accent/20 bg-accent/5 px-2.5 py-1 text-[0.65rem] font-black uppercase tracking-widest text-accent">
              {book.category}
            </span>

            <h1 className="max-w-3xl text-4xl font-black leading-[1.08] text-foreground sm:text-5xl">
              {book.title}
            </h1>

            <p className="mt-3 text-base font-bold text-muted">
              Por <span className="text-coffee">{book.author}</span>
            </p>

            <div className="mt-5 flex flex-wrap items-center gap-4">
              <div className="flex items-center gap-1.5 rounded-full bg-white px-3 py-1.5 shadow-sm">
                <span className="text-sm font-black text-foreground">
                  {displayedRating.toFixed(1)}
                </span>
                <StarRating rating={displayedRating} />
              </div>
              <a
                href="#resenas"
                className="text-xs font-bold text-muted underline decoration-border underline-offset-4 transition hover:text-foreground"
              >
                Leer {displayedReviewCount} resenas
              </a>
            </div>

            <div className="mt-8 border-y border-border/60 py-8">
              <span className="mb-1 block text-[0.65rem] font-bold uppercase tracking-widest text-muted">
                Precio de lista
              </span>
              <div className="flex flex-wrap items-end gap-4">
                <span className="text-4xl font-black leading-none text-foreground sm:text-5xl">
                  {priceFormatter.format(book.price)}
                </span>
                {book.previousPrice && (
                  <span className="text-base font-semibold text-muted line-through decoration-muted/50">
                    {priceFormatter.format(book.previousPrice)}
                  </span>
                )}
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
            </div>

            <div className="mt-8">
              <h2 className="text-[0.65rem] font-black uppercase tracking-widest text-muted">
                Sinopsis
              </h2>
              <p className="mt-3 max-w-2xl text-base font-medium leading-8 text-foreground/80">
                {book.description}
              </p>
            </div>

            <div className="mt-10 flex flex-col gap-3 sm:flex-row">
              <button
                type="button"
                disabled={book.stock <= 0 || isAddingToCart}
                onClick={handleAddToCart}
                className="flex h-14 flex-1 items-center justify-center rounded-full bg-foreground px-8 font-black text-paper shadow-[0_8px_20px_rgba(53,30,28,0.25)] transition hover:-translate-y-0.5 hover:bg-accent hover:shadow-[0_12px_28px_rgba(53,30,28,0.35)] disabled:cursor-not-allowed disabled:opacity-55 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent"
              >
                {isAddingToCart ? "Agregando..." : "Agregar al carrito"}
              </button>
              <button
                type="button"
                className="flex h-14 flex-1 items-center justify-center rounded-full border-2 border-border bg-transparent px-8 font-black text-foreground transition hover:border-foreground hover:bg-card focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent"
              >
                Leer fragmento
              </button>
              <button
                type="button"
                aria-label={isFavorite ? "Quitar de favoritos" : "Agregar a favoritos"}
                disabled={isUpdatingFavorite}
                onClick={() => void handleToggleFavorite()}
                className={`flex h-14 w-full items-center justify-center rounded-full border-2 transition focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent sm:w-14 ${
                  isFavorite
                    ? "border-accent bg-accent/10 text-accent"
                    : "border-border bg-transparent text-muted hover:border-accent hover:bg-accent/5 hover:text-accent"
                }`}
              >
                <svg
                  className="h-6 w-6"
                  fill={isFavorite ? "currentColor" : "none"}
                  viewBox="0 0 24 24"
                  stroke="currentColor"
                  strokeWidth="2.5"
                  aria-hidden="true"
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    d="M4.318 6.318a4.5 4.5 0 000 6.364L12 20.364l7.682-7.682a4.5 4.5 0 00-6.364-6.364L12 7.636l-1.318-1.318a4.5 4.5 0 00-6.364 0z"
                  />
                </svg>
              </button>
            </div>

            {(cartMessage || cartError) && (
              <div
                className={`mt-4 rounded-[18px] border px-5 py-4 text-sm font-bold ${
                  cartError
                    ? "border-accent/25 bg-accent/5 text-foreground"
                    : "border-[#b8d8c0] bg-[#eef8f0] text-[#315f3a]"
                }`}
              >
                {cartError ?? cartMessage}
                {cartError?.includes("iniciar sesion") ? (
                  <Link href="/auth/login" className="ml-2 text-accent underline underline-offset-4">
                    Iniciar sesion
                  </Link>
                ) : cartMessage?.includes("favoritos") ? (
                  <Link href="/favorites" className="ml-2 text-accent underline underline-offset-4">
                    Ver favoritos
                  </Link>
                ) : cartMessage ? (
                  <Link href="/cart" className="ml-2 text-accent underline underline-offset-4">
                    Ver carrito
                  </Link>
                ) : null}
              </div>
            )}

            <section id="resenas" className="mt-12 border-t border-border/60 pt-8">
              <div className="flex flex-col gap-2 sm:flex-row sm:items-end sm:justify-between">
                <div>
                  <h2 className="text-lg font-black text-foreground">
                    Resenas de lectores
                  </h2>
                  <p className="mt-1 text-sm font-semibold text-muted">
                    {displayedReviewCount} opiniones guardadas en la base de datos.
                  </p>
                </div>
                <div className="flex items-center gap-2 rounded-full border border-border bg-paper px-3 py-1.5">
                  <span className="text-sm font-black text-foreground">
                    {displayedRating.toFixed(1)}
                  </span>
                  <StarRating rating={displayedRating} />
                </div>
              </div>

              <div className="mt-5 border border-border bg-paper/75 p-4">
                <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
                  <div>
                    <p className="text-xs font-black uppercase tracking-widest text-accent">
                      Tu resena
                    </p>
                    <p className="mt-1 text-sm font-semibold text-muted">
                      Comparte una opinion real sobre este libro.
                    </p>
                  </div>
                  <div className="flex gap-1">
                    {[1, 2, 3, 4, 5].map((rating) => (
                      <button
                        key={rating}
                        type="button"
                        onClick={() => setReviewRating(rating)}
                        className={`h-9 w-9 rounded-full border text-sm font-black transition focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent ${
                          reviewRating >= rating
                            ? "border-[#d09a3f] bg-[#fff6df] text-[#a9731c]"
                            : "border-border bg-background text-muted hover:border-[#d09a3f]"
                        }`}
                        aria-label={`${rating} estrellas`}
                      >
                        {rating}
                      </button>
                    ))}
                  </div>
                </div>
                <textarea
                  value={reviewComment}
                  onChange={(event) => setReviewComment(event.target.value)}
                  maxLength={1000}
                  rows={4}
                  placeholder="Escribe que te parecio el libro..."
                  className="mt-4 w-full resize-none border border-border bg-background px-4 py-3 text-sm font-semibold leading-6 text-foreground outline-none transition placeholder:text-muted/60 focus:border-accent focus:ring-1 focus:ring-accent"
                />
                <div className="mt-3 flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
                  <p className="text-xs font-bold text-muted">
                    {reviewComment.trim().length}/1000 caracteres
                  </p>
                  <button
                    type="button"
                    onClick={() => void handleSaveReview()}
                    disabled={isSavingReview}
                    className="h-10 rounded-full bg-foreground px-5 text-xs font-black uppercase tracking-widest text-paper transition hover:bg-accent disabled:cursor-not-allowed disabled:opacity-55 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent"
                  >
                    {isSavingReview ? "Guardando" : "Guardar resena"}
                  </button>
                </div>
                {(reviewsError || reviewMessage) ? (
                  <p
                    className={`mt-3 text-sm font-bold ${
                      reviewsError ? "text-accent" : "text-[#315f3a]"
                    }`}
                  >
                    {reviewsError ?? reviewMessage}
                    {reviewsError?.includes("iniciar sesion") ? (
                      <Link href="/auth/login" className="ml-2 underline underline-offset-4">
                        Iniciar sesion
                      </Link>
                    ) : null}
                  </p>
                ) : null}
              </div>

              <div className="mt-5 grid gap-3">
                {isLoadingReviews ? (
                  <div className="border border-border bg-paper p-5 text-sm font-bold text-muted">
                    Cargando resenas...
                  </div>
                ) : reviewsSummary?.items.length ? (
                  reviewsSummary.items.map((review) => (
                    <article key={review.id} className="border border-border bg-paper p-4">
                      <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
                        <div>
                          <p className="text-xs font-black uppercase tracking-widest text-accent">
                            {review.isVerifiedPurchase ? "Compra verificada" : "Lector registrado"}
                          </p>
                          <h3 className="mt-2 text-sm font-black text-foreground">
                            {review.userFullName}
                          </h3>
                        </div>
                        <div className="flex items-center gap-2">
                          <span className="text-sm font-black text-foreground">
                            {review.rating.toFixed(1)}
                          </span>
                          <StarRating rating={review.rating} />
                        </div>
                      </div>
                      <p className="mt-3 text-sm font-semibold leading-6 text-foreground/80">
                        {review.comment}
                      </p>
                      <p className="mt-3 text-xs font-bold text-muted">
                        {reviewDateFormatter.format(new Date(review.updatedAt ?? review.createdAt))}
                      </p>
                    </article>
                  ))
                ) : (
                  <div className="border border-border bg-paper p-5 text-sm font-bold text-muted">
                    Aun no hay resenas para este libro.
                  </div>
                )}
              </div>
            </section>

          </div>
        </section>

        {relatedBooks.length > 0 && (
          <section className="mt-12">
            <h2 className="text-xl font-black text-foreground">
              Tambien te puede gustar
            </h2>
            <div className="mt-5 grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
              {relatedBooks.map((relatedBook) => (
                <Link
                  key={relatedBook.id}
                  href={`/books/${relatedBook.slug}`}
                  className="group flex gap-4 border border-border bg-paper p-4 transition hover:border-accent/50 hover:bg-card focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent"
                >
                  <div className="relative h-28 w-20 shrink-0 overflow-hidden rounded-[6px] bg-card shadow-sm">
                    <Image
                      src={relatedBook.image}
                      alt={`Portada de ${relatedBook.title}`}
                      fill
                      className="object-cover transition duration-500 group-hover:scale-[1.03]"
                      sizes="80px"
                    />
                  </div>
                  <div>
                    <p className="text-xs font-black uppercase tracking-widest text-accent">
                      {relatedBook.category}
                    </p>
                    <h3 className="mt-2 text-sm font-black leading-5 text-foreground">
                      {relatedBook.title}
                    </h3>
                    <p className="mt-1 text-xs font-semibold text-muted">
                      {relatedBook.author}
                    </p>
                    <p className="mt-3 text-sm font-black text-foreground">
                      {priceFormatter.format(relatedBook.price)}
                    </p>
                  </div>
                </Link>
              ))}
            </div>
          </section>
        )}
      </div>
    </main>
  );
}
