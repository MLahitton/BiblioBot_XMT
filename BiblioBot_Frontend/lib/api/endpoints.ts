export const API_ENDPOINTS = {
  chat: {
    publicMessage: "/api/chat/public-message",
    message: "/api/chat/message",
  },
  auth: {
    register: "/api/auth/register",
    login: "/api/auth/login",
    refresh: "/api/auth/refresh",
    me: "/api/auth/me",
  },
  books: "/api/libros",
  bookById: (id: string) => `/api/libros/${id}`,
  bookReviews: (id: string) => `/api/libros/${id}/resenas`,
  bookSearch: "/api/libros/search",
  categories: "/api/categorias",
  cart: {
    bySession: (sessionId: string) => `/api/carrito/${sessionId}`,
    items: "/api/carrito",
    item: (sessionId: string, bookId: string) => `/api/carrito/${sessionId}/items/${bookId}`,
    clear: (sessionId: string) => `/api/carrito/${sessionId}`,
  },
  favorites: {
    list: "/api/libros/favoritos",
    status: (bookId: string) => `/api/libros/${bookId}/favorito`,
    item: (bookId: string) => `/api/libros/${bookId}/favorito`,
  },
  inventory: "/api/inventario",
  usersLookup: "/api/busquedas/usuarios",
  adminProducts: {
    list: "/api/admin/productos",
    item: (id: string) => `/api/admin/productos/${id}`,
    activate: (id: string) => `/api/admin/productos/${id}/activar`,
    deactivate: (id: string) => `/api/admin/productos/${id}/desactivar`,
  },
  adminUsers: {
    list: "/api/admin/usuarios",
    item: (id: string) => `/api/admin/usuarios/${id}`,
    activate: (id: string) => `/api/admin/usuarios/${id}/activar`,
    deactivate: (id: string) => `/api/admin/usuarios/${id}/desactivar`,
  },
  adminRoles: "/api/admin/roles",
  sales: "/api/ventas",
} as const;

