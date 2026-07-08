"use client";

import { type FormEvent, useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { motion } from "framer-motion";
import { register } from "../services/auth.service";

type FieldName =
  | "name"
  | "email"
  | "phone"
  | "documentNumber"
  | "password"
  | "confirmPassword";

type FieldErrors = Partial<Record<FieldName, string>>;

const emptyFieldMessage = "No se ha llenado ese campo.";
const duplicateEmailMessage = "Ese correo ya tiene una cuenta asignada. Usa otro correo o inicia sesion.";

function getInputClass(hasError: boolean): string {
  return [
    "w-full rounded-2xl border bg-background px-4 py-3.5 text-sm font-medium text-foreground transition-colors placeholder:text-muted/60 focus:outline-none focus:ring-1",
    hasError
      ? "border-red-300 focus:border-red-400 focus:ring-red-200"
      : "border-border focus:border-accent focus:ring-accent",
  ].join(" ");
}

function isDuplicateEmailError(error: unknown): boolean {
  return error instanceof Error && error.message.includes("EMAIL_ALREADY_EXISTS");
}

export function RegisterForm() {
  const router = useRouter();
  const [name, setName] = useState("");
  const [email, setEmail] = useState("");
  const [phone, setPhone] = useState("");
  const [documentNumber, setDocumentNumber] = useState("");
  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({});
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const clearFieldError = (field: FieldName) => {
    setFieldErrors((current) => {
      if (!current[field]) return current;

      const next = { ...current };
      delete next[field];
      return next;
    });
    setError(null);
  };

  const setFieldError = (field: FieldName, message: string) => {
    setFieldErrors({ [field]: message });
    setError(null);
  };

  const handleNumericChange = (
    field: "phone" | "documentNumber",
    value: string,
    onChange: (nextValue: string) => void,
  ) => {
    if (/[A-Za-z]/.test(value)) {
      const message = field === "phone"
        ? "No se pueden ingresar letras en el telefono."
        : "No se pueden ingresar letras en el documento.";
      setFieldError(field, message);
    } else {
      clearFieldError(field);
    }

    onChange(value.replace(/\D/g, ""));
  };

  const validateForm = (): boolean => {
    const trimmedName = name.trim();
    const trimmedEmail = email.trim();
    const trimmedPhone = phone.trim();
    const trimmedDocumentNumber = documentNumber.trim();

    const requiredFields: Array<{ field: FieldName; value: string }> = [
      { field: "name", value: trimmedName },
      { field: "email", value: trimmedEmail },
      { field: "phone", value: trimmedPhone },
      { field: "documentNumber", value: trimmedDocumentNumber },
      { field: "password", value: password },
      { field: "confirmPassword", value: confirmPassword },
    ];

    const emptyField = requiredFields.find((item) => item.value.length === 0);
    if (emptyField) {
      setFieldError(emptyField.field, emptyFieldMessage);
      return false;
    }

    if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(trimmedEmail)) {
      setFieldError("email", "Ingresa un correo valido.");
      return false;
    }

    if (!/^\d+$/.test(trimmedPhone)) {
      setFieldError("phone", "El telefono solo puede contener numeros.");
      return false;
    }

    if (!/^\d+$/.test(trimmedDocumentNumber)) {
      setFieldError("documentNumber", "El documento solo puede contener numeros.");
      return false;
    }

    if (password !== confirmPassword) {
      setFieldError("confirmPassword", "Las contrasenas no coinciden.");
      return false;
    }

    if (password.length < 8) {
      setFieldError("password", "La contrasena debe tener al menos 8 caracteres.");
      return false;
    }

    setFieldErrors({});
    setError(null);
    return true;
  };

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    if (!validateForm()) return;

    setIsSubmitting(true);

    try {
      await register({
        fullName: name.trim(),
        email: email.trim().toLowerCase(),
        password,
        phone: phone.trim(),
        documentNumber: documentNumber.trim(),
      });
      router.push("/");
      router.refresh();
    } catch (registerError) {
      if (isDuplicateEmailError(registerError)) {
        setFieldError("email", duplicateEmailMessage);
      } else {
        setError(registerError instanceof Error ? registerError.message : "No se pudo crear la cuenta.");
      }
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <motion.div
      initial={{ opacity: 0, y: 20 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.5 }}
      className="w-full max-w-lg rounded-[32px] border border-border/50 bg-paper/80 p-7 shadow-[0_24px_80px_rgba(53,30,28,0.08)] backdrop-blur-xl sm:p-10"
    >
      <div className="mb-7 text-center">
        <h2 className="text-3xl font-black tracking-tight text-foreground">
          Crear cuenta
        </h2>
        <p className="mx-auto mt-2 max-w-xs text-sm font-medium leading-6 text-muted">
          Unete a Webook y descubre un mundo de libros
        </p>
      </div>

      <form onSubmit={handleSubmit} className="space-y-4" noValidate>
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
            onChange={(event) => {
              setName(event.target.value);
              clearFieldError("name");
            }}
            placeholder="Tu nombre"
            maxLength={150}
            aria-invalid={Boolean(fieldErrors.name)}
            aria-describedby={fieldErrors.name ? "name-error" : undefined}
            className={getInputClass(Boolean(fieldErrors.name))}
          />
          {fieldErrors.name ? (
            <p id="name-error" className="text-xs font-bold text-red-700">
              {fieldErrors.name}
            </p>
          ) : null}
        </div>

        <div className="space-y-1.5">
          <label
            htmlFor="email"
            className="text-xs font-bold uppercase tracking-widest text-foreground/80"
          >
            Correo electronico
          </label>
          <input
            id="email"
            type="email"
            value={email}
            onChange={(event) => {
              setEmail(event.target.value);
              clearFieldError("email");
            }}
            placeholder="hola@webook.com"
            maxLength={180}
            aria-invalid={Boolean(fieldErrors.email)}
            aria-describedby={fieldErrors.email ? "email-error" : undefined}
            className={getInputClass(Boolean(fieldErrors.email))}
          />
          {fieldErrors.email ? (
            <p id="email-error" className="text-xs font-bold text-red-700">
              {fieldErrors.email}
            </p>
          ) : null}
        </div>

        <div className="grid gap-4 sm:grid-cols-2">
          <div className="space-y-1.5">
            <label
              htmlFor="phone"
              className="text-xs font-bold uppercase tracking-widest text-foreground/80"
            >
              Telefono
            </label>
            <input
              id="phone"
              type="tel"
              value={phone}
              onChange={(event) => handleNumericChange("phone", event.target.value, setPhone)}
              placeholder="3001234567"
              inputMode="numeric"
              maxLength={40}
              aria-invalid={Boolean(fieldErrors.phone)}
              aria-describedby={fieldErrors.phone ? "phone-error" : undefined}
              className={getInputClass(Boolean(fieldErrors.phone))}
            />
            {fieldErrors.phone ? (
              <p id="phone-error" className="text-xs font-bold text-red-700">
                {fieldErrors.phone}
              </p>
            ) : null}
          </div>

          <div className="space-y-1.5">
            <label
              htmlFor="documentNumber"
              className="text-xs font-bold uppercase tracking-widest text-foreground/80"
            >
              Documento
            </label>
            <input
              id="documentNumber"
              type="text"
              value={documentNumber}
              onChange={(event) => handleNumericChange("documentNumber", event.target.value, setDocumentNumber)}
              placeholder="Numero de documento"
              inputMode="numeric"
              maxLength={50}
              aria-invalid={Boolean(fieldErrors.documentNumber)}
              aria-describedby={fieldErrors.documentNumber ? "document-error" : undefined}
              className={getInputClass(Boolean(fieldErrors.documentNumber))}
            />
            {fieldErrors.documentNumber ? (
              <p id="document-error" className="text-xs font-bold text-red-700">
                {fieldErrors.documentNumber}
              </p>
            ) : null}
          </div>
        </div>

        <div className="grid gap-4 sm:grid-cols-2">
          <div className="space-y-1.5">
            <label
              htmlFor="password"
              className="text-xs font-bold uppercase tracking-widest text-foreground/80"
            >
              Contrasena
            </label>
            <input
              id="password"
              type="password"
              value={password}
              onChange={(event) => {
                setPassword(event.target.value);
                clearFieldError("password");
              }}
              placeholder="********"
              minLength={8}
              maxLength={100}
              aria-invalid={Boolean(fieldErrors.password)}
              aria-describedby={fieldErrors.password ? "password-error" : undefined}
              className={getInputClass(Boolean(fieldErrors.password))}
            />
            {fieldErrors.password ? (
              <p id="password-error" className="text-xs font-bold text-red-700">
                {fieldErrors.password}
              </p>
            ) : null}
          </div>

          <div className="space-y-1.5">
            <label
              htmlFor="confirmPassword"
              className="text-xs font-bold uppercase tracking-widest text-foreground/80"
            >
              Confirmar
            </label>
            <input
              id="confirmPassword"
              type="password"
              value={confirmPassword}
              onChange={(event) => {
                setConfirmPassword(event.target.value);
                clearFieldError("confirmPassword");
              }}
              placeholder="********"
              minLength={8}
              maxLength={100}
              aria-invalid={Boolean(fieldErrors.confirmPassword)}
              aria-describedby={fieldErrors.confirmPassword ? "confirm-password-error" : undefined}
              className={getInputClass(Boolean(fieldErrors.confirmPassword))}
            />
            {fieldErrors.confirmPassword ? (
              <p id="confirm-password-error" className="text-xs font-bold text-red-700">
                {fieldErrors.confirmPassword}
              </p>
            ) : null}
          </div>
        </div>

        {error ? (
          <p className="rounded-2xl border border-red-200 bg-red-50 px-4 py-3 text-sm font-bold text-red-700">
            {error}
          </p>
        ) : null}

        <button
          type="submit"
          disabled={isSubmitting}
          className="group relative mt-7 flex h-14 w-full items-center justify-center overflow-hidden rounded-full bg-foreground px-8 font-black text-paper shadow-[0_8px_20px_rgba(53,30,28,0.25)] transition-all hover:-translate-y-0.5 hover:shadow-[0_12px_28px_rgba(53,30,28,0.35)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent disabled:cursor-not-allowed disabled:opacity-60"
        >
          <span className="relative z-10">
            {isSubmitting ? "Creando cuenta..." : "Registrarse"}
          </span>
          <div className="absolute inset-0 z-0 bg-gradient-to-r from-accent to-[#a0c9cb] opacity-0 transition-opacity duration-300 group-hover:opacity-100" />
        </button>
      </form>

      <p className="mt-7 text-center text-sm font-medium text-muted">
        Ya tienes una cuenta?{" "}
        <Link
          href="/auth/login"
          className="font-bold text-foreground transition-colors hover:text-accent"
        >
          Iniciar sesion
        </Link>
      </p>
    </motion.div>
  );
}
