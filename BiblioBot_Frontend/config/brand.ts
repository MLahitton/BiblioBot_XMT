export const brandConfig = {
  name: "Webook",
  slogan: "Libros simples, claros y curados.",
  description:
    "Un ecommerce claro y minimalista para descubrir libros con imágenes realistas y una compra directa.",
  visualStyle: {
    mood: "Minimalista, luminoso, cálido y cercano a un marketplace premium.",
    surface:
      "Morning Snow y Amazon Mist como base, Black Kite para texto, Toxic Orange para acción, Garnet y Aqua Mist para categorías.",
    motion:
      "Entradas sutiles y ligeras, sin parallax protagonista para mantener el home despejado.",
  },
  colors: {
    morningSnow: "#F5F4ED",
    amazonMist: "#ECECDC",
    aquaMist: "#A0C9CB",
    toxicOrange: "#FF6037",
    blackKite: "#351E1C",
    garnet: "#733635",
  },
  semantic: {
    pageBackground: "bg-background-soft",
    elevatedSurface: "bg-background",
    productSurface: "bg-card",
    primaryText: "text-foreground",
    secondaryText: "text-muted",
    accentText: "text-accent",
    softBorder: "border-border",
    focusRing: "focus-visible:ring-accent",
  },
} as const;

export type BrandConfig = typeof brandConfig;
