import { RegisterForm } from "@/features/auth/components/RegisterForm";
import type { Metadata } from "next";

export const metadata: Metadata = {
  title: "Crear Cuenta | Webook",
  description: "Crea tu cuenta en Webook y empieza a explorar.",
};

export default function RegisterPage() {
  return <RegisterForm />;
}
