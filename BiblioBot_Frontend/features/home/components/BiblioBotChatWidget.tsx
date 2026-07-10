"use client";

import Image from "next/image";
import { useRouter } from "next/navigation";
import { FormEvent, useEffect, useRef, useState } from "react";
import { sendChatMessage, resetChatSessionId } from "../services/chat.service";
import type { ChatbotBookSummary, ChatbotLink, ChatbotResponse } from "../types/chat.types";
import { useChatContext } from "./ChatContext";

type ChatMessage = {
  id: number;
  author: "bot" | "user";
  text: string;
  payload?: ChatbotResponse;
  isError?: boolean;
};

const initialMessages: ChatMessage[] = [
  {
    id: 1,
    author: "bot",
    text: "Hola, soy BiblioBot. Puedo ayudarte a buscar libros, revisar detalles, consultar stock o preparar una compra segura.",
  },
];

const quickPrompts = [
  "Recomiendame libros de fantasia",
  "Ver libro El Hobbit",
  "Hay stock de El Hobbit",
];

const priceFormatter = new Intl.NumberFormat("es-CO", {
  style: "currency",
  currency: "COP",
  maximumFractionDigits: 0,
});

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
  const router = useRouter();
  const [isOpen, setIsOpen] = useState(false);
  const [isExpanded, setIsExpanded] = useState(false);
  const [input, setInput] = useState("");
  const [messages, setMessages] = useState<ChatMessage[]>(initialMessages);
  const [isSending, setIsSending] = useState(false);
  const messageIdRef = useRef(initialMessages.length + 1);
  const messagesEndRef = useRef<HTMLDivElement | null>(null);
  const { setIsChatExpanded } = useChatContext();

  useEffect(() => {
    setIsChatExpanded(isOpen && isExpanded);
  }, [isOpen, isExpanded, setIsChatExpanded]);

  useEffect(() => {
    if (isOpen) {
      messagesEndRef.current?.scrollIntoView({ block: "end" });
    }
  }, [isOpen, messages, isExpanded, isSending]);

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

  const nextMessageId = () => {
    const nextId = messageIdRef.current;
    messageIdRef.current += 1;
    return nextId;
  };

  const closeChat = () => {
    setIsOpen(false);
    setIsExpanded(false);
  };

  const restartChat = () => {
    resetChatSessionId();
    messageIdRef.current = initialMessages.length + 1;
    setMessages(initialMessages);
    setInput("");
  };

  const navigateTo = (url: string | null | undefined) => {
    const safeUrl = getSafeInternalPath(url);

    if (safeUrl) {
      router.push(safeUrl);
    }
  };

  const sendMessage = async (text: string) => {
    const trimmed = text.trim();

    if (!trimmed || isSending) return;

    setMessages((currentMessages) => [
      ...currentMessages,
      { id: nextMessageId(), author: "user", text: trimmed },
    ]);
    setInput("");
    setIsSending(true);

    try {
      const result = await sendChatMessage(trimmed);
      setMessages((currentMessages) => [
        ...currentMessages,
        {
          id: nextMessageId(),
          author: "bot",
          text: result.response.response,
          payload: result.response,
        },
      ]);
    } catch (error) {
      setMessages((currentMessages) => [
        ...currentMessages,
        {
          id: nextMessageId(),
          author: "bot",
          text: error instanceof Error ? error.message : "No pude contactar a BiblioBot en este momento.",
          isError: true,
        },
      ]);
    } finally {
      setIsSending(false);
    }
  };

  const handleSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    void sendMessage(input);
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
              <p className="truncate text-xs font-semibold text-muted">
                {isSending ? "Consultando catalogo" : "En linea"}
              </p>
            </div>
          </div>

          <div className="flex shrink-0 items-center gap-2">
            <button
              type="button"
              className="hidden rounded-full border border-border bg-card px-3 py-2 text-[0.65rem] font-black uppercase tracking-widest text-muted transition hover:border-accent/40 hover:text-accent sm:inline-flex"
              onClick={restartChat}
            >
              Nuevo
            </button>
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
                <div
                  className={`max-w-[86%] px-4 py-3 text-sm font-semibold leading-5 ${
                    message.author === "user"
                      ? "rounded-[18px] rounded-br-md bg-foreground text-paper"
                      : message.isError
                        ? "rounded-[18px] rounded-bl-md border border-red-200 bg-red-50 text-red-700"
                        : "rounded-[18px] rounded-bl-md bg-card text-foreground shadow-[inset_0_1px_0_rgba(255,255,255,0.44)]"
                  }`}
                >
                  <p>{message.text}</p>
                  {message.payload ? (
                    <ChatResponseActions payload={message.payload} onNavigate={navigateTo} />
                  ) : null}
                </div>
              </div>
              {message.id === 1 ? (
                <div className="mt-3 flex flex-wrap gap-2 pl-2">
                  {quickPrompts.map((prompt) => (
                    <button
                      key={prompt}
                      type="button"
                      disabled={isSending}
                      className="rounded-full border border-border bg-paper px-3 py-2 text-xs font-extrabold text-foreground shadow-sm transition hover:border-accent/40 hover:bg-card disabled:cursor-not-allowed disabled:opacity-60 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent"
                      onClick={() => void sendMessage(prompt)}
                    >
                      {prompt}
                    </button>
                  ))}
                </div>
              ) : null}
            </div>
          ))}
          {isSending ? (
            <div className="flex justify-start">
              <p className="rounded-[18px] rounded-bl-md bg-card px-4 py-3 text-sm font-semibold text-muted shadow-[inset_0_1px_0_rgba(255,255,255,0.44)]">
                BiblioBot esta leyendo el catalogo...
              </p>
            </div>
          ) : null}
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
            disabled={isSending}
            className="h-11 min-w-0 flex-1 rounded-full border border-border bg-card px-4 text-sm font-semibold text-foreground outline-none placeholder:text-muted disabled:cursor-not-allowed disabled:opacity-70 focus:border-accent"
          />
          <button
            type="submit"
            disabled={isSending || !input.trim()}
            className="h-11 rounded-full bg-accent px-4 text-xs font-black text-paper shadow-[0_10px_20px_rgba(255,96,55,0.2)] transition hover:bg-foreground disabled:cursor-not-allowed disabled:opacity-60 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent"
          >
            {isSending ? "..." : "Enviar"}
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

function ChatResponseActions({
  payload,
  onNavigate,
}: {
  payload: ChatbotResponse;
  onNavigate: (url: string | null | undefined) => void;
}) {
  const metadata = payload.context.metadata ?? {};
  const books = Array.isArray(metadata.books) ? metadata.books : [];
  const book = metadata.book;
  const stock = metadata.stock;
  const isAuthRequired = payload.context.intent === "auth_required" || payload.context.nextAction === "AUTH_REQUIRED";
  const catalogUrl = getCatalogUrl(payload);
  const productUrl = getProductUrl(payload);
  const invoiceUrl = getFirstSafeLink(payload.links) ?? getInvoiceUrl(payload);
  const cartUrl = getFirstSafeLink(payload.links) ?? "/cart";

  return (
    <div className="mt-3 space-y-3">
      {books.length > 0 ? <BookSummaryList books={books.slice(0, 4)} /> : null}
      {book ? <BookDetailSummary book={book} /> : null}
      {stock ? <StockSummary stock={stock} /> : null}

      {isAuthRequired ? (
        <div className="flex flex-wrap gap-2">
          <ActionButton label="Iniciar sesion" url={findLinkUrl(payload.links, "AUTH_LOGIN", "/auth/login")} onNavigate={onNavigate} />
          <ActionButton label="Crear cuenta" url={findLinkUrl(payload.links, "AUTH_REGISTER", "/auth/register")} onNavigate={onNavigate} />
        </div>
      ) : null}

      {payload.uiAction === "NAVIGATE_TO_CATALOG" && catalogUrl ? (
        <ActionButton label="Ver catalogo" url={catalogUrl} onNavigate={onNavigate} />
      ) : null}

      {payload.uiAction === "NAVIGATE_TO_PRODUCT" && productUrl ? (
        <ActionButton label="Ver detalle" url={productUrl} onNavigate={onNavigate} />
      ) : null}

      {payload.uiAction === "SHOW_INVOICE" && invoiceUrl ? (
        <ActionButton label="Ver factura" url={invoiceUrl} onNavigate={onNavigate} />
      ) : null}

      {payload.uiAction === "OPEN_CART" ? (
        <ActionButton label="Abrir carrito" url={cartUrl} onNavigate={onNavigate} />
      ) : null}

      {payload.uiAction === "APPLY_FILTERS" && catalogUrl ? (
        <ActionButton label="Aplicar filtros" url={catalogUrl} onNavigate={onNavigate} />
      ) : null}
    </div>
  );
}

function BookSummaryList({ books }: { books: ChatbotBookSummary[] }) {
  return (
    <div className="grid gap-2">
      {books.map((book, index) => (
        <div key={`${book.id ?? book.title ?? index}`} className="rounded-2xl border border-border/70 bg-paper/70 p-3">
          <p className="text-sm font-black text-foreground">{book.title ?? "Libro recomendado"}</p>
          <p className="mt-1 text-xs font-bold text-muted">
            {[book.author, book.genre].filter(Boolean).join(" · ") || "Catalogo BiblioBot"}
          </p>
          <div className="mt-2 flex flex-wrap gap-2 text-[0.68rem] font-black uppercase tracking-wider text-muted">
            {typeof book.price === "number" ? <span>{priceFormatter.format(book.price)}</span> : null}
            {book.available !== null && book.available !== undefined ? (
              <span>{book.available ? "Disponible" : "No disponible"}</span>
            ) : null}
          </div>
        </div>
      ))}
    </div>
  );
}

function BookDetailSummary({ book }: { book: ChatbotBookSummary }) {
  return (
    <div className="rounded-2xl border border-border/70 bg-paper/70 p-3">
      <p className="text-xs font-black uppercase tracking-widest text-accent">Detalle del libro</p>
      <p className="mt-1 text-sm font-black text-foreground">{book.title ?? "Libro"}</p>
      <p className="mt-1 text-xs font-bold text-muted">
        {[book.author, book.genre].filter(Boolean).join(" · ") || "Informacion disponible"}
      </p>
      {typeof book.price === "number" ? (
        <p className="mt-2 text-xs font-black text-foreground">{priceFormatter.format(book.price)}</p>
      ) : null}
    </div>
  );
}

function StockSummary({ stock }: { stock: NonNullable<ChatbotResponse["context"]["metadata"]>["stock"] }) {
  if (!stock) return null;

  const totalStock = stock.totalStock ?? stock.stock;

  return (
    <div className="rounded-2xl border border-border/70 bg-paper/70 p-3">
      <p className="text-xs font-black uppercase tracking-widest text-accent">Disponibilidad</p>
      <p className="mt-1 text-sm font-black text-foreground">{stock.title ?? "Libro consultado"}</p>
      <p className="mt-1 text-xs font-bold text-muted">
        {typeof totalStock === "number" ? `${totalStock} unidades disponibles` : "Disponibilidad consultada"}
      </p>
    </div>
  );
}

function ActionButton({
  label,
  url,
  onNavigate,
}: {
  label: string;
  url: string | null | undefined;
  onNavigate: (url: string | null | undefined) => void;
}) {
  const safeUrl = getSafeInternalPath(url);

  if (!safeUrl) return null;

  return (
    <button
      type="button"
      className="rounded-full bg-foreground px-3 py-2 text-xs font-black text-paper transition hover:bg-accent focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent"
      onClick={() => onNavigate(safeUrl)}
    >
      {label}
    </button>
  );
}

function getCatalogUrl(payload: ChatbotResponse): string | null {
  const metadata = payload.context.metadata ?? {};
  const linkUrl = findLinkUrl(payload.links, "CATALOG_SEARCH");

  if (linkUrl) return linkUrl;

  if (typeof metadata.frontendRoute === "string" && metadata.frontendRoute.startsWith("/")) {
    return metadata.frontendRoute;
  }

  if (typeof metadata.query === "string" && metadata.query.trim()) {
    return `/search?q=${encodeURIComponent(metadata.query.trim())}`;
  }

  return "/search";
}

function getProductUrl(payload: ChatbotResponse): string | null {
  const linkUrl = findLinkUrl(payload.links, "BOOK_DETAIL");

  if (linkUrl) return linkUrl;

  const selectedBookId = payload.context.selectedBookId;

  return selectedBookId ? `/books/${selectedBookId}` : null;
}

function getInvoiceUrl(payload: ChatbotResponse): string | null {
  const invoiceNumber = payload.context.invoiceNumber ?? payload.context.metadata?.invoiceNumber;

  return invoiceNumber ? `/dashboard?invoice=${encodeURIComponent(invoiceNumber)}` : null;
}

function findLinkUrl(links: ChatbotLink[], type: string, fallback?: string): string | null {
  return links.find((link) => link.type === type)?.url ?? fallback ?? null;
}

function getFirstSafeLink(links: ChatbotLink[]): string | null {
  for (const link of links) {
    const safeUrl = getSafeInternalPath(link.url);

    if (safeUrl) return safeUrl;
  }

  return null;
}

function getSafeInternalPath(url: string | null | undefined): string | null {
  if (!url || !url.startsWith("/") || url.startsWith("//") || url.includes("\\")) {
    return null;
  }

  if (url.startsWith("/api/") || /^\/[a-z][a-z0-9+.-]*:/i.test(url)) {
    return null;
  }

  return url;
}
