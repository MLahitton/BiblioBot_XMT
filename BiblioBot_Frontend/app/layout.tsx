import type { Metadata } from "next";
import "./globals.css";

export const metadata: Metadata = {
  title: "BiblioBot Frontend",
  description: "Frontend application for BiblioBot.",
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
