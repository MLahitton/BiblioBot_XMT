export const API_ENDPOINTS = {
  auth: {
    register: "/api/auth/register",
    login: "/api/auth/login",
    refresh: "/api/auth/refresh",
    me: "/api/auth/me",
  },
  books: "/api/libros",
  bookById: (id: string) => `/api/libros/${id}`,
  bookSearch: "/api/libros/search",
  categories: "/api/categorias",
  cart: {
    bySession: (sessionId: string) => `/api/carrito/${sessionId}`,
    items: "/api/carrito",
    item: (sessionId: string, bookId: string) => `/api/carrito/${sessionId}/items/${bookId}`,
    clear: (sessionId: string) => `/api/carrito/${sessionId}`,
  },
  sales: "/api/ventas",
} as const;
