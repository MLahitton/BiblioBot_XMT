import { routes } from "@/constants/routes";
import { defaultCurrency, defaultPriceLocale } from "@/constants/currency";

export const siteConfig = {
  name: "Webook",
  description:
    "Ecommerce minimalista de libros con recomendaciones, colecciones curadas e imágenes realistas.",
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
  author: "Equipo Webook",
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
    currency: defaultCurrency,
    defaultLocale: defaultPriceLocale,
    shippingMessage: "Envíos seleccionados y recomendaciones hechas para ti.",
    supportEmail: "support@webook.example.com",
  },
} as const;

export type SiteConfig = typeof siteConfig;
