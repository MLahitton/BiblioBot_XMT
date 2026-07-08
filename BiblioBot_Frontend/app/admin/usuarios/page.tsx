import type { Metadata } from "next";
import { Header } from "@/components/layout/Header";
import { AdminUsersPage } from "@/features/dashboard/components/AdminUsersPage";
import { BiblioBotChatWidget } from "@/features/home/components/BiblioBotChatWidget";
import { ChatProvider } from "@/features/home/components/ChatContext";
import { PageShell } from "@/features/home/components/PageShell";

export const metadata: Metadata = {
  title: "Usuarios | Webook",
  description: "Modulo administrativo de usuarios de Webook.",
};

export default function AdminUsersRoutePage() {
  return (
    <ChatProvider>
      <PageShell>
        <Header />
        <AdminUsersPage />
        <BiblioBotChatWidget />
      </PageShell>
    </ChatProvider>
  );
}
