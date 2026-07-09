export const env = {
  apiBaseUrl: process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5218",
  browserApiBaseUrl: process.env.NEXT_PUBLIC_API_BROWSER_BASE_URL ?? "/backend-api",
} as const;
