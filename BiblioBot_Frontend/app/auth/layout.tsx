import Image from "next/image";
import Link from "next/link";

export default function AuthLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <div className="relative flex min-h-screen flex-col items-center justify-center overflow-hidden bg-background px-4 py-12 sm:px-6 lg:px-8">
      {/* Decorative background matching Webook style */}
      <div className="absolute inset-0 z-0 bg-gradient-to-b from-[#f2ead8]/50 to-[#fdfbf7]" />
      <div className="absolute -left-20 top-20 z-0 h-96 w-96 rounded-full bg-accent/5 blur-[100px]" />
      <div className="absolute -right-20 bottom-20 z-0 h-96 w-96 rounded-full bg-[#a0c9cb]/10 blur-[100px]" />

      <div className="relative z-10 flex w-full max-w-[400px] flex-col items-center">
        <Link
          href="/"
          className="mb-8 rounded-lg outline-none transition-transform hover:scale-105 focus-visible:ring-2 focus-visible:ring-foreground"
          aria-label="Volver a Webook inicio"
        >
          <Image
            src="/images/biblioBot/cutouts/Logo_Webook-cutout.png"
            alt="Webook"
            width={180}
            height={64}
            className="h-12 w-auto object-contain drop-shadow-[0_8px_12px_rgba(53,30,28,0.12)]"
            priority
          />
        </Link>

        {children}
      </div>
    </div>
  );
}
