import type { AuthUser } from "@/features/auth/types/auth.types";

export const ADMIN_EMAIL = "admin@gmail.com";

export function isAdminAccount(user: AuthUser | null): boolean {
  return user?.email.trim().toLowerCase() === ADMIN_EMAIL;
}
