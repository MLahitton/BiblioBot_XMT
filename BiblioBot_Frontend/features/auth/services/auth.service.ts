import { API_ENDPOINTS } from "@/lib/api/endpoints";
import { apiGet, apiPost } from "@/lib/api/api-client";
import {
  clearStoredSession,
  getStoredSession,
  storeSession,
} from "./auth-storage";
import type {
  AuthSession,
  AuthUser,
  LoginPayload,
  RegisterPayload,
} from "../types/auth.types";

export async function login(payload: LoginPayload): Promise<AuthSession> {
  const session = await apiPost<AuthSession, LoginPayload>(
    API_ENDPOINTS.auth.login,
    payload,
  );
  storeSession(session);
  return session;
}

export async function register(payload: RegisterPayload): Promise<AuthSession> {
  const session = await apiPost<AuthSession, RegisterPayload>(
    API_ENDPOINTS.auth.register,
    payload,
  );
  storeSession(session);
  return session;
}

export async function refreshSession(): Promise<AuthSession | null> {
  const currentSession = getStoredSession();
  if (!currentSession?.refreshToken) return null;

  const session = await apiPost<AuthSession, { refreshToken: string }>(
    API_ENDPOINTS.auth.refresh,
    { refreshToken: currentSession.refreshToken },
  );
  storeSession(session);
  return session;
}

export async function getCurrentUser(): Promise<AuthUser | null> {
  const currentSession = getStoredSession();
  if (!currentSession?.accessToken) return null;

  try {
    return await apiGet<AuthUser>(API_ENDPOINTS.auth.me, {
      token: currentSession.accessToken,
    });
  } catch {
    clearStoredSession();
    return null;
  }
}

export function logout(): void {
  clearStoredSession();
}
