export const routes = {
  home: "/",
  categories: "/#categorias",
  featured: "/#destacados",
  benefits: "/#beneficios",
  experience: "/#experiencia",
  cart: "/cart",
  favorites: "/favorites",
  checkout: "/checkout",
  auth: "/auth",
  dashboard: "/dashboard",
  adminInventory: "/admin/inventario",
  adminUsers: "/admin/usuarios",
} as const;

export type AppRoute = (typeof routes)[keyof typeof routes];
