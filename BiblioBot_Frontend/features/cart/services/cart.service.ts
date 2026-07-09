import { API_ENDPOINTS } from "@/lib/api/endpoints";
import { apiDelete, apiGet, apiPost } from "@/lib/api/api-client";
import { getStoredSession } from "@/features/auth/services/auth-storage";
import type {
  AddOrUpdateCartItemPayload,
  Cart,
  Sale,
} from "../types/cart.types";
import { getCartSessionId, resetCartSessionId } from "./cart-storage";

export const CART_UPDATED_EVENT = "bibliobot:cart-updated";

export class CartAuthError extends Error {
  constructor() {
    super("Debes iniciar sesion para usar el carrito.");
    this.name = "CartAuthError";
  }
}

function getAccessToken(): string {
  const token = getStoredSession()?.accessToken;

  if (!token) {
    throw new CartAuthError();
  }

  return token;
}

function emptyCart(sessionId = ""): Cart {
  return {
    sessionId,
    status: "ACTIVE",
    items: [],
    totalItems: 0,
    subtotal: 0,
  };
}

function notifyCartUpdated(cart: Cart): void {
  if (typeof window === "undefined") return;
  window.dispatchEvent(new CustomEvent<Cart>(CART_UPDATED_EVENT, { detail: cart }));
}

export function getEmptyCart(): Cart {
  return emptyCart(typeof window === "undefined" ? "" : getCartSessionId());
}

export async function getCurrentCart(): Promise<Cart> {
  const sessionId = getCartSessionId();
  const token = getAccessToken();

  const cart = await apiGet<Cart>(API_ENDPOINTS.cart.bySession(sessionId), {
    token,
  });

  return cart ?? emptyCart(sessionId);
}

export async function addOrUpdateCartItem(
  payload: Omit<AddOrUpdateCartItemPayload, "sessionId">,
): Promise<Cart> {
  const sessionId = getCartSessionId();
  const token = getAccessToken();

  const cart = await apiPost<Cart, AddOrUpdateCartItemPayload>(
    API_ENDPOINTS.cart.items,
    {
      sessionId,
      ...payload,
    },
    { token },
  );

  notifyCartUpdated(cart);
  return cart;
}

export async function addBookToCart(
  bookId: string,
  maxQuantity?: number,
): Promise<Cart> {
  const cart = await getCurrentCart();
  const currentItem = cart.items.find((item) => item.bookId === bookId);
  const nextQuantity = (currentItem?.quantity ?? 0) + 1;
  const quantity = maxQuantity ? Math.min(nextQuantity, maxQuantity) : nextQuantity;

  return addOrUpdateCartItem({
    bookId,
    quantity,
  });
}

export async function removeCartItem(bookId: string): Promise<Cart> {
  const sessionId = getCartSessionId();
  const token = getAccessToken();

  const cart = await apiDelete<Cart>(API_ENDPOINTS.cart.item(sessionId, bookId), {
    token,
  });

  notifyCartUpdated(cart);
  return cart;
}

export async function clearCart(): Promise<Cart> {
  const sessionId = getCartSessionId();
  const token = getAccessToken();

  const cart = await apiDelete<Cart>(API_ENDPOINTS.cart.clear(sessionId), {
    token,
  });

  resetCartSessionId();
  notifyCartUpdated(emptyCart(getCartSessionId()));
  return cart;
}

export async function createSaleFromCart(): Promise<Sale> {
  const sessionId = getCartSessionId();
  const token = getAccessToken();

  return apiPost<Sale, { sessionId: string; originCode: string }>(
    API_ENDPOINTS.sales,
    {
      sessionId,
      originCode: "WEB_UI",
    },
    { token },
  );
}
