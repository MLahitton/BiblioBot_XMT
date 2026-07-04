"use client";

import { useState } from "react";
import Link from "next/link";
import { motion } from "framer-motion";

export function LoginForm() {
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    // Simulate login
    console.log({ email, password });
  };

  return (
    <motion.div
      initial={{ opacity: 0, y: 20 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.5 }}
      className="w-full max-w-md rounded-[32px] border border-border/50 bg-paper/80 p-8 shadow-[0_24px_80px_rgba(53,30,28,0.08)] backdrop-blur-xl sm:p-12"
    >
      <div className="mb-8 text-center">
        <h2 className="text-3xl font-black tracking-tight text-foreground">
          Bienvenido de vuelta
        </h2>
        <p className="mt-2 text-sm font-medium text-muted">
          Ingresa tus datos para acceder a tu cuenta
        </p>
      </div>

      <form onSubmit={handleSubmit} className="space-y-5">
        <div className="space-y-1.5">
          <label
            htmlFor="email"
            className="text-xs font-bold uppercase tracking-widest text-foreground/80"
          >
            Correo Electrónico
          </label>
          <input
            id="email"
            type="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            placeholder="hola@webook.com"
            required
            className="w-full rounded-2xl border border-border bg-background px-4 py-3.5 text-sm font-medium text-foreground transition-colors placeholder:text-muted/60 focus:border-accent focus:outline-none focus:ring-1 focus:ring-accent"
          />
        </div>

        <div className="space-y-1.5">
          <div className="flex items-center justify-between">
            <label
              htmlFor="password"
              className="text-xs font-bold uppercase tracking-widest text-foreground/80"
            >
              Contraseña
            </label>
            <Link
              href="#"
              className="text-xs font-bold text-accent transition-colors hover:text-accent/80"
            >
              ¿Olvidaste tu contraseña?
            </Link>
          </div>
          <input
            id="password"
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            placeholder="••••••••"
            required
            className="w-full rounded-2xl border border-border bg-background px-4 py-3.5 text-sm font-medium text-foreground transition-colors placeholder:text-muted/60 focus:border-accent focus:outline-none focus:ring-1 focus:ring-accent"
          />
        </div>

        <button
          type="submit"
          className="group relative mt-8 flex w-full h-14 items-center justify-center overflow-hidden rounded-full bg-foreground px-8 font-black text-paper shadow-[0_8px_20px_rgba(53,30,28,0.25)] transition-all hover:-translate-y-0.5 hover:shadow-[0_12px_28px_rgba(53,30,28,0.35)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent"
        >
          <span className="relative z-10">Iniciar Sesión</span>
          <div className="absolute inset-0 z-0 bg-gradient-to-r from-accent to-[#a0c9cb] opacity-0 transition-opacity duration-300 group-hover:opacity-100" />
        </button>
      </form>

      <p className="mt-8 text-center text-sm font-medium text-muted">
        ¿No tienes una cuenta?{" "}
        <Link
          href="/auth/register"
          className="font-bold text-foreground transition-colors hover:text-accent"
        >
          Crear cuenta
        </Link>
      </p>
    </motion.div>
  );
}
