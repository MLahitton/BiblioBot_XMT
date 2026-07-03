export const uiConfig = {
  maxContentWidth: "1180px",
  headerHeight: "72px",
  cardRadius: "0.75rem",
  glassBlur: "18px",
  parallaxBookMaxWidth: "420px",
  breakpoints: {
    mobile: 360,
    tablet: 768,
    laptop: 1024,
    desktop: 1280,
  },
  zIndex: {
    base: 1,
    floatingBook: 20,
    header: 50,
    modal: 100,
  },
} as const;
