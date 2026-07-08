import type { Metadata } from "next";
import { Header } from "@/components/layout/Header";
import { AdminDashboardPage } from "@/features/dashboard/components/AdminDashboardPage";
import { BiblioBotChatWidget } from "@/features/home/components/BiblioBotChatWidget";
import { ChatProvider } from "@/features/home/components/ChatContext";
import { PageShell } from "@/features/home/components/PageShell";

export const metadata: Metadata = {
  title: "Panel admin | Webook",
  description: "Panel administrativo de Webook.",
};

export default function DashboardRoutePage() {
  return (
    <ChatProvider>
      <PageShell>
        <Header />
        <AdminDashboardPage />
        <BiblioBotChatWidget />
      </PageShell>
    </ChatProvider>
  );
}
