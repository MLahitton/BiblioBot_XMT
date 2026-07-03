import { env } from "@/config/env";

type ApiClientOptions = RequestInit & {
  baseUrl?: string;
};

function buildApiUrl(endpoint: string, baseUrl: string): string {
  return `${baseUrl.replace(/\/$/, "")}${endpoint}`;
}

export async function apiGet<TResponse>(
  endpoint: string,
  options: ApiClientOptions = {},
): Promise<TResponse> {
  const baseUrl = options.baseUrl ?? env.apiBaseUrl;

  if (!baseUrl) {
    throw new Error("API base URL is not configured.");
  }

  const response = await fetch(buildApiUrl(endpoint, baseUrl), {
    ...options,
    method: "GET",
  });

  if (!response.ok) {
    throw new Error(`API request failed with status ${response.status}.`);
  }

  return response.json() as Promise<TResponse>;
}
