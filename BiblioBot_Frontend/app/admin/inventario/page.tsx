import type { Metadata } from "next";
import { Header } from "@/components/layout/Header";
import { AdminInventoryPage } from "@/features/dashboard/components/AdminInventoryPage";
import { BiblioBotChatWidget } from "@/features/home/components/BiblioBotChatWidget";
import { ChatProvider } from "@/features/home/components/ChatContext";
import { PageShell } from "@/features/home/components/PageShell";

export const metadata: Metadata = {
  title: "Inventario | Webook",
  description: "Modulo administrativo de inventario de Webook.",
};

export default function AdminInventoryRoutePage() {
  return (
    <ChatProvider>
      <PageShell>
        <Header />
        <AdminInventoryPage />
        <BiblioBotChatWidget />
      </PageShell>
    </ChatProvider>
  );
}
