import type { Metadata } from "next";
import { Header } from "@/components/layout/Header";
import { FavoritesPage } from "@/features/favorites/components/FavoritesPage";
import { BiblioBotChatWidget } from "@/features/home/components/BiblioBotChatWidget";
import { ChatProvider } from "@/features/home/components/ChatContext";
import { PageShell } from "@/features/home/components/PageShell";

export const metadata: Metadata = {
  title: "Favoritos | Webook",
  description: "Consulta tus libros favoritos guardados en Webook.",
};

export default function FavoritesRoutePage() {
  return (
    <ChatProvider>
      <PageShell>
        <Header />
        <FavoritesPage />
        <BiblioBotChatWidget />
      </PageShell>
    </ChatProvider>
  );
}
