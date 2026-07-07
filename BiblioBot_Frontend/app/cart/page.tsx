import type { Metadata } from "next";
import { Header } from "@/components/layout/Header";
import { CartPage } from "@/features/cart/components/CartPage";
import { BiblioBotChatWidget } from "@/features/home/components/BiblioBotChatWidget";
import { ChatProvider } from "@/features/home/components/ChatContext";
import { PageShell } from "@/features/home/components/PageShell";

export const metadata: Metadata = {
  title: "Carrito | Webook",
  description: "Revisa y confirma los libros seleccionados en Webook.",
};

export default function CartRoutePage() {
  return (
    <ChatProvider>
      <PageShell>
        <Header />
        <CartPage />
        <BiblioBotChatWidget />
      </PageShell>
    </ChatProvider>
  );
}
