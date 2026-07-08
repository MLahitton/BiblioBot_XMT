"use client";

import Link from "next/link";
import { useEffect, useMemo, useState } from "react";
import { routes } from "@/constants/routes";
import { getStoredSession } from "@/features/auth/services/auth-storage";
import type { AuthUser } from "@/features/auth/types/auth.types";
import { isAdminAccount } from "../services/admin-access";

type AdminOption = {
  title: string;
  description: string;
  permission: string;
  href?: string;
};

const adminOptions: AdminOption[] = [
  {
    title: "Usuarios",
    description: "Consultar usuarios, roles y estado de cuentas.",
    permission: "admin.users.read",
    href: routes.adminUsers,
  },
  {
    title: "Inventario",
    description: "Controlar existencias, entradas, salidas y ajustes.",
    permission: "inventory.read",
    href: routes.adminInventory,
  },
];

export function AdminDashboardPage() {
  const [user, setUser] = useState<AuthUser | null>(null);
  const [isReady, setIsReady] = useState(false);

  useEffect(() => {
    window.setTimeout(() => {
      setUser(getStoredSession()?.user ?? null);
      setIsReady(true);
    }, 0);
  }, []);

  const availableOptions = useMemo(() => {
    if (!user) return [];
    return adminOptions.filter((option) =>
      user.permissions.includes(option.permission),
    );
  }, [user]);

  if (!isReady) {
    return (
      <main className="min-h-screen bg-background px-5 pb-16 pt-36 text-foreground sm:px-8 md:pt-24 lg:px-12">
        <section className="mx-auto max-w-6xl border-y border-border/70 bg-paper/55 py-14">
          <p className="text-center text-sm font-black uppercase tracking-widest text-muted">
            Cargando panel
          </p>
        </section>
      </main>
    );
  }

  if (!isAdminAccount(user)) {
    return (
      <main className="min-h-screen bg-background px-5 pb-16 pt-36 text-foreground sm:px-8 md:pt-24 lg:px-12">
        <section className="mx-auto max-w-3xl border-y border-border/70 bg-paper/55 px-5 py-16 text-center">
          <h1 className="text-3xl font-black text-foreground">
            Acceso administrativo
          </h1>
          <p className="mx-auto mt-3 max-w-md text-sm font-semibold leading-6 text-muted">
            Este panel esta disponible solo para la cuenta administradora.
          </p>
          <Link
            href="/auth/login"
            className="mt-7 inline-flex h-11 items-center justify-center rounded-full bg-foreground px-6 text-sm font-black text-paper shadow-[0_10px_24px_rgba(53,30,28,0.18)] transition hover:bg-accent focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent"
          >
            Iniciar sesion
          </Link>
        </section>
      </main>
    );
  }

  return (
    <main className="min-h-screen bg-background px-5 pb-16 pt-36 text-foreground sm:px-8 md:pt-24 lg:px-12">
      <div className="mx-auto max-w-6xl">
        <section className="border-b border-border/70 pb-7">
          <p className="text-[0.68rem] font-black uppercase tracking-widest text-accent">
            Administracion
          </p>
          <h1 className="mt-2 text-4xl font-black leading-tight text-foreground sm:text-5xl">
            Panel admin
          </h1>
          <p className="mt-3 max-w-2xl text-sm font-semibold leading-6 text-muted">
            Cuenta activa: {user?.email}. Estas opciones estan reservadas para la cuenta administradora.
          </p>
        </section>

        <section className="mt-8 grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {availableOptions.map((option) => (
            <Link
              key={option.permission}
              href={option.href ?? routes.dashboard}
              className="border border-[rgba(53,30,28,0.18)] bg-paper/75 p-5 shadow-[0_10px_24px_rgba(53,30,28,0.05)]"
            >
              <p className="text-[0.62rem] font-black uppercase tracking-widest text-accent">
                {option.permission}
              </p>
              <h2 className="mt-3 text-xl font-black text-foreground">
                {option.title}
              </h2>
              <p className="mt-2 text-sm font-semibold leading-6 text-muted">
                {option.description}
              </p>
            </Link>
          ))}
        </section>
      </div>
    </main>
  );
}
