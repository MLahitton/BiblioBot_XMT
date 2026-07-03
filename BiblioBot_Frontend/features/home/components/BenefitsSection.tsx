import Image from "next/image";
import { landingCopy } from "../data/landing-copy.data";
import { ScrollReveal } from "./ScrollReveal";

const benefits = [
  {
    title: "Recomendaciones personalizadas",
    description: "Colecciones pensadas para tu ritmo, tus generos y tu momento.",
    icon: "/icons/star.svg",
  },
  {
    title: "Compra facil y rapida",
    description: "Una experiencia clara para descubrir, comparar y decidir.",
    icon: "/icons/cart.svg",
  },
  {
    title: "Categorias curadas",
    description: "Rutas editoriales para pasar de la curiosidad al libro correcto.",
    icon: "/icons/category.svg",
  },
  {
    title: "Lecturas para cada momento",
    description: "Libros para aprender, imaginar, crear o bajar el ritmo.",
    icon: "/icons/book.svg",
  },
];

export function BenefitsSection() {
  return (
    <section id="beneficios" className="px-6 py-[72px]">
      <div className="mx-auto max-w-6xl">
        <div className="grid gap-10 lg:grid-cols-[0.9fr_1.1fr] lg:items-end">
          <ScrollReveal>
            <p className="text-sm font-medium uppercase tracking-[0.2em] text-accent">
              {landingCopy.benefits.eyebrow}
            </p>
            <h2 className="mt-3 text-3xl font-semibold leading-tight text-foreground sm:text-4xl">
              {landingCopy.benefits.title}
            </h2>
            <p className="mt-4 max-w-xl text-base leading-7 text-muted">
              Diseno editorial, navegacion sencilla y senales claras para comprar
              libros con calma.
            </p>
          </ScrollReveal>
          <div className="grid gap-4 sm:grid-cols-2">
            {benefits.map((benefit, index) => (
              <ScrollReveal key={benefit.title} delay={index * 0.07} y={22}>
                <article className="rounded-xl border border-border bg-card p-5 backdrop-blur-xl transition duration-300 hover:-translate-y-1 hover:border-accent/40 hover:bg-white/[0.1]">
                  <div className="flex h-11 w-11 items-center justify-center rounded-xl bg-accent/10">
                    <Image src={benefit.icon} alt="" width={25} height={25} />
                  </div>
                  <h3 className="mt-4 text-base font-semibold text-foreground">
                    {benefit.title}
                  </h3>
                  <p className="mt-2 text-sm leading-6 text-muted">
                    {benefit.description}
                  </p>
                </article>
              </ScrollReveal>
            ))}
          </div>
        </div>
      </div>
    </section>
  );
}
