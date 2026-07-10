import { API_ENDPOINTS } from "@/lib/api/endpoints";
import { apiPost } from "@/lib/api/api-client";
import { getStoredSession } from "@/features/auth/services/auth-storage";
import type { ChatbotResponse, SendChatMessageResult } from "../types/chat.types";

const CHAT_SESSION_STORAGE_KEY = "bibliobot_chat_session_id";

type ChatRequestBody = {
  sessionId: string;
  message: string;
};

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

export async function sendChatMessage(message: string): Promise<SendChatMessageResult> {
  const sessionId = getOrCreateChatSessionId();
  const session = getStoredSession();
  const token = session?.accessToken?.trim();
  const authenticated = Boolean(token);
  const endpoint = authenticated
    ? API_ENDPOINTS.chat.message
    : API_ENDPOINTS.chat.publicMessage;

  try {
    const response = await apiPost<ChatbotResponse, ChatRequestBody>(
      endpoint,
      {
        sessionId,
        message,
      },
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

function createChatSessionId(): string {
  if (typeof crypto !== "undefined" && "randomUUID" in crypto) {
    return `guest-${crypto.randomUUID()}`;
  }

  return `guest-${Date.now()}-${Math.random().toString(36).slice(2, 10)}`;
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
