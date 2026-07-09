"use client";

import Image from "next/image";
import { FormEvent, useEffect, useRef, useState } from "react";
import { useChatContext } from "./ChatContext";

type ChatMessage = {
  id: number;
  author: "bot" | "user";
  text: string;
};

const initialMessages: ChatMessage[] = [
  {
    id: 1,
    author: "bot",
    text: "Hola, soy BiblioBot. Puedo ayudarte a encontrar una lectura por categoria, precio o estado de animo.",
  },
];

const quickPrompts = [
  "Recomiendame ficcion",
  "Libros para aprender",
  "Algo para regalar",
];

function getBotReply(message: string): string {
  const normalized = message.toLowerCase();

  if (normalized.includes("ficcion") || normalized.includes("novela")) {
    return "Te recomiendo empezar por La Ciudad de Papel. Tiene un tono cercano, urbano y es una buena primera compra.";
  }

  if (normalized.includes("aprender") || normalized.includes("tecnologia")) {
    return "Para aprender, Codigo Aurora funciona muy bien: es claro, moderno y pensado para entrar en temas digitales sin friccion.";
  }

  if (normalized.includes("regal")) {
    return "Para regalo miraria Materia Viva o Galeria Interior. Son opciones cuidadas, bonitas y faciles de recomendar.";
  }

  return "Puedo ayudarte con recomendaciones, categorias, precios o novedades. Cuentame que tipo de lectura tienes en mente.";
}

function CloseIcon() {
  return (
    <svg aria-hidden="true" className="h-4 w-4" fill="none" viewBox="0 0 24 24">
      <path
        d="M6.75 6.75l10.5 10.5M17.25 6.75l-10.5 10.5"
        stroke="currentColor"
        strokeLinecap="round"
        strokeWidth="2.2"
      />
    </svg>
  );
}

function ExpandIcon({ isExpanded }: { isExpanded: boolean }) {
  if (isExpanded) {
    return (
      <svg aria-hidden="true" className="h-4 w-4" fill="none" viewBox="0 0 24 24">
        <path
          d="M9.5 4.75v4.75H4.75M14.5 19.25V14.5h4.75M4.75 9.5l4.75-4.75M19.25 14.5l-4.75 4.75"
          stroke="currentColor"
          strokeLinecap="round"
          strokeLinejoin="round"
          strokeWidth="2"
        />
      </svg>
    );
  }

  return (
    <svg aria-hidden="true" className="h-4 w-4" fill="none" viewBox="0 0 24 24">
      <path
        d="M8.5 4.75H4.75V8.5M15.5 4.75h3.75V8.5M4.75 15.5v3.75H8.5M19.25 15.5v3.75H15.5"
        stroke="currentColor"
        strokeLinecap="round"
        strokeLinejoin="round"
        strokeWidth="2"
      />
    </svg>
  );
}

export function BiblioBotChatWidget() {
  const [isOpen, setIsOpen] = useState(false);
  const [isExpanded, setIsExpanded] = useState(false);
  const [input, setInput] = useState("");
  const [messages, setMessages] = useState<ChatMessage[]>(initialMessages);
  const messagesEndRef = useRef<HTMLDivElement | null>(null);
  const { setIsChatExpanded } = useChatContext();

  useEffect(() => {
    setIsChatExpanded(isOpen && isExpanded);
  }, [isOpen, isExpanded, setIsChatExpanded]);

  useEffect(() => {
    if (isOpen) {
      messagesEndRef.current?.scrollIntoView({ block: "end" });
    }
  }, [isOpen, messages, isExpanded]);

  useEffect(() => {
    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        setIsOpen(false);
        setIsExpanded(false);
      }
    };
    window.addEventListener("keydown", handleKeyDown);
    return () => window.removeEventListener("keydown", handleKeyDown);
  }, []);

  const closeChat = () => {
    setIsOpen(false);
    setIsExpanded(false);
  };

  const sendMessage = (text: string) => {
    const trimmed = text.trim();
    if (!trimmed) return;
    setMessages((currentMessages) => [
      ...currentMessages,
      { id: currentMessages.length + 1, author: "user", text: trimmed },
      { id: currentMessages.length + 2, author: "bot", text: getBotReply(trimmed) },
    ]);
    setInput("");
  };

  const handleSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    sendMessage(input);
  };

  const panelClassName = isExpanded
    ? `fixed inset-y-0 right-0 z-[110] flex h-dvh w-full origin-right flex-col overflow-hidden border-l border-border bg-paper shadow-[-8px_0_40px_rgba(53,30,28,0.14)] transition-[transform,opacity] duration-300 sm:w-[420px] ${
        isOpen ? "pointer-events-auto translate-x-0 opacity-100" : "pointer-events-none translate-x-full opacity-0"
      }`
    : `absolute bottom-[5.25rem] right-5 flex max-h-[calc(100dvh-7rem)] w-[calc(100vw-2.5rem)] max-w-sm origin-bottom-right flex-col overflow-hidden rounded-[26px] border border-border bg-paper/98 shadow-[0_28px_70px_rgba(53,30,28,0.18)] backdrop-blur-md transition duration-300 sm:bottom-[6.75rem] sm:right-7 sm:w-[360px] ${
        isOpen ? "pointer-events-auto translate-y-0 scale-100 opacity-100" : "pointer-events-none translate-y-4 scale-95 opacity-0"
      }`;

  const messagesClassName = isExpanded
    ? "relative min-h-0 flex-1 space-y-3 overflow-y-auto px-4 py-4 [scrollbar-color:rgba(53,30,28,0.22)_transparent] [scrollbar-width:thin] sm:px-5"
    : "relative max-h-[350px] space-y-3 overflow-y-auto px-4 py-4 [scrollbar-color:rgba(53,30,28,0.22)_transparent] [scrollbar-width:thin]";

  return (
    <div className="pointer-events-none fixed inset-0 z-[120]">
      <div id="bibliobot-chat" className={panelClassName}>
        {!isExpanded ? (
          <div className="absolute -bottom-2 right-8 h-5 w-5 rotate-45 border-b border-r border-border bg-paper" />
        ) : null}

        <div className="relative flex items-center justify-between gap-3 border-b border-border/80 px-4 py-3 sm:px-5">
          {isExpanded && (
            <div className="absolute inset-x-0 top-0 h-[3px] bg-gradient-to-r from-accent via-[#a0c9cb] to-accent opacity-70" />
          )}
          <div className="flex min-w-0 items-center gap-3">
            <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-full border border-border bg-card shadow-inner">
              <Image
                src="/images/biblioBot/cutouts/icono_bibliobot-cutout.png"
                alt=""
                width={833}
                height={970}
                className="h-8 w-8 object-contain"
                sizes="32px"
              />
            </div>
            <div className="min-w-0">
              <div className="flex items-center gap-2">
                <p className="truncate text-sm font-black text-foreground">BiblioBot</p>
                <span className="h-2 w-2 rounded-full bg-accent" />
              </div>
              <p className="truncate text-xs font-semibold text-muted">En linea</p>
            </div>
          </div>

          <div className="flex shrink-0 items-center gap-2">
            <button
              type="button"
              className={`flex h-9 w-9 items-center justify-center rounded-full border text-foreground transition focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent ${
                isExpanded
                  ? "border-foreground bg-foreground text-paper hover:bg-accent"
                  : "border-border bg-card hover:border-accent/50 hover:bg-paper"
              }`}
              aria-label={isExpanded ? "Restaurar chat" : "Expandir chat"}
              onClick={() => setIsExpanded((v) => !v)}
            >
              <ExpandIcon isExpanded={isExpanded} />
            </button>
            <button
              type="button"
              className="flex h-9 w-9 items-center justify-center rounded-full border border-border bg-paper text-muted transition hover:border-accent/50 hover:bg-accent hover:text-paper focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent"
              aria-label="Cerrar chat"
              onClick={closeChat}
            >
              <CloseIcon />
            </button>
          </div>
        </div>

        <div className={messagesClassName}>
          {messages.map((message) => (
            <div key={message.id}>
              <div className={`flex ${message.author === "user" ? "justify-end" : "justify-start"}`}>
                <p
                  className={`max-w-[82%] px-4 py-3 text-sm font-semibold leading-5 ${
                    message.author === "user"
                      ? "rounded-[18px] rounded-br-md bg-foreground text-paper"
                      : "rounded-[18px] rounded-bl-md bg-card text-foreground shadow-[inset_0_1px_0_rgba(255,255,255,0.44)]"
                  }`}
                >
                  {message.text}
                </p>
              </div>
              {message.id === 1 ? (
                <div className="mt-3 flex flex-wrap gap-2 pl-2">
                  {quickPrompts.map((prompt) => (
                    <button
                      key={prompt}
                      type="button"
                      className="rounded-full border border-border bg-paper px-3 py-2 text-xs font-extrabold text-foreground shadow-sm transition hover:border-accent/40 hover:bg-card focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent"
                      onClick={() => sendMessage(prompt)}
                    >
                      {prompt}
                    </button>
                  ))}
                </div>
              ) : null}
            </div>
          ))}
          <div ref={messagesEndRef} />
        </div>

        <form
          className="relative flex items-center gap-2 border-t border-border/80 bg-paper px-4 py-3 sm:px-5"
          onSubmit={handleSubmit}
        >
          <label className="sr-only" htmlFor="bibliobot-message">
            Mensaje para BiblioBot
          </label>
          <input
            id="bibliobot-message"
            value={input}
            onChange={(event) => setInput(event.target.value)}
            placeholder="Escribe tu pregunta"
            className="h-11 min-w-0 flex-1 rounded-full border border-border bg-card px-4 text-sm font-semibold text-foreground outline-none placeholder:text-muted focus:border-accent"
          />
          <button
            type="submit"
            className="h-11 rounded-full bg-accent px-4 text-xs font-black text-paper shadow-[0_10px_20px_rgba(255,96,55,0.2)] transition hover:bg-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent"
          >
            Enviar
          </button>
        </form>
      </div>

      <button
        type="button"
        className={`pointer-events-auto absolute bottom-5 right-5 flex h-16 w-16 items-center justify-center rounded-[24px] border border-border bg-paper shadow-[0_18px_38px_rgba(53,30,28,0.2),0_0_0_6px_rgba(255,96,55,0.08)] transition hover:-translate-y-1 hover:border-accent/40 hover:shadow-[0_22px_44px_rgba(53,30,28,0.24),0_0_0_7px_rgba(160,201,203,0.16)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent sm:bottom-7 sm:right-7 sm:h-20 sm:w-20 sm:rounded-[28px] ${
          isExpanded && isOpen ? "pointer-events-none translate-x-4 opacity-0" : "opacity-100"
        }`}
        aria-label={isOpen ? "Cerrar BiblioBot" : "Abrir BiblioBot"}
        aria-controls="bibliobot-chat"
        aria-expanded={isOpen}
        onClick={() => setIsOpen((v) => !v)}
      >
        <span className="absolute -bottom-1 right-3 h-4 w-4 rotate-45 border-b border-r border-border bg-paper" />
        <Image
          src="/images/biblioBot/cutouts/icono_bibliobot-cutout.png"
          alt=""
          width={833}
          height={970}
          className="relative h-[78%] w-[78%] object-contain"
          sizes="(max-width: 640px) 52px, 66px"
        />
      </button>
    </div>
  );
}
