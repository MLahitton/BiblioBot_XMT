export const brandConfig = {
  name: "Webook",
  slogan: "Lecturas que encuentran su momento.",
  description:
    "Una librería online moderna para descubrir historias, autores e ideas con una experiencia visual premium.",
  visualStyle: {
    mood: "Moderno, cinematográfico, minimalista y elegante.",
    surface:
      "Dark UI con detalles glassmorphism, brillos suaves y fondos inspirados en papel editorial.",
    motion:
      "Preparado para un libro flotante con desplazamientos suaves, profundidad y aparición gradual de contenido.",
  },
  colors: {
    background: "#14110F",
    backgroundSoft: "#1D1915",
    paper: "#F4E8D3",
    warmWhite: "#FFF8EC",
    text: "#FFF6E8",
    textMuted: "#C9BCA7",
    accent: "#D7A74F",
    accentSoft: "#F1CF88",
    coffee: "#3A271C",
    card: "rgba(255, 248, 236, 0.08)",
    cardSolid: "#211B16",
    border: "rgba(244, 232, 211, 0.16)",
    success: "#8FCB9B",
    warning: "#E5B460",
    danger: "#D96C5F",
  },
  semantic: {
    pageBackground: "bg-background",
    elevatedSurface: "bg-card-solid/80",
    glassSurface: "bg-white/[0.08] backdrop-blur-xl",
    primaryText: "text-foreground",
    secondaryText: "text-muted",
    accentText: "text-accent",
    softBorder: "border-border",
    focusRing: "focus-visible:ring-accent",
  },
} as const;

export type BrandConfig = typeof brandConfig;
