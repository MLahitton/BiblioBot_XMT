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
const simulatedPaymentDelayMs = 850;

type PaymentMethod = "card" | "transfer" | "cash";

type PaymentForm = {
  cardholderName: string;
  cardNumber: string;
  cardExpiry: string;
  cardCvv: string;
  transferBank: string;
};

type PaymentErrors = Partial<Record<keyof PaymentForm, string>>;

const paymentOptions: Array<{
  id: PaymentMethod;
  label: string;
  detail: string;
}> = [
  {
    id: "card",
    label: "Tarjeta",
    detail: "Credito o debito",
  },
  {
    id: "transfer",
    label: "Transferencia",
    detail: "PSE simulado",
  },
  {
    id: "cash",
    label: "Contra entrega",
    detail: "Pago al recibir",
  },
];

const paymentMethodLabels: Record<PaymentMethod, string> = {
  card: "tarjeta",
  transfer: "transferencia",
  cash: "contra entrega",
};

const transferBanks = [
  "Bancolombia",
  "Davivienda",
  "Banco de Bogota",
  "Nequi",
];

function formatPrice(value: number) {
  return priceFormatter.format(value);
}

function getDigits(value: string) {
  return value.replace(/\D/g, "");
}

function formatCardNumber(value: string) {
  return getDigits(value).slice(0, 19).replace(/(.{4})/g, "$1 ").trim();
}

function formatExpiry(value: string) {
  const digits = getDigits(value).slice(0, 4);
  if (digits.length <= 2) return digits;
  return `${digits.slice(0, 2)}/${digits.slice(2)}`;
}

function isExpiryValid(value: string) {
  const match = /^(\d{2})\/(\d{2})$/.exec(value);

  if (!match) return false;

  const month = Number(match[1]);
  const year = 2000 + Number(match[2]);

  if (month < 1 || month > 12) return false;

  const now = new Date();
  const firstDayOfCurrentMonth = new Date(now.getFullYear(), now.getMonth(), 1);
  const firstDayAfterExpiryMonth = new Date(year, month, 1);

  return firstDayAfterExpiryMonth > firstDayOfCurrentMonth;
}

function waitForSimulatedPayment() {
  return new Promise((resolve) => {
    window.setTimeout(resolve, simulatedPaymentDelayMs);
  });
}

function validatePayment(method: PaymentMethod, form: PaymentForm): PaymentErrors {
  const errors: PaymentErrors = {};

  if (method === "card") {
    if (!form.cardholderName.trim()) {
      errors.cardholderName = "No se ha llenado ese campo.";
    }

    const cardDigits = getDigits(form.cardNumber);
    if (!cardDigits) {
      errors.cardNumber = "No se ha llenado ese campo.";
    } else if (cardDigits.length < 13) {
      errors.cardNumber = "Ingresa un numero de tarjeta valido.";
    }

    if (!form.cardExpiry.trim()) {
      errors.cardExpiry = "No se ha llenado ese campo.";
    } else if (!isExpiryValid(form.cardExpiry)) {
      errors.cardExpiry = "Ingresa una fecha valida.";
    }

    const cvvDigits = getDigits(form.cardCvv);
    if (!cvvDigits) {
      errors.cardCvv = "No se ha llenado ese campo.";
    } else if (cvvDigits.length < 3) {
      errors.cardCvv = "Ingresa un codigo valido.";
    }
  }

  if (method === "transfer" && !form.transferBank) {
    errors.transferBank = "No se ha llenado ese campo.";
  }

  return errors;
}

export function CartPage() {
  const [cart, setCart] = useState<Cart>(() => getEmptyCart());
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [successSale, setSuccessSale] = useState<Sale | null>(null);
  const [pendingAction, setPendingAction] = useState<string | null>(null);
  const [paymentMethod, setPaymentMethod] = useState<PaymentMethod>("card");
  const [paymentForm, setPaymentForm] = useState<PaymentForm>({
    cardholderName: "",
    cardNumber: "",
    cardExpiry: "",
    cardCvv: "",
    transferBank: "",
  });
  const [paymentErrors, setPaymentErrors] = useState<PaymentErrors>({});
  const [approvedPaymentMethod, setApprovedPaymentMethod] =
    useState<PaymentMethod | null>(null);
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

  const handlePaymentMethodChange = (method: PaymentMethod) => {
    setPaymentMethod(method);
    setPaymentErrors({});
    setSuccessSale(null);
  };

  const updatePaymentField = (field: keyof PaymentForm, value: string) => {
    const nextValue = (() => {
      if (field === "cardNumber") return formatCardNumber(value);
      if (field === "cardExpiry") return formatExpiry(value);
      if (field === "cardCvv") return getDigits(value).slice(0, 4);
      return value;
    })();

    setPaymentForm((currentForm) => ({
      ...currentForm,
      [field]: nextValue,
    }));
    setPaymentErrors((currentErrors) => ({
      ...currentErrors,
      [field]: undefined,
    }));
    setSuccessSale(null);
  };

  const handleCheckout = async () => {
    const nextPaymentErrors = validatePayment(paymentMethod, paymentForm);

    if (Object.keys(nextPaymentErrors).length > 0) {
      setPaymentErrors(nextPaymentErrors);
      return;
    }

    setPendingAction("checkout");
    setError(null);
    setSuccessSale(null);

    try {
      await waitForSimulatedPayment();
      const sale = await createSaleFromCart();
      setSuccessSale(sale);
      setApprovedPaymentMethod(paymentMethod);

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
      <main className="min-h-screen bg-background px-5 pb-16 pt-36 text-foreground sm:px-8 md:pt-24 lg:px-12">
        <section className="mx-auto max-w-6xl border-y border-border/70 bg-paper/55 py-14">
          <p className="text-center text-sm font-black uppercase tracking-widest text-muted">
            Cargando carrito
          </p>
        </section>
      </main>
    );
  }

  return (
    <main className="min-h-screen bg-background px-5 pb-16 pt-36 text-foreground sm:px-8 md:pt-24 lg:px-12">
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
            Pago simulado aprobado por {approvedPaymentMethod ? paymentMethodLabels[approvedPaymentMethod] : "el metodo seleccionado"}.
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

              <div className="mt-6 border-y border-border/70 py-5">
                <div className="flex items-center justify-between gap-4">
                  <h3 className="text-sm font-black text-foreground">
                    Forma de pago
                  </h3>
                  <span className="rounded-full border border-[rgba(53,30,28,0.18)] bg-[#f8efe9] px-3 py-1 text-[0.62rem] font-black uppercase tracking-widest text-muted">
                    Simulado
                  </span>
                </div>

                <div className="mt-4 grid gap-2">
                  {paymentOptions.map((option) => {
                    const isSelected = paymentMethod === option.id;

                    return (
                      <button
                        key={option.id}
                        type="button"
                        disabled={pendingAction !== null}
                        onClick={() => handlePaymentMethodChange(option.id)}
                        className={`flex min-h-14 items-center justify-between gap-3 rounded-[14px] border px-4 text-left transition focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent disabled:cursor-not-allowed disabled:opacity-50 ${
                          isSelected
                            ? "border-foreground bg-[#f8efe9] shadow-[0_8px_18px_rgba(53,30,28,0.08)]"
                            : "border-[rgba(53,30,28,0.18)] bg-transparent hover:border-[rgba(53,30,28,0.34)]"
                        }`}
                        aria-pressed={isSelected}
                      >
                        <span>
                          <span className="block text-sm font-black text-foreground">
                            {option.label}
                          </span>
                          <span className="mt-0.5 block text-[0.68rem] font-bold uppercase tracking-widest text-muted">
                            {option.detail}
                          </span>
                        </span>
                        <span
                          aria-hidden="true"
                          className={`flex h-5 w-5 items-center justify-center rounded-full border ${
                            isSelected
                              ? "border-foreground bg-foreground"
                              : "border-[rgba(53,30,28,0.28)]"
                          }`}
                        >
                          {isSelected && (
                            <span className="h-2 w-2 rounded-full bg-paper" />
                          )}
                        </span>
                      </button>
                    );
                  })}
                </div>

                {paymentMethod === "card" && (
                  <div className="mt-4 space-y-3">
                    <label className="block">
                      <span className="text-[0.68rem] font-black uppercase tracking-widest text-muted">
                        Titular
                      </span>
                      <input
                        value={paymentForm.cardholderName}
                        onChange={(event) =>
                          updatePaymentField("cardholderName", event.target.value)
                        }
                        disabled={pendingAction !== null}
                        aria-invalid={Boolean(paymentErrors.cardholderName)}
                        className="mt-1 h-11 w-full rounded-[14px] border border-[rgba(53,30,28,0.2)] bg-paper px-4 text-sm font-bold text-foreground outline-none transition placeholder:text-muted/55 focus:border-accent focus:ring-2 focus:ring-accent/20 disabled:cursor-not-allowed disabled:opacity-55"
                        placeholder="Nombre en la tarjeta"
                      />
                      {paymentErrors.cardholderName && (
                        <span className="mt-1 block text-xs font-bold text-red-600">
                          {paymentErrors.cardholderName}
                        </span>
                      )}
                    </label>

                    <label className="block">
                      <span className="text-[0.68rem] font-black uppercase tracking-widest text-muted">
                        Numero
                      </span>
                      <input
                        value={paymentForm.cardNumber}
                        onChange={(event) =>
                          updatePaymentField("cardNumber", event.target.value)
                        }
                        disabled={pendingAction !== null}
                        inputMode="numeric"
                        aria-invalid={Boolean(paymentErrors.cardNumber)}
                        className="mt-1 h-11 w-full rounded-[14px] border border-[rgba(53,30,28,0.2)] bg-paper px-4 text-sm font-bold text-foreground outline-none transition placeholder:text-muted/55 focus:border-accent focus:ring-2 focus:ring-accent/20 disabled:cursor-not-allowed disabled:opacity-55"
                        placeholder="4242 4242 4242 4242"
                      />
                      {paymentErrors.cardNumber && (
                        <span className="mt-1 block text-xs font-bold text-red-600">
                          {paymentErrors.cardNumber}
                        </span>
                      )}
                    </label>

                    <div className="grid grid-cols-2 gap-3">
                      <label className="block">
                        <span className="text-[0.68rem] font-black uppercase tracking-widest text-muted">
                          Vence
                        </span>
                        <input
                          value={paymentForm.cardExpiry}
                          onChange={(event) =>
                            updatePaymentField("cardExpiry", event.target.value)
                          }
                          disabled={pendingAction !== null}
                          inputMode="numeric"
                          aria-invalid={Boolean(paymentErrors.cardExpiry)}
                          className="mt-1 h-11 w-full rounded-[14px] border border-[rgba(53,30,28,0.2)] bg-paper px-4 text-sm font-bold text-foreground outline-none transition placeholder:text-muted/55 focus:border-accent focus:ring-2 focus:ring-accent/20 disabled:cursor-not-allowed disabled:opacity-55"
                          placeholder="MM/AA"
                        />
                        {paymentErrors.cardExpiry && (
                          <span className="mt-1 block text-xs font-bold text-red-600">
                            {paymentErrors.cardExpiry}
                          </span>
                        )}
                      </label>

                      <label className="block">
                        <span className="text-[0.68rem] font-black uppercase tracking-widest text-muted">
                          CVV
                        </span>
                        <input
                          value={paymentForm.cardCvv}
                          onChange={(event) =>
                            updatePaymentField("cardCvv", event.target.value)
                          }
                          disabled={pendingAction !== null}
                          inputMode="numeric"
                          aria-invalid={Boolean(paymentErrors.cardCvv)}
                          className="mt-1 h-11 w-full rounded-[14px] border border-[rgba(53,30,28,0.2)] bg-paper px-4 text-sm font-bold text-foreground outline-none transition placeholder:text-muted/55 focus:border-accent focus:ring-2 focus:ring-accent/20 disabled:cursor-not-allowed disabled:opacity-55"
                          placeholder="123"
                        />
                        {paymentErrors.cardCvv && (
                          <span className="mt-1 block text-xs font-bold text-red-600">
                            {paymentErrors.cardCvv}
                          </span>
                        )}
                      </label>
                    </div>
                  </div>
                )}

                {paymentMethod === "transfer" && (
                  <label className="mt-4 block">
                    <span className="text-[0.68rem] font-black uppercase tracking-widest text-muted">
                      Banco
                    </span>
                    <select
                      value={paymentForm.transferBank}
                      onChange={(event) =>
                        updatePaymentField("transferBank", event.target.value)
                      }
                      disabled={pendingAction !== null}
                      aria-invalid={Boolean(paymentErrors.transferBank)}
                      className="mt-1 h-11 w-full rounded-[14px] border border-[rgba(53,30,28,0.2)] bg-paper px-4 text-sm font-bold text-foreground outline-none transition focus:border-accent focus:ring-2 focus:ring-accent/20 disabled:cursor-not-allowed disabled:opacity-55"
                    >
                      <option value="">Selecciona un banco</option>
                      {transferBanks.map((bank) => (
                        <option key={bank} value={bank}>
                          {bank}
                        </option>
                      ))}
                    </select>
                    {paymentErrors.transferBank && (
                      <span className="mt-1 block text-xs font-bold text-red-600">
                        {paymentErrors.transferBank}
                      </span>
                    )}
                  </label>
                )}

                {paymentMethod === "cash" && (
                  <div className="mt-4 rounded-[14px] border border-[rgba(53,30,28,0.16)] bg-paper px-4 py-3">
                    <p className="text-sm font-bold leading-5 text-muted">
                      Se registrara el pedido y el pago quedara marcado como simulado para entrega.
                    </p>
                  </div>
                )}
              </div>

              <button
                type="button"
                disabled={pendingAction !== null || isEmpty}
                onClick={handleCheckout}
                className="mt-6 flex h-12 w-full items-center justify-center rounded-full bg-foreground px-6 text-sm font-black text-paper shadow-[0_10px_24px_rgba(53,30,28,0.18)] transition hover:bg-accent disabled:cursor-not-allowed disabled:opacity-50 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent"
              >
                {pendingAction === "checkout" ? "Procesando pago..." : "Confirmar pedido"}
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
