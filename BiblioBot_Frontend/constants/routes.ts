export const routes = {
  home: "/",
  categories: "/#categorias",
  featured: "/#destacados",
  benefits: "/#beneficios",
  experience: "/#experiencia",
  cart: "/cart",
  checkout: "/checkout",
  auth: "/auth",
  dashboard: "/dashboard",
} as const;

export type AppRoute = (typeof routes)[keyof typeof routes];
