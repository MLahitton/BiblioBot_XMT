import Image from "next/image";
import Link from "next/link";
import { siteConfig } from "@/config/site";

const navItems = siteConfig.navItems.filter((item) =>
  ["Inicio", "Categorías", "Destacados"].includes(item.label),
);

export function Header() {
  return (
    <header className="fixed left-0 right-0 top-0 z-[100] bg-background/90 px-6 py-3 backdrop-blur-md lg:px-10">
      <div className="flex h-14 items-center justify-between rounded-b-[28px] rounded-t-sm bg-paper px-5 shadow-[0_14px_34px_var(--shadow-soft)]">
        <Link
          href="/"
          className="flex items-center rounded-lg outline-none focus-visible:ring-2 focus-visible:ring-foreground"
          aria-label="Webook inicio"
        >
          <Image
            src="/images/biblioBot/cutouts/Logo_Webook-cutout.png"
            alt="Webook Logo"
            width={942}
            height={998}
            className="h-9 w-auto object-contain drop-shadow-[0_8px_12px_rgba(53,30,28,0.12)]"
            priority
          />
        </Link>

        <nav
          className="hidden items-center gap-9 md:flex"
          aria-label="Navegación principal"
        >
          {navItems.map((item) => (
            <a
              key={item.href}
              href={item.href}
              className="text-xs font-bold text-foreground transition hover:text-accent focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent"
            >
              {item.label}
            </a>
          ))}
        </nav>

        <div className="flex items-center gap-2">
          <button
            type="button"
            className="flex h-9 w-9 items-center justify-center rounded-full border border-border bg-paper transition hover:bg-card focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent"
            aria-label="Buscar libros"
          >
            <Image src="/icons/search.svg" alt="" width={17} height={17} />
          </button>
          <button
            type="button"
            className="relative flex h-9 w-9 items-center justify-center rounded-full border border-border bg-paper transition hover:bg-card focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent"
            aria-label="Ver carrito"
          >
            <Image src="/icons/cart.svg" alt="" width={17} height={17} />
            <span className="absolute -right-0.5 -top-0.5 h-3.5 min-w-3.5 rounded-full bg-accent px-1 text-[0.55rem] font-bold leading-3.5 text-paper">
              0
            </span>
          </button>
          <div className="hidden items-center gap-2 sm:flex border-l border-border/60 pl-3 ml-1">
            <Link
              href="/auth/login"
              className="rounded-full px-3 py-1.5 text-xs font-bold text-foreground transition hover:text-accent focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent"
            >
              Iniciar sesión
            </Link>
            <Link
              href="/auth/register"
              className="rounded-full bg-foreground px-4 py-1.5 text-xs font-bold text-paper transition hover:bg-accent focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent"
            >
              Crear cuenta
            </Link>
          </div>
        </div>
      </div>
    </header>
  );
}
