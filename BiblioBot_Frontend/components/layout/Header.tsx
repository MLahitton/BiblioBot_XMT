import Image from "next/image";
import Link from "next/link";
import { siteConfig } from "@/config/site";

const navItems = siteConfig.navItems.filter((item) =>
  ["Inicio", "Categorías", "Destacados"].includes(item.label),
);

export function Header() {
  return (
    <header className="sticky top-0 z-50 border-b border-border bg-background/[0.82] px-4 backdrop-blur-xl">
      <div className="mx-auto flex h-[72px] max-w-6xl items-center justify-between gap-4">
        <Link
          href="/"
          className="flex items-center gap-3 rounded-full outline-none transition focus-visible:ring-2 focus-visible:ring-accent"
          aria-label="Webook inicio"
        >
          <span className="flex h-10 w-10 items-center justify-center rounded-full border border-border bg-card">
            <Image src="/icons/book.svg" alt="" width={22} height={22} />
          </span>
          <span className="text-xl font-semibold text-foreground">Webook</span>
        </Link>

        <nav
          className="hidden items-center gap-1 rounded-full border border-border bg-card px-2 py-2 md:flex"
          aria-label="Navegación principal"
        >
          {navItems.map((item) => (
            <a
              key={item.href}
              href={item.href}
              className="rounded-full px-4 py-2 text-sm font-medium text-muted transition hover:bg-white/[0.08] hover:text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent"
            >
              {item.label}
            </a>
          ))}
        </nav>

        <div className="flex items-center gap-2">
          <button
            type="button"
            className="flex h-10 w-10 items-center justify-center rounded-full border border-border bg-card transition hover:border-accent/60 hover:bg-white/[0.1] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent"
            aria-label="Buscar libros"
          >
            <Image src="/icons/search.svg" alt="" width={20} height={20} />
          </button>
          <button
            type="button"
            className="flex h-10 w-10 items-center justify-center rounded-full border border-border bg-card transition hover:border-accent/60 hover:bg-white/[0.1] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent"
            aria-label="Ver carrito"
          >
            <Image src="/icons/cart.svg" alt="" width={20} height={20} />
          </button>
          <button
            type="button"
            className="hidden rounded-full bg-accent px-4 py-2 text-sm font-semibold text-background transition hover:bg-accent-soft focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent sm:inline-flex"
          >
            Cuenta
          </button>
        </div>
      </div>
    </header>
  );
}
