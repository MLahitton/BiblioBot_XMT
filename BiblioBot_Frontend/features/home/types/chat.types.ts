export type ChatbotUiAction =
  | "NAVIGATE_TO_CATALOG"
  | "NAVIGATE_TO_PRODUCT"
  | "SHOW_INVOICE"
  | "OPEN_CART"
  | "APPLY_FILTERS"
  | "NONE"
  | string;

export type ChatbotLink = {
  label: string;
  url: string;
  type: string;
};

export type ChatbotBookSummary = {
  id?: string | null;
  title?: string | null;
  author?: string | null;
  genre?: string | null;
  price?: number | null;
  available?: boolean | null;
  totalStock?: number | null;
};

export type ChatbotStockSummary = {
  bookId?: string | null;
  title?: string | null;
  totalStock?: number | null;
  stock?: number | null;
  available?: boolean | null;
  status?: string | null;
};

export type ChatbotMetadata = {
  sessionId?: string | null;
  detectedIntent?: string | null;
  guest?: boolean | null;
  query?: string | null;
  resultCount?: number | null;
  frontendRoute?: string | null;
  filters?: Record<string, string> | null;
  books?: ChatbotBookSummary[] | null;
  book?: ChatbotBookSummary | null;
  stock?: ChatbotStockSummary | null;
  invoiceNumber?: string | null;
  saleId?: string | null;
  [key: string]: unknown;
};

export type ChatbotContext = {
  intent?: string | null;
  requiresConfirmation?: boolean | null;
  actionRef?: string | null;
  invoiceNumber?: string | null;
  saleOrigin?: string | null;
  nextAction?: string | null;
  selectedBookId?: string | null;
  saleId?: string | null;
  selectedBranchId?: string | null;
  metadata?: ChatbotMetadata | null;
  [key: string]: unknown;
};

export type ChatbotResponse = {
  response: string;
  state: string;
  links: ChatbotLink[];
  uiAction: ChatbotUiAction;
  context: ChatbotContext;
};

export type SendChatMessageResult = {
  response: ChatbotResponse;
  sessionId: string;
  authenticated: boolean;
};
