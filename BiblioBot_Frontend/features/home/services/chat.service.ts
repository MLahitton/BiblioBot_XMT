import { API_ENDPOINTS } from "@/lib/api/endpoints";
import { apiPost } from "@/lib/api/api-client";
import { getStoredSession } from "@/features/auth/services/auth-storage";
import type {
  ChatbotContextBook,
  ChatbotPageContext,
  ChatbotResponse,
  SendChatMessageRequest,
  SendChatMessageResult,
} from "../types/chat.types";

export const BIBLIOBOT_CHAT_RESET_EVENT = "bibliobot:chat-reset";
export const CHAT_SESSION_STORAGE_KEY = "bibliobot_chat_session_id";
export const CHAT_MESSAGES_STORAGE_PREFIX = "bibliobot_chat_messages_";

const MAX_VISIBLE_CONTEXT_BOOKS = 10;
const MAX_CONTEXT_FILTERS = 8;

export function getOrCreateChatSessionId(): string {
  if (typeof window === "undefined") {
    return createChatSessionId();
  }

  const existingSessionId = window.localStorage.getItem(CHAT_SESSION_STORAGE_KEY);

  if (existingSessionId?.trim()) {
    return existingSessionId;
  }

  const sessionId = createChatSessionId();
  window.localStorage.setItem(CHAT_SESSION_STORAGE_KEY, sessionId);
  return sessionId;
}

export function resetChatSessionId(): string {
  const sessionId = createChatSessionId();

  if (typeof window !== "undefined") {
    window.localStorage.setItem(CHAT_SESSION_STORAGE_KEY, sessionId);
  }

  return sessionId;
}

export function getChatMessagesStorageKey(sessionId: string): string {
  return `${CHAT_MESSAGES_STORAGE_PREFIX}${sessionId}`;
}

export function clearBiblioBotChatSession(): void {
  if (typeof window === "undefined") return;

  const currentSessionId = window.localStorage.getItem(CHAT_SESSION_STORAGE_KEY);

  window.localStorage.removeItem(CHAT_SESSION_STORAGE_KEY);

  if (currentSessionId?.trim()) {
    window.localStorage.removeItem(getChatMessagesStorageKey(currentSessionId));
  }

  for (let index = window.localStorage.length - 1; index >= 0; index -= 1) {
    const key = window.localStorage.key(index);

    if (key?.startsWith(CHAT_MESSAGES_STORAGE_PREFIX)) {
      window.localStorage.removeItem(key);
    }
  }

  window.dispatchEvent(new Event(BIBLIOBOT_CHAT_RESET_EVENT));
}

export async function sendChatMessage(
  message: string,
  pageContext?: ChatbotPageContext | null,
): Promise<SendChatMessageResult> {
  const sessionId = getOrCreateChatSessionId();
  const session = getStoredSession();
  const token = session?.accessToken?.trim();
  const authenticated = Boolean(token);
  const endpoint = authenticated
    ? API_ENDPOINTS.chat.message
    : API_ENDPOINTS.chat.publicMessage;
  const payload = buildChatRequestPayload(sessionId, message, pageContext);

  try {
    const response = await apiPost<ChatbotResponse, SendChatMessageRequest>(
      endpoint,
      payload,
      authenticated ? { token } : {},
    );

    return {
      response,
      sessionId,
      authenticated,
    };
  } catch (error) {
    throw new Error(getFriendlyChatErrorMessage(error));
  }
}

export async function sendPublicMessage(
  message: string,
  pageContext?: ChatbotPageContext | null,
): Promise<SendChatMessageResult> {
  const sessionId = getOrCreateChatSessionId();
  const payload = buildChatRequestPayload(sessionId, message, pageContext);

  try {
    const response = await apiPost<ChatbotResponse, SendChatMessageRequest>(
      API_ENDPOINTS.chat.publicMessage,
      payload,
    );

    return {
      response,
      sessionId,
      authenticated: false,
    };
  } catch (error) {
    throw new Error(getFriendlyChatErrorMessage(error));
  }
}

function buildChatRequestPayload(
  sessionId: string,
  message: string,
  pageContext?: ChatbotPageContext | null,
): SendChatMessageRequest {
  const trimmedMessage = message.trim();

  if (!trimmedMessage) {
    throw new Error("El mensaje no puede estar vacio.");
  }

  const normalizedPageContext = normalizePageContext(pageContext);

  return {
    sessionId,
    message: trimmedMessage,
    ...(normalizedPageContext ? { pageContext: normalizedPageContext } : {}),
  };
}

function createChatSessionId(): string {
  if (typeof crypto !== "undefined" && "randomUUID" in crypto) {
    return `guest-${crypto.randomUUID()}`;
  }

  return `guest-${Date.now()}-${Math.random().toString(36).slice(2, 10)}`;
}

function normalizePageContext(pageContext?: ChatbotPageContext | null): ChatbotPageContext | undefined {
  if (!pageContext) return undefined;

  const activeFilters = normalizeActiveFilters(pageContext.activeFilters);
  const visibleBooks = (pageContext.visibleBooks ?? [])
    .map(normalizeContextBook)
    .filter((book): book is ChatbotContextBook => Boolean(book))
    .slice(0, MAX_VISIBLE_CONTEXT_BOOKS);
  const selectedBook = normalizeContextBook(pageContext.selectedBook);

  const normalized: ChatbotPageContext = {};
  const route = normalizeOptionalText(pageContext.route);
  const pageTitle = normalizeOptionalText(pageContext.pageTitle);
  const searchQuery = normalizeOptionalText(pageContext.searchQuery);
  const activeCategory = normalizeOptionalText(pageContext.activeCategory);

  if (route) normalized.route = route;
  if (pageTitle) normalized.pageTitle = pageTitle;
  if (searchQuery) normalized.searchQuery = searchQuery;
  if (activeCategory) normalized.activeCategory = activeCategory;
  if (Object.keys(activeFilters).length > 0) normalized.activeFilters = activeFilters;
  if (visibleBooks.length > 0) normalized.visibleBooks = visibleBooks;
  if (selectedBook) normalized.selectedBook = selectedBook;
  if (pageContext.cartSummary) {
    normalized.cartSummary = {
      itemCount: normalizeOptionalNumber(pageContext.cartSummary.itemCount),
      totalItems: normalizeOptionalNumber(pageContext.cartSummary.totalItems),
      subtotal: normalizeOptionalNumber(pageContext.cartSummary.subtotal),
    };
  }

  return Object.keys(normalized).length > 0 ? normalized : undefined;
}

function normalizeActiveFilters(filters?: Record<string, string> | null): Record<string, string> {
  if (!filters) return {};

  return Object.entries(filters)
    .slice(0, MAX_CONTEXT_FILTERS)
    .reduce<Record<string, string>>((accumulator, [key, value]) => {
      const normalizedKey = normalizeOptionalText(key);
      const normalizedValue = normalizeOptionalText(value);

      if (normalizedKey && normalizedValue) {
        accumulator[normalizedKey] = normalizedValue;
      }

      return accumulator;
    }, {});
}

function normalizeContextBook(book?: ChatbotContextBook | null): ChatbotContextBook | null {
  if (!book) return null;

  const id = normalizeOptionalText(book.id);
  const title = normalizeOptionalText(book.title);

  if (!id && !title) return null;

  return {
    ...(id ? { id } : {}),
    ...(title ? { title } : {}),
    authors: (book.authors ?? []).map(normalizeOptionalText).filter((value): value is string => Boolean(value)),
    categories: (book.categories ?? []).map(normalizeOptionalText).filter((value): value is string => Boolean(value)),
    price: normalizeOptionalNumber(book.price),
    available: typeof book.available === "boolean" ? book.available : null,
  };
}

function normalizeOptionalText(value: unknown): string | undefined {
  if (typeof value !== "string") return undefined;

  const trimmed = value.trim();
  return trimmed || undefined;
}

function normalizeOptionalNumber(value: unknown): number | null {
  if (typeof value !== "number" || !Number.isFinite(value)) return null;
  return value;
}

function getFriendlyChatErrorMessage(error: unknown): string {
  const rawMessage = error instanceof Error ? error.message : "";
  const statusMatch = rawMessage.match(/status\s+(\d{3})/i);
  const status = statusMatch?.[1];

  if (status === "400") {
    return "No pude procesar ese mensaje. Intenta reformularlo con un poco mas de detalle.";
  }

  if (status === "401") {
    return "Tu sesion expiro. Inicia sesion nuevamente para continuar.";
  }

  if (status === "502" || status === "504") {
    return "BiblioBot esta tardando en responder. Intenta de nuevo en unos segundos.";
  }

  if (status === "500") {
    return "BiblioBot tuvo un problema interno. Intenta nuevamente en un momento.";
  }

  if (rawMessage.toLowerCase().includes("sesion")) {
    return rawMessage;
  }

  return "No pude contactar a BiblioBot en este momento. Revisa que el backend este activo e intenta de nuevo.";
}
