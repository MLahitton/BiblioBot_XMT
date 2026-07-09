"use client";

import { createContext, useContext, useState, ReactNode } from "react";

type ChatContextValue = {
  isChatExpanded: boolean;
  setIsChatExpanded: (value: boolean) => void;
};

const ChatContext = createContext<ChatContextValue>({
  isChatExpanded: false,
  setIsChatExpanded: () => {},
});

export function ChatProvider({ children }: { children: ReactNode }) {
  const [isChatExpanded, setIsChatExpanded] = useState(false);

  return (
    <ChatContext.Provider value={{ isChatExpanded, setIsChatExpanded }}>
      {children}
    </ChatContext.Provider>
  );
}

export function useChatContext() {
  return useContext(ChatContext);
}
