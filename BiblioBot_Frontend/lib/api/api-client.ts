import { env } from "@/config/env";

type ApiClientOptions = RequestInit & {
  baseUrl?: string;
  token?: string | null;
  query?: Record<string, string | number | boolean | null | undefined>;
};

function buildApiUrl(endpoint: string, baseUrl: string): string {
  const usesProxyBase = baseUrl.includes("/backend-api");
  const normalizedEndpoint = usesProxyBase && endpoint.startsWith("/api/")
    ? endpoint.slice(4)
    : endpoint;
  return `${baseUrl.replace(/\/$/, "")}${normalizedEndpoint.startsWith("/") ? "" : "/"}${normalizedEndpoint}`;
}

function resolveBaseUrl(options: ApiClientOptions): string {
  return options.baseUrl ?? (typeof window === "undefined" ? env.apiBaseUrl : env.browserApiBaseUrl);
}

function appendQuery(url: string, query?: ApiClientOptions["query"]): string {
  if (!query) return url;

  const params = new URLSearchParams();
  Object.entries(query).forEach(([key, value]) => {
    if (value !== undefined && value !== null && value !== "") {
      params.set(key, String(value));
    }
  });

  const queryString = params.toString();
  return queryString ? `${url}?${queryString}` : url;
}

function buildHeaders(options: ApiClientOptions): HeadersInit {
  const headers = new Headers(options.headers);
  headers.set("Accept", "application/json");

  if (options.body && !headers.has("Content-Type")) {
    headers.set("Content-Type", "application/json");
  }

  if (options.token) {
    headers.set("Authorization", `Bearer ${options.token}`);
  }

  return headers;
}

async function parseApiError(response: Response): Promise<Error> {
  try {
    const payload = (await response.json()) as { message?: string; title?: string };
    return new Error(payload.message ?? payload.title ?? `API request failed with status ${response.status}.`);
  } catch {
    return new Error(`API request failed with status ${response.status}.`);
  }
}

async function apiRequest<TResponse>(
  endpoint: string,
  options: ApiClientOptions = {},
): Promise<TResponse> {
  const baseUrl = resolveBaseUrl(options);

  if (!baseUrl) {
    throw new Error("API base URL is not configured.");
  }

  const { baseUrl: _baseUrl, query, token, ...requestOptions } = options;
  void _baseUrl;
  void token;
  const response = await fetch(appendQuery(buildApiUrl(endpoint, baseUrl), query), {
    ...requestOptions,
    headers: buildHeaders(options),
    cache: requestOptions.cache ?? "no-store",
  });

  if (!response.ok) {
    throw await parseApiError(response);
  }

  if (response.status === 204) {
    return undefined as TResponse;
  }

  return response.json() as Promise<TResponse>;
}

export async function apiGet<TResponse>(
  endpoint: string,
  options: ApiClientOptions = {},
): Promise<TResponse> {
  return apiRequest<TResponse>(endpoint, {
    ...options,
    method: "GET",
  });
}

export async function apiPost<TResponse, TBody = unknown>(
  endpoint: string,
  body: TBody,
  options: ApiClientOptions = {},
): Promise<TResponse> {
  return apiRequest<TResponse>(endpoint, {
    ...options,
    method: "POST",
    body: JSON.stringify(body),
  });
}

export async function apiDelete<TResponse>(
  endpoint: string,
  options: ApiClientOptions = {},
): Promise<TResponse> {
  return apiRequest<TResponse>(endpoint, {
    ...options,
    method: "DELETE",
  });
}
