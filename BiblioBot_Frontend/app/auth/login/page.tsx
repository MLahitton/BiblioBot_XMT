import { LoginForm } from "@/features/auth/components/LoginForm";
import type { Metadata } from "next";

export const metadata: Metadata = {
  title: "Iniciar Sesión | Webook",
  description: "Inicia sesión en tu cuenta de Webook para continuar.",
};

export default function LoginPage() {
  return <LoginForm />;
}
