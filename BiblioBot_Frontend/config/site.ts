import { routes } from "@/constants/routes";

export const siteConfig = {
  name: "Webook",
  description:
    "Ecommerce moderno de libros con recomendaciones, colecciones curadas y una experiencia visual inmersiva.",
  url: "https://webook.example.com",
  keywords: [
    "libros",
    "ecommerce",
    "lectura",
    "librería online",
    "recomendaciones",
    "autores",
    "Webook",
  ],
  author: "Webook Team",
  navItems: [
    {
      label: "Inicio",
      href: routes.home,
    },
    {
      label: "Categorías",
      href: routes.categories,
    },
    {
      label: "Destacados",
      href: routes.featured,
    },
    {
      label: "Beneficios",
      href: routes.benefits,
    },
  ],
  socialLinks: {
    instagram: "https://instagram.com/webook",
    x: "https://x.com/webook",
    linkedin: "https://linkedin.com/company/webook",
  },
  ecommerce: {
    currency: "USD",
    defaultLocale: "es-CO",
    shippingMessage: "Envíos seleccionados y recomendaciones hechas para ti.",
    supportEmail: "support@webook.example.com",
  },
} as const;

export type SiteConfig = typeof siteConfig;
