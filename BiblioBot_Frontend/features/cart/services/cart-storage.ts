const CART_SESSION_KEY = "bibliobot.cart.session";

function createSessionId(): string {
  if (typeof crypto !== "undefined" && "randomUUID" in crypto) {
    return crypto.randomUUID();
  }

  return `cart-${Date.now()}-${Math.random().toString(16).slice(2)}`;
}

export function getCartSessionId(): string {
  if (typeof window === "undefined") return "";

  const currentSessionId = window.localStorage.getItem(CART_SESSION_KEY);
  if (currentSessionId) return currentSessionId;

  const sessionId = createSessionId();
  window.localStorage.setItem(CART_SESSION_KEY, sessionId);
  return sessionId;
}

export function resetCartSessionId(): string {
  if (typeof window === "undefined") return "";

  const sessionId = createSessionId();
  window.localStorage.setItem(CART_SESSION_KEY, sessionId);
  return sessionId;
}
