"use client";

import { useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { motion } from "framer-motion";
import { register } from "../services/auth.service";

export function RegisterForm() {
  const router = useRouter();
  const [name, setName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);

    if (password !== confirmPassword) {
      setError("Las contraseñas no coinciden.");
      return;
    }

    if (password.length < 8) {
      setError("La contraseña debe tener al menos 8 caracteres.");
      return;
    }

    setIsSubmitting(true);

    try {
      await register({
        fullName: name.trim(),
        email: email.trim(),
        password,
      });
      router.push("/");
      router.refresh();
    } catch (error) {
      setError(error instanceof Error ? error.message : "No se pudo crear la cuenta.");
    } finally {
      setIsSubmitting(false);
    }
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
          Crear cuenta
        </h2>
        <p className="mt-2 text-sm font-medium text-muted">
          Únete a Webook y descubre un mundo de libros
        </p>
      </div>

      <form onSubmit={handleSubmit} className="space-y-5">
        <div className="space-y-1.5">
          <label
            htmlFor="name"
            className="text-xs font-bold uppercase tracking-widest text-foreground/80"
          >
            Nombre completo
          </label>
          <input
            id="name"
            type="text"
            value={name}
            onChange={(e) => setName(e.target.value)}
            placeholder="Tu nombre"
            required
            className="w-full rounded-2xl border border-border bg-background px-4 py-3.5 text-sm font-medium text-foreground transition-colors placeholder:text-muted/60 focus:border-accent focus:outline-none focus:ring-1 focus:ring-accent"
          />
        </div>

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
          <label
            htmlFor="password"
            className="text-xs font-bold uppercase tracking-widest text-foreground/80"
          >
            Contraseña
          </label>
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

        <div className="space-y-1.5">
          <label
            htmlFor="confirmPassword"
            className="text-xs font-bold uppercase tracking-widest text-foreground/80"
          >
            Confirmar contraseña
          </label>
          <input
            id="confirmPassword"
            type="password"
            value={confirmPassword}
            onChange={(e) => setConfirmPassword(e.target.value)}
            placeholder="••••••••"
            required
            className="w-full rounded-2xl border border-border bg-background px-4 py-3.5 text-sm font-medium text-foreground transition-colors placeholder:text-muted/60 focus:border-accent focus:outline-none focus:ring-1 focus:ring-accent"
          />
        </div>

        {error ? (
          <p className="rounded-2xl border border-red-200 bg-red-50 px-4 py-3 text-sm font-bold text-red-700">
            {error}
          </p>
        ) : null}

        <button
          type="submit"
          disabled={isSubmitting}
          className="group relative mt-8 flex w-full h-14 items-center justify-center overflow-hidden rounded-full bg-foreground px-8 font-black text-paper shadow-[0_8px_20px_rgba(53,30,28,0.25)] transition-all hover:-translate-y-0.5 hover:shadow-[0_12px_28px_rgba(53,30,28,0.35)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent"
        >
          <span className="relative z-10">
            {isSubmitting ? "Creando cuenta..." : "Registrarse"}
          </span>
          <div className="absolute inset-0 z-0 bg-gradient-to-r from-accent to-[#a0c9cb] opacity-0 transition-opacity duration-300 group-hover:opacity-100" />
        </button>
      </form>

      <p className="mt-8 text-center text-sm font-medium text-muted">
        ¿Ya tienes una cuenta?{" "}
        <Link
          href="/auth/login"
          className="font-bold text-foreground transition-colors hover:text-accent"
        >
          Iniciar sesión
        </Link>
      </p>
    </motion.div>
  );
}
