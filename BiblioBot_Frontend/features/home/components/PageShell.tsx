"use client";

import { ReactNode } from "react";
import { useChatContext } from "./ChatContext";

export function PageShell({ children }: { children: ReactNode }) {
  const { isChatExpanded } = useChatContext();

  return (
    <div
      className={`page-shell min-h-screen w-full bg-background text-foreground ${
        isChatExpanded ? "chat-sidebar-open" : ""
      }`}
    >
      {children}
    </div>
  );
}
