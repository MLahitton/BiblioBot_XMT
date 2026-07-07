import type { AuthSession } from "../types/auth.types";

const SESSION_STORAGE_KEY = "bibliobot.auth.session";

export function getStoredSession(): AuthSession | null {
  if (typeof window === "undefined") return null;

  const rawSession = window.localStorage.getItem(SESSION_STORAGE_KEY);
  if (!rawSession) return null;

  try {
    return JSON.parse(rawSession) as AuthSession;
  } catch {
    window.localStorage.removeItem(SESSION_STORAGE_KEY);
    return null;
  }
}

export function storeSession(session: AuthSession): void {
  if (typeof window === "undefined") return;
  window.localStorage.setItem(SESSION_STORAGE_KEY, JSON.stringify(session));
}

export function clearStoredSession(): void {
  if (typeof window === "undefined") return;
  window.localStorage.removeItem(SESSION_STORAGE_KEY);
}
