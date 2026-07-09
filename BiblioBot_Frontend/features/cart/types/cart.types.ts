export type CartItem = {
  id: string;
  bookId: string;
  bookTitle: string;
  isbn?: string | null;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
  imageUrl?: string | null;
};

export type Cart = {
  id?: string | null;
  sessionId: string;
  userId?: string | null;
  status: string;
  items: CartItem[];
  totalItems: number;
  subtotal: number;
};

export type AddOrUpdateCartItemPayload = {
  sessionId: string;
  bookId: string;
  quantity: number;
  branchId?: string | null;
};

export type SaleDetail = {
  id: string;
  bookId: string;
  bookTitleSnapshot: string;
  isbnSnapshot?: string | null;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
};

export type Sale = {
  id: string;
  customerId: string;
  customerName?: string | null;
  actorId: string;
  actorName?: string | null;
  branchId?: string | null;
  branchName?: string | null;
  statusCode: string;
  statusName: string;
  originCode: string;
  originName: string;
  subtotal: number;
  taxTotal: number;
  total: number;
  createdAt: string;
  confirmedAt?: string | null;
  details: SaleDetail[];
};
