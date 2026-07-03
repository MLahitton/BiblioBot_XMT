import type { Metadata } from "next";
import "./globals.css";

export const metadata: Metadata = {
  title: "Webook | Ecommerce moderno de libros",
  description:
    "Descubre libros, categorías curadas y recomendaciones editoriales en Webook.",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="es">
      <body>{children}</body>
    </html>
  );
}
