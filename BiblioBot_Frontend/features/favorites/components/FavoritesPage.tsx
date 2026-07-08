"use client";

import Image from "next/image";
import Link from "next/link";
import { useEffect, useMemo, useState } from "react";
import { defaultPriceLocale, priceFormatOptions } from "@/constants/currency";
import { routes } from "@/constants/routes";
import { CartAuthError, addBookToCart } from "@/features/cart/services/cart.service";
import {
  FAVORITES_UPDATED_EVENT,
  FavoritesAuthError,
  clearFavorites,
  getFavorites,
  removeFavorite,
} from "../services/favorites.service";
import type { FavoriteBook } from "../types/favorite.types";

const priceFormatter = new Intl.NumberFormat(
  defaultPriceLocale,
  priceFormatOptions,
);

function formatPrice(value: number) {
  return priceFormatter.format(value);
}

export function FavoritesPage() {
  const [favorites, setFavorites] = useState<FavoriteBook[]>([]);
  const [pendingAction, setPendingAction] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const totalValue = useMemo(
    () => favorites.reduce((total, favorite) => total + favorite.price, 0),
    [favorites],
  );

  useEffect(() => {
    getFavorites()
      .then((nextFavorites) => {
        setFavorites(nextFavorites);
        setError(null);
      })
      .catch((loadError) => {
        setFavorites([]);
        setError(
          loadError instanceof FavoritesAuthError
            ? loadError.message
            : loadError instanceof Error
              ? loadError.message
              : "No se pudieron cargar tus favoritos.",
        );
      })
      .finally(() => setIsLoading(false));

    const handleFavoritesUpdated = (event: Event) => {
      const nextFavorites = (event as CustomEvent<FavoriteBook[]>).detail;
      setFavorites(Array.isArray(nextFavorites) ? nextFavorites : []);
    };

    window.addEventListener(FAVORITES_UPDATED_EVENT, handleFavoritesUpdated);

    return () => {
      window.removeEventListener(FAVORITES_UPDATED_EVENT, handleFavoritesUpdated);
    };
  }, []);

  const handleRemove = async (bookId: string) => {
    setPendingAction(bookId);
    setMessage(null);
    setError(null);

    try {
      setFavorites(await removeFavorite(bookId));
      setMessage("Libro eliminado de favoritos.");
    } catch (actionError) {
      setError(
        actionError instanceof Error
          ? actionError.message
          : "No se pudo eliminar el libro de favoritos.",
      );
    } finally {
      setPendingAction(null);
    }
  };

  const handleClear = async () => {
    setPendingAction("clear");
    setMessage(null);
    setError(null);

    try {
      setFavorites(await clearFavorites());
      setMessage("Lista de favoritos vaciada.");
    } catch (actionError) {
      setError(
        actionError instanceof Error
          ? actionError.message
          : "No se pudo vaciar la lista de favoritos.",
      );
    } finally {
      setPendingAction(null);
    }
  };

  const handleAddToCart = async (favorite: FavoriteBook) => {
    setPendingAction(favorite.id);
    setMessage(null);
    setError(null);

    try {
      await addBookToCart(favorite.id, favorite.stock);
      setMessage("Libro agregado al carrito.");
    } catch (actionError) {
      setError(
        actionError instanceof CartAuthError
          ? actionError.message
          : actionError instanceof Error
            ? actionError.message
            : "No se pudo agregar el libro al carrito.",
      );
    } finally {
      setPendingAction(null);
    }
  };

  return (
    <main className="min-h-screen bg-background px-5 pb-16 pt-36 text-foreground sm:px-8 md:pt-24 lg:px-12">
      <div className="mx-auto max-w-6xl">
        <div className="mb-7 flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
          <div>
            <p className="text-[0.68rem] font-black uppercase tracking-widest text-accent">
              Biblioteca personal
            </p>
            <h1 className="mt-2 text-4xl font-black leading-tight text-foreground sm:text-5xl">
              Favoritos
            </h1>
          </div>
          <Link
            href={routes.home}
            className="inline-flex h-10 items-center justify-center rounded-full border border-[rgba(53,30,28,0.24)] bg-paper px-5 text-[0.68rem] font-black uppercase tracking-widest text-foreground shadow-[0_6px_14px_rgba(53,30,28,0.06)] transition hover:border-accent hover:text-accent focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent"
          >
            Seguir explorando
          </Link>
        </div>

        {(message || error) && (
          <div
            className={`mb-5 rounded-[18px] border px-5 py-4 text-sm font-bold ${
              error
                ? "border-accent/25 bg-accent/5 text-foreground"
                : "border-[#b8d8c0] bg-[#eef8f0] text-[#315f3a]"
            }`}
          >
            {error ?? message}
            {error?.includes("iniciar sesion") && (
              <Link href="/auth/login" className="ml-2 text-accent underline underline-offset-4">
                Iniciar sesion
              </Link>
            )}
          </div>
        )}

        {isLoading ? (
          <section className="border-y border-border/70 bg-paper/55 px-5 py-16 text-center">
            <p className="text-sm font-black uppercase tracking-widest text-muted">
              Cargando favoritos
            </p>
          </section>
        ) : favorites.length === 0 ? (
          <section className="border-y border-border/70 bg-paper/55 px-5 py-16 text-center">
            <div className="mx-auto flex h-16 w-16 items-center justify-center rounded-full border border-[rgba(53,30,28,0.24)] bg-[#f8efe9] text-2xl text-accent shadow-[0_10px_24px_rgba(53,30,28,0.08)]">
              <span aria-hidden="true">♡</span>
            </div>
            <h2 className="mt-6 text-2xl font-black text-foreground">
              Aun no tienes favoritos
            </h2>
            <p className="mx-auto mt-3 max-w-md text-sm font-semibold leading-6 text-muted">
              Marca libros con el corazon para guardarlos aqui y volver a ellos cuando quieras.
            </p>
            <Link
              href="/#destacados"
              className="mt-7 inline-flex h-12 items-center justify-center rounded-full bg-foreground px-7 text-sm font-black text-paper shadow-[0_10px_24px_rgba(53,30,28,0.18)] transition hover:bg-accent focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent"
            >
              Ver libros
            </Link>
          </section>
        ) : (
          <section className="grid gap-8 lg:grid-cols-[1fr_320px]">
            <div className="grid gap-4 sm:grid-cols-2">
              {favorites.map((favorite) => (
                <article
                  key={favorite.id}
                  className="border border-[rgba(53,30,28,0.18)] bg-paper/70 p-4 shadow-[0_10px_24px_rgba(53,30,28,0.05)]"
                >
                  <Link
                    href={`/books/${favorite.slug}`}
                    className="group grid gap-4 sm:grid-cols-[92px_1fr]"
                  >
                    <div className="relative h-36 w-24 overflow-hidden rounded-[8px] border border-white/60 bg-card shadow-[8px_14px_26px_rgba(53,30,28,0.18),inset_5px_0_0_rgba(0,0,0,0.14)]">
                      <Image
                        src={favorite.image}
                        alt={`Portada de ${favorite.title}`}
                        fill
                        className="object-cover transition duration-500 group-hover:scale-[1.03]"
                        sizes="96px"
                      />
                      <span className="absolute inset-y-0 left-0 w-4 bg-gradient-to-r from-black/35 via-black/10 to-transparent" />
                    </div>

                    <div className="min-w-0">
                      <p className="text-[0.65rem] font-black uppercase tracking-widest text-accent">
                        {favorite.category}
                      </p>
                      <h2 className="mt-2 line-clamp-2 text-base font-black leading-6 text-foreground">
                        {favorite.title}
                      </h2>
                      <p className="mt-1 truncate text-xs font-bold text-muted">
                        Por {favorite.author}
                      </p>
                      <p className="mt-3 text-lg font-black text-foreground">
                        {formatPrice(favorite.price)}
                      </p>
                    </div>
                  </Link>

                  <div className="mt-4 flex flex-wrap gap-2">
                    <button
                      type="button"
                      disabled={pendingAction !== null || favorite.stock <= 0}
                      onClick={() => void handleAddToCart(favorite)}
                      className="flex h-10 flex-1 items-center justify-center rounded-full bg-foreground px-4 text-[0.72rem] font-black text-paper shadow-[0_8px_18px_rgba(53,30,28,0.14)] transition hover:bg-accent disabled:cursor-not-allowed disabled:opacity-50 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent"
                    >
                      {pendingAction === favorite.id ? "Agregando..." : "Al carrito"}
                    </button>
                    <button
                      type="button"
                      disabled={pendingAction !== null}
                      onClick={() => void handleRemove(favorite.id)}
                      className="flex h-10 items-center justify-center rounded-full border border-[rgba(53,30,28,0.24)] bg-transparent px-4 text-[0.72rem] font-black text-foreground transition hover:border-accent hover:text-accent disabled:cursor-not-allowed disabled:opacity-50 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent"
                    >
                      {pendingAction === favorite.id ? "Quitando..." : "Quitar"}
                    </button>
                  </div>
                </article>
              ))}
            </div>

            <aside className="h-fit border border-[rgba(53,30,28,0.18)] bg-paper/80 p-5 shadow-[0_18px_40px_rgba(53,30,28,0.08)] lg:sticky lg:top-24">
              <h2 className="text-lg font-black text-foreground">
                Resumen
              </h2>
              <div className="mt-5 space-y-3 border-y border-border/70 py-5 text-sm font-bold">
                <div className="flex justify-between gap-4">
                  <span className="text-muted">Guardados</span>
                  <span className="text-foreground">{favorites.length}</span>
                </div>
                <div className="flex justify-between gap-4">
                  <span className="text-muted">Valor referencial</span>
                  <span className="text-foreground">{formatPrice(totalValue)}</span>
                </div>
              </div>
              <button
                type="button"
                disabled={pendingAction !== null}
                onClick={() => void handleClear()}
                className="mt-5 flex h-11 w-full items-center justify-center rounded-full border border-[rgba(53,30,28,0.24)] bg-transparent px-6 text-[0.72rem] font-black text-foreground transition hover:border-accent hover:text-accent focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent"
              >
                {pendingAction === "clear" ? "Vaciando..." : "Vaciar favoritos"}
              </button>
            </aside>
          </section>
        )}
      </div>
    </main>
  );
}
