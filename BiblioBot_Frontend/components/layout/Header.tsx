"use client";

import Image from "next/image";
import Link from "next/link";
import { FormEvent, useEffect, useRef, useState } from "react";
import { siteConfig } from "@/config/site";
import { routes } from "@/constants/routes";
import type { AuthUser } from "@/features/auth/types/auth.types";
import { getStoredSession } from "@/features/auth/services/auth-storage";
import { logout } from "@/features/auth/services/auth.service";
import { CART_UPDATED_EVENT, getCurrentCart } from "@/features/cart/services/cart.service";
import { getCategories } from "@/features/categories/services/categories.service";
import type { Category } from "@/features/categories/types/category.types";
import {
  FAVORITES_UPDATED_EVENT,
  getFavoriteCount,
} from "@/features/favorites/services/favorites.service";
import { useChatContext } from "@/features/home/components/ChatContext";

const visibleNavHrefs: string[] = [
  routes.home,
  routes.categories,
  routes.featured,
];

const navItems = siteConfig.navItems.filter((item) =>
  visibleNavHrefs.includes(item.href),
);

function ChevronIcon({ isOpen }: { isOpen: boolean }) {
  return (
    <svg
      aria-hidden="true"
      className={`h-3.5 w-3.5 transition ${isOpen ? "rotate-180" : ""}`}
      fill="none"
      viewBox="0 0 24 24"
    >
      <path
        d="m6.75 9.75 5.25 5.25 5.25-5.25"
        stroke="currentColor"
        strokeLinecap="round"
        strokeLinejoin="round"
        strokeWidth="2.4"
      />
    </svg>
  );
}

function HeartIcon({ isFilled = false }: { isFilled?: boolean }) {
  return (
    <svg
      aria-hidden="true"
      className="h-[18px] w-[18px]"
      fill={isFilled ? "currentColor" : "none"}
      viewBox="0 0 24 24"
    >
      <path
        d="M20.84 4.61a5.5 5.5 0 0 0-7.78 0L12 5.67l-1.06-1.06a5.5 5.5 0 0 0-7.78 7.78L12 21.23l8.84-8.84a5.5 5.5 0 0 0 0-7.78Z"
        stroke="currentColor"
        strokeLinecap="round"
        strokeLinejoin="round"
        strokeWidth="2.1"
      />
    </svg>
  );
}

function getUserInitials(user: AuthUser) {
  const source = user.fullName.trim() || user.email.trim();
  const parts = source.split(/\s+/).filter(Boolean);

  if (parts.length === 0) {
    return "U";
  }

  const first = parts[0]?.[0] ?? "";
  const last = parts.length > 1 ? parts[parts.length - 1]?.[0] ?? "" : "";

  return `${first}${last}`.toUpperCase();
}

export function Header() {
  const [isCategoryMenuOpen, setIsCategoryMenuOpen] = useState(false);
  const [isProfileMenuOpen, setIsProfileMenuOpen] = useState(false);
  const [searchTerm, setSearchTerm] = useState("");
  const [categories, setCategories] = useState<Category[]>([]);
  const [user, setUser] = useState<AuthUser | null>(null);
  const [cartTotal, setCartTotal] = useState(0);
  const [favoriteTotal, setFavoriteTotal] = useState(0);
  const categoryMenuRef = useRef<HTMLDivElement | null>(null);
  const profileMenuRef = useRef<HTMLDivElement | null>(null);
  const { isChatExpanded } = useChatContext();

  useEffect(() => {
    const syncSession = () => {
      const session = getStoredSession();
      setUser(session?.user ?? null);
      setFavoriteTotal(getFavoriteCount());

      if (session?.accessToken) {
        getCurrentCart()
          .then((cart) => setCartTotal(cart.totalItems))
          .catch(() => setCartTotal(0));
      } else {
        setCartTotal(0);
      }
    };

    window.setTimeout(syncSession, 0);

    getCategories()
      .then((categories) => setCategories(categories.slice(0, 8)))
      .catch(() => setCategories([]));

    const handlePointerDown = (event: PointerEvent) => {
      if (
        categoryMenuRef.current &&
        !categoryMenuRef.current.contains(event.target as Node)
      ) {
        setIsCategoryMenuOpen(false);
      }

      if (
        profileMenuRef.current &&
        !profileMenuRef.current.contains(event.target as Node)
      ) {
        setIsProfileMenuOpen(false);
      }
    };

    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        setIsCategoryMenuOpen(false);
        setIsProfileMenuOpen(false);
      }
    };

    const handleCartUpdated = (event: Event) => {
      const cart = (event as CustomEvent<{ totalItems?: number }>).detail;
      setCartTotal(cart?.totalItems ?? 0);
    };

    const handleFavoritesUpdated = (event: Event) => {
      const favorites = (event as CustomEvent<unknown[]>).detail;
      setFavoriteTotal(Array.isArray(favorites) ? favorites.length : getFavoriteCount());
    };

    document.addEventListener("pointerdown", handlePointerDown);
    window.addEventListener("keydown", handleKeyDown);
    window.addEventListener(CART_UPDATED_EVENT, handleCartUpdated);
    window.addEventListener(FAVORITES_UPDATED_EVENT, handleFavoritesUpdated);

    return () => {
      document.removeEventListener("pointerdown", handlePointerDown);
      window.removeEventListener("keydown", handleKeyDown);
      window.removeEventListener(CART_UPDATED_EVENT, handleCartUpdated);
      window.removeEventListener(FAVORITES_UPDATED_EVENT, handleFavoritesUpdated);
    };
  }, []);

  const handleSearchSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const query = searchTerm.trim();
    const params = query ? `?q=${encodeURIComponent(query)}` : "";
    window.location.href = `/search${params}`;
  };

  const handleLogout = () => {
    logout();
    setUser(null);
    setCartTotal(0);
    setIsProfileMenuOpen(false);
    window.location.href = "/";
  };

  return (
    <header
      className={`fixed left-0 right-0 top-0 z-[100] border-b border-border/40 bg-paper/95 px-4 py-2 shadow-[0_10px_24px_rgba(53,30,28,0.08)] backdrop-blur-md transition-[right] duration-300 sm:px-6 lg:px-10 ${
        isChatExpanded ? "sm:right-[420px]" : ""
      }`}
    >
      <div className="mx-auto grid h-10 max-w-7xl grid-cols-[auto_minmax(140px,1fr)_auto] items-center gap-2 sm:grid-cols-[auto_minmax(200px,300px)_auto] sm:gap-4 md:grid-cols-[minmax(300px,1fr)_minmax(220px,300px)_minmax(260px,1fr)] md:gap-5">
        <div className="flex min-w-0 items-center justify-start gap-5">
          <Link
            href="/"
            className="flex h-9 w-[74px] shrink-0 items-center justify-start rounded-lg outline-none transition hover:-translate-y-0.5 focus-visible:ring-2 focus-visible:ring-accent sm:w-[88px]"
            aria-label="Webook inicio"
          >
            <Image
              src="/images/biblioBot/cutouts/Logo_Webook-cutout.png"
              alt="Webook"
              width={942}
              height={998}
              className="h-9 w-auto object-contain drop-shadow-[0_8px_12px_rgba(53,30,28,0.14)]"
              priority
            />
          </Link>

          <nav
            className="hidden min-w-0 items-center justify-start gap-6 md:flex"
            aria-label="Navegacion principal"
          >
            {navItems.map((item) =>
              item.href === routes.categories ? (
                <div key={item.href} ref={categoryMenuRef} className="relative">
                  <button
                    type="button"
                    className="flex h-8 items-center gap-1.5 rounded-full border border-[rgba(53,30,28,0.2)] px-3 text-[0.68rem] font-black text-foreground transition hover:border-accent hover:text-accent focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent"
                    aria-expanded={isCategoryMenuOpen}
                    aria-controls="category-menu"
                    onClick={() => {
                      setIsCategoryMenuOpen((isOpen) => !isOpen);
                      setIsProfileMenuOpen(false);
                    }}
                  >
                    Categorias
                    <ChevronIcon isOpen={isCategoryMenuOpen} />
                  </button>

                  <div
                    id="category-menu"
                    className={`absolute left-1/2 top-10 z-20 w-72 -translate-x-1/2 overflow-hidden rounded-[22px] border border-border bg-paper shadow-[0_24px_56px_rgba(53,30,28,0.18)] transition duration-200 ${
                      isCategoryMenuOpen
                        ? "pointer-events-auto translate-y-0 opacity-100"
                        : "pointer-events-none -translate-y-2 opacity-0"
                    }`}
                  >
                    <div className="border-b border-border/70 px-4 py-3">
                      <p className="text-[0.62rem] font-black uppercase tracking-widest text-muted">
                        Explorar por categoria
                      </p>
                    </div>
                    <div className="grid gap-1 p-2">
                      {categories.length > 0 ? categories.map((category) => (
                        <Link
                          key={category.id}
                          href={`/#${category.slug}`}
                          className="flex items-center gap-3 rounded-xl px-3 py-2.5 text-left transition hover:bg-card focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent"
                          onClick={() => setIsCategoryMenuOpen(false)}
                        >
                          <span className="flex h-8 w-8 shrink-0 items-center justify-center rounded-lg border border-border bg-background-soft">
                            <Image
                              src={category.icon}
                              alt=""
                              width={15}
                              height={15}
                              aria-hidden
                            />
                          </span>
                          <span className="min-w-0 flex-1">
                            <span className="block truncate text-sm font-black text-foreground">
                              {category.name}
                            </span>
                            <span className="block truncate text-xs font-semibold text-muted">
                              {category.totalBooks} libros
                            </span>
                          </span>
                        </Link>
                      )) : (
                        <span className="px-3 py-2.5 text-xs font-bold text-muted">
                          Sin categorias disponibles
                        </span>
                      )}
                    </div>
                  </div>
                </div>
              ) : (
                <a
                  key={item.href}
                  href={item.href}
                  className={`flex h-8 items-center rounded-full border border-[rgba(53,30,28,0.2)] px-3 text-[0.68rem] font-black transition hover:border-accent hover:text-accent focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent ${
                    item.href === routes.home ? "text-accent" : "text-foreground"
                  }`}
                >
                  {item.href === routes.home ? "Inicio" : "Destacados"}
                </a>
              ),
            )}
          </nav>
        </div>

        <div className="min-w-0 justify-self-stretch">
          <form
            className="min-w-0"
            role="search"
            onSubmit={handleSearchSubmit}
          >
            <label className="sr-only" htmlFor="site-search">
              Buscar libros
            </label>
            <div className="flex h-10 overflow-hidden rounded-full border border-[rgba(53,30,28,0.24)] bg-[#f8efe9] shadow-[inset_0_1px_0_rgba(255,255,255,0.65),0_6px_14px_rgba(53,30,28,0.06)] focus-within:border-accent focus-within:ring-2 focus-within:ring-accent/18">
              <button
                type="submit"
                className="flex h-full w-11 shrink-0 items-center justify-center transition hover:bg-paper focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent"
                aria-label="Buscar"
              >
                <Image src="/icons/search.svg" alt="" width={16} height={16} />
              </button>
              <input
                id="site-search"
                value={searchTerm}
                onChange={(event) => setSearchTerm(event.target.value)}
                placeholder="Buscar libros, aut..."
                className="min-w-0 flex-1 bg-transparent pr-4 text-xs font-semibold italic text-foreground outline-none placeholder:text-muted"
              />
            </div>
          </form>
        </div>

        <div className="flex min-w-0 shrink-0 items-center justify-end gap-3 justify-self-end sm:gap-4">
          <Link
            href={routes.favorites}
            className="relative flex h-10 w-10 shrink-0 items-center justify-center rounded-full border border-[rgba(53,30,28,0.24)] bg-[#f8efe9] text-foreground shadow-[0_6px_14px_rgba(53,30,28,0.06)] transition hover:border-accent hover:text-accent focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent"
            aria-label="Ver favoritos"
          >
            <HeartIcon isFilled={favoriteTotal > 0} />
            {favoriteTotal > 0 && (
              <span className="absolute -right-1 -top-1 flex h-4 min-w-4 items-center justify-center rounded-full bg-accent px-1 text-[0.52rem] font-black leading-none text-paper">
                {favoriteTotal > 9 ? "9+" : favoriteTotal}
              </span>
            )}
          </Link>
          <Link
            href={routes.cart}
            className="relative flex h-10 w-10 shrink-0 items-center justify-center rounded-full border border-[rgba(53,30,28,0.24)] bg-[#f8efe9] text-foreground shadow-[0_6px_14px_rgba(53,30,28,0.06)] transition hover:border-accent hover:text-accent focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent"
            aria-label="Ver carrito"
          >
            <Image src="/icons/cart.svg" alt="" width={18} height={18} />
            {cartTotal > 0 && (
              <span className="absolute -right-1 -top-1 flex h-4 min-w-4 items-center justify-center rounded-full bg-accent px-1 text-[0.52rem] font-black leading-none text-paper">
                {cartTotal > 9 ? "9+" : cartTotal}
              </span>
            )}
          </Link>
          <div className="hidden shrink-0 items-center gap-4 sm:flex">
            {user ? (
              <div ref={profileMenuRef} className="relative">
                <button
                  type="button"
                  className="flex h-10 w-10 items-center justify-center rounded-full border border-[rgba(53,30,28,0.26)] bg-[#f8efe9] text-[0.66rem] font-black text-foreground shadow-[inset_0_1px_0_rgba(255,255,255,0.75),0_8px_18px_rgba(53,30,28,0.08)] transition hover:border-accent hover:text-accent focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent"
                  aria-label="Abrir menu de perfil"
                  aria-expanded={isProfileMenuOpen}
                  aria-controls="profile-menu"
                  title={user.fullName || user.email}
                  onClick={() => {
                    setIsProfileMenuOpen((isOpen) => !isOpen);
                    setIsCategoryMenuOpen(false);
                  }}
                >
                  {getUserInitials(user)}
                </button>

                <div
                  id="profile-menu"
                  className={`absolute right-0 top-12 z-20 w-56 overflow-hidden rounded-[18px] border border-border bg-paper shadow-[0_20px_48px_rgba(53,30,28,0.16)] transition duration-200 ${
                    isProfileMenuOpen
                      ? "pointer-events-auto translate-y-0 opacity-100"
                      : "pointer-events-none -translate-y-2 opacity-0"
                  }`}
                >
                  <div className="border-b border-border/70 px-4 py-3">
                    <span className="block truncate text-[0.72rem] font-black text-foreground">
                      {user.fullName || "Mi cuenta"}
                    </span>
                    <span className="mt-1 block truncate text-[0.62rem] font-bold text-muted">
                      {user.email}
                    </span>
                  </div>
                  <div className="p-2">
                    <button
                      type="button"
                      onClick={handleLogout}
                      className="flex h-10 w-full items-center justify-center rounded-xl text-[0.68rem] font-black text-foreground transition hover:bg-[#f8efe9] hover:text-accent focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent"
                    >
                      Salir
                    </button>
                  </div>
                </div>
              </div>
            ) : (
              <>
                <Link
                  href="/auth/login"
                  className="flex h-10 items-center whitespace-nowrap rounded-full border border-[rgba(53,30,28,0.22)] px-4 text-[0.68rem] font-black text-foreground transition hover:border-accent hover:text-accent focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent"
                >
                  Iniciar sesion
                </Link>
                <Link
                  href="/auth/register"
                  className="flex h-10 items-center whitespace-nowrap rounded-full border border-[rgba(53,30,28,0.32)] bg-foreground px-6 text-[0.68rem] font-black text-paper shadow-[0_8px_18px_rgba(53,30,28,0.18)] transition hover:border-accent hover:bg-accent focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent"
                >
                  Crear cuenta
                </Link>
              </>
            )}
          </div>
        </div>
      </div>
    </header>
  );
}
