export type ParallaxStage = {
  id: string;
  sectionName: string;
  scrollRange: string;
  bookPosition: {
    x: string;
    y: string;
  };
  bookRotation: {
    x: number;
    y: number;
    z: number;
  };
  bookScale: number;
  contentBehavior: string;
  description: string;
};

export const parallaxPlan: ParallaxStage[] = [
  {
    id: "hero",
    sectionName: "Hero",
    scrollRange: "0% - 18%",
    bookPosition: {
      x: "center",
      y: "center",
    },
    bookRotation: {
      x: 5,
      y: -12,
      z: -4,
    },
    bookScale: 1,
    contentBehavior: "Titulo y acciones aparecen con fade suave.",
    description: "Libro centrado, grande y flotando como pieza principal.",
  },
  {
    id: "categories",
    sectionName: "Categorías",
    scrollRange: "18% - 38%",
    bookPosition: {
      x: "right 18%",
      y: "center",
    },
    bookRotation: {
      x: 8,
      y: -24,
      z: 6,
    },
    bookScale: 0.86,
    contentBehavior: "Categorías entran en stagger desde abajo.",
    description: "El libro se desplaza hacia la derecha y deja espacio a cards compactas.",
  },
  {
    id: "featured-books",
    sectionName: "Libros destacados",
    scrollRange: "38% - 62%",
    bookPosition: {
      x: "left 12%",
      y: "top 24%",
    },
    bookRotation: {
      x: 2,
      y: 18,
      z: -8,
    },
    bookScale: 0.72,
    contentBehavior: "Cards de productos aparecen por grupos con profundidad leve.",
    description: "El libro reduce escala para acompanar el grid de productos.",
  },
  {
    id: "benefits",
    sectionName: "Beneficios",
    scrollRange: "62% - 82%",
    bookPosition: {
      x: "center",
      y: "bottom 12%",
    },
    bookRotation: {
      x: 0,
      y: -6,
      z: 2,
    },
    bookScale: 0.58,
    contentBehavior: "Beneficios entran con fade y blur reducido.",
    description: "El libro se integra al fondo con presencia mas atmosferica.",
  },
  {
    id: "final-cta",
    sectionName: "CTA final",
    scrollRange: "82% - 100%",
    bookPosition: {
      x: "center",
      y: "center",
    },
    bookRotation: {
      x: 4,
      y: -10,
      z: 0,
    },
    bookScale: 0.9,
    contentBehavior: "CTA final aparece sobre una composicion limpia y enfocada.",
    description: "El libro vuelve al centro como cierre visual decorativo.",
  },
];
