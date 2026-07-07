"use client";

import Image from "next/image";
import Link from "next/link";
import { useEffect, useMemo, useState } from "react";
import { defaultPriceLocale, priceFormatOptions } from "@/constants/currency";
import { routes } from "@/constants/routes";
import {
  CartAuthError,
  addOrUpdateCartItem,
  clearCart,
  createSaleFromCart,
  getCurrentCart,
  getEmptyCart,
  removeCartItem,
} from "../services/cart.service";
import type { Cart, Sale } from "../types/cart.types";

const priceFormatter = new Intl.NumberFormat(
  defaultPriceLocale,
  priceFormatOptions,
);

const fallbackCover = "/images/books/book-01.svg";

function formatPrice(value: number) {
  return priceFormatter.format(value);
}

export function CartPage() {
  const [cart, setCart] = useState<Cart>(() => getEmptyCart());
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [successSale, setSuccessSale] = useState<Sale | null>(null);
  const [pendingAction, setPendingAction] = useState<string | null>(null);
  const isEmpty = cart.items.length === 0;

  const totals = useMemo(() => {
    const subtotal = cart.subtotal ?? 0;
    const tax = 0;
    return {
      subtotal,
      tax,
      total: subtotal + tax,
    };
  }, [cart.subtotal]);

  useEffect(() => {
    let isMounted = true;

    getCurrentCart()
      .then((currentCart) => {
        if (!isMounted) return;
        setCart(currentCart);
        setError(null);
      })
      .catch((loadError) => {
        if (!isMounted) return;
        setError(
          loadError instanceof CartAuthError
            ? loadError.message
            : "No se pudo cargar tu carrito.",
        );
      })
      .finally(() => {
        if (isMounted) setIsLoading(false);
      });

    return () => {
      isMounted = false;
    };
  }, []);

  const runCartAction = async (
    actionId: string,
    action: () => Promise<Cart>,
  ) => {
    setPendingAction(actionId);
    setError(null);

    try {
      const nextCart = await action();
      setCart(nextCart);
    } catch (actionError) {
      setError(
        actionError instanceof Error
          ? actionError.message
          : "No se pudo actualizar el carrito.",
      );
    } finally {
      setPendingAction(null);
    }
  };

  const handleQuantityChange = (bookId: string, quantity: number) => {
    if (quantity <= 0) {
      void runCartAction(`remove-${bookId}`, () => removeCartItem(bookId));
      return;
    }

    void runCartAction(`qty-${bookId}`, () =>
      addOrUpdateCartItem({
        bookId,
        quantity,
      }),
    );
  };

  const handleClearCart = () => {
    void runCartAction("clear", async () => {
      await clearCart();
      return getEmptyCart();
    });
  };

  const handleCheckout = async () => {
    setPendingAction("checkout");
    setError(null);
    setSuccessSale(null);

    try {
      const sale = await createSaleFromCart();
      setSuccessSale(sale);

      try {
        await clearCart();
      } catch {
        // La venta ya fue creada; el carrito visual se reinicia igualmente.
      }

      setCart(getEmptyCart());
    } catch (checkoutError) {
      setError(
        checkoutError instanceof Error
          ? checkoutError.message
          : "No se pudo confirmar el pedido.",
      );
    } finally {
      setPendingAction(null);
    }
  };

  if (isLoading) {
    return (
      <main className="min-h-screen bg-background px-5 pb-16 pt-24 text-foreground sm:px-8 lg:px-12">
        <section className="mx-auto max-w-6xl border-y border-border/70 bg-paper/55 py-14">
          <p className="text-center text-sm font-black uppercase tracking-widest text-muted">
            Cargando carrito
          </p>
        </section>
      </main>
    );
  }

  return (
    <main className="min-h-screen bg-background px-5 pb-16 pt-24 text-foreground sm:px-8 lg:px-12">
      <div className="mx-auto max-w-6xl">
        <div className="mb-7 flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
          <div>
            <p className="text-[0.68rem] font-black uppercase tracking-widest text-accent">
              Tu seleccion
            </p>
            <h1 className="mt-2 text-4xl font-black leading-tight text-foreground sm:text-5xl">
              Carrito
            </h1>
          </div>
          <Link
            href={routes.home}
            className="inline-flex h-10 items-center justify-center rounded-full border border-[rgba(53,30,28,0.24)] bg-paper px-5 text-[0.68rem] font-black uppercase tracking-widest text-foreground shadow-[0_6px_14px_rgba(53,30,28,0.06)] transition hover:border-accent hover:text-accent focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent"
          >
            Seguir explorando
          </Link>
        </div>

        {error && (
          <div className="mb-5 rounded-[18px] border border-accent/25 bg-accent/5 px-5 py-4 text-sm font-bold text-foreground">
            {error}
            {error.includes("iniciar sesion") && (
              <Link href="/auth/login" className="ml-2 text-accent underline underline-offset-4">
                Iniciar sesion
              </Link>
            )}
          </div>
        )}

        {successSale && (
          <div className="mb-5 rounded-[18px] border border-[#b8d8c0] bg-[#eef8f0] px-5 py-4 text-sm font-bold text-[#315f3a]">
            Pedido creado correctamente. Codigo: {successSale.id.slice(0, 8).toUpperCase()}
          </div>
        )}

        {isEmpty ? (
          <section className="border-y border-border/70 bg-paper/55 px-5 py-16 text-center">
            <div className="mx-auto flex h-16 w-16 items-center justify-center rounded-full border border-[rgba(53,30,28,0.24)] bg-[#f8efe9] shadow-[0_10px_24px_rgba(53,30,28,0.08)]">
              <Image src="/icons/cart.svg" alt="" width={24} height={24} />
            </div>
            <h2 className="mt-6 text-2xl font-black text-foreground">
              Tu carrito esta vacio
            </h2>
            <p className="mx-auto mt-3 max-w-md text-sm font-semibold leading-6 text-muted">
              Agrega libros desde el catalogo y vuelve aqui para revisar cantidades antes de confirmar.
            </p>
            <Link
              href="/#destacados"
              className="mt-7 inline-flex h-12 items-center justify-center rounded-full bg-foreground px-7 text-sm font-black text-paper shadow-[0_10px_24px_rgba(53,30,28,0.18)] transition hover:bg-accent focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent"
            >
              Ver libros
            </Link>
          </section>
        ) : (
          <section className="grid gap-8 lg:grid-cols-[1fr_340px]">
            <div className="space-y-3">
              {cart.items.map((item) => (
                <article
                  key={item.id}
                  className="grid gap-4 border border-[rgba(53,30,28,0.18)] bg-paper/70 p-4 shadow-[0_10px_24px_rgba(53,30,28,0.05)] sm:grid-cols-[84px_1fr_auto] sm:items-center"
                >
                  <div className="relative h-28 w-20 overflow-hidden rounded-[8px] border border-border bg-card shadow-sm">
                    <Image
                      src={item.imageUrl?.trim() || fallbackCover}
                      alt={`Portada de ${item.bookTitle}`}
                      fill
                      className="object-cover"
                      sizes="80px"
                    />
                  </div>

                  <div className="min-w-0">
                    <h2 className="truncate text-base font-black text-foreground">
                      {item.bookTitle}
                    </h2>
                    {item.isbn && (
                      <p className="mt-1 text-xs font-bold text-muted">
                        ISBN {item.isbn}
                      </p>
                    )}
                    <p className="mt-3 text-sm font-black text-foreground">
                      {formatPrice(item.unitPrice)}
                    </p>
                  </div>

                  <div className="flex flex-wrap items-center gap-3 sm:justify-end">
                    <div className="flex h-10 items-center overflow-hidden rounded-full border border-[rgba(53,30,28,0.24)] bg-[#f8efe9]">
                      <button
                        type="button"
                        aria-label="Disminuir cantidad"
                        disabled={pendingAction !== null}
                        onClick={() => handleQuantityChange(item.bookId, item.quantity - 1)}
                        className="h-full w-10 text-lg font-black text-foreground transition hover:bg-paper hover:text-accent disabled:cursor-not-allowed disabled:opacity-45"
                      >
                        -
                      </button>
                      <span className="min-w-8 text-center text-sm font-black text-foreground">
                        {item.quantity}
                      </span>
                      <button
                        type="button"
                        aria-label="Aumentar cantidad"
                        disabled={pendingAction !== null}
                        onClick={() => handleQuantityChange(item.bookId, item.quantity + 1)}
                        className="h-full w-10 text-lg font-black text-foreground transition hover:bg-paper hover:text-accent disabled:cursor-not-allowed disabled:opacity-45"
                      >
                        +
                      </button>
                    </div>

                    <p className="min-w-[86px] text-right text-sm font-black text-foreground">
                      {formatPrice(item.lineTotal)}
                    </p>

                    <button
                      type="button"
                      disabled={pendingAction !== null}
                      onClick={() => handleQuantityChange(item.bookId, 0)}
                      className="h-10 rounded-full border border-[rgba(53,30,28,0.22)] bg-transparent px-4 text-[0.68rem] font-black text-muted transition hover:border-accent hover:text-accent disabled:cursor-not-allowed disabled:opacity-45"
                    >
                      Quitar
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
                  <span className="text-muted">Libros</span>
                  <span className="text-foreground">{cart.totalItems}</span>
                </div>
                <div className="flex justify-between gap-4">
                  <span className="text-muted">Subtotal</span>
                  <span className="text-foreground">{formatPrice(totals.subtotal)}</span>
                </div>
                <div className="flex justify-between gap-4">
                  <span className="text-muted">Impuestos</span>
                  <span className="text-foreground">{formatPrice(totals.tax)}</span>
                </div>
              </div>
              <div className="mt-5 flex items-end justify-between gap-4">
                <span className="text-sm font-black uppercase tracking-widest text-muted">
                  Total
                </span>
                <span className="text-2xl font-black text-foreground">
                  {formatPrice(totals.total)}
                </span>
              </div>

              <button
                type="button"
                disabled={pendingAction !== null || isEmpty}
                onClick={handleCheckout}
                className="mt-6 flex h-12 w-full items-center justify-center rounded-full bg-foreground px-6 text-sm font-black text-paper shadow-[0_10px_24px_rgba(53,30,28,0.18)] transition hover:bg-accent disabled:cursor-not-allowed disabled:opacity-50 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent"
              >
                {pendingAction === "checkout" ? "Confirmando..." : "Confirmar pedido"}
              </button>

              <button
                type="button"
                disabled={pendingAction !== null || isEmpty}
                onClick={handleClearCart}
                className="mt-3 flex h-11 w-full items-center justify-center rounded-full border border-[rgba(53,30,28,0.24)] bg-transparent px-6 text-[0.72rem] font-black text-foreground transition hover:border-accent hover:text-accent disabled:cursor-not-allowed disabled:opacity-50 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent"
              >
                Vaciar carrito
              </button>
            </aside>
          </section>
        )}
      </div>
    </main>
  );
}
