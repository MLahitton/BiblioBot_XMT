import { landingCopy } from "../data/landing-copy.data";

export function FinalCtaSection() {
  return (
    <footer className="px-6 pb-10 pt-4 lg:px-10">
      <section className="relative grid gap-8 rounded-xl bg-[linear-gradient(145deg,#351E1C,#733635)] px-6 py-8 text-paper shadow-[0_22px_54px_rgba(53,30,28,0.24)] sm:px-8 lg:grid-cols-[1fr_0.8fr] lg:items-end">
        <div className="relative z-10">
          <h2 className="max-w-md text-4xl font-black leading-[1.02] sm:text-5xl">
            {landingCopy.finalCta.title}
          </h2>
          <form className="mt-8 flex max-w-xs items-center rounded-full bg-white p-1">
            <label className="sr-only" htmlFor="newsletter-email">
              Correo electrónico
            </label>
            <input
              id="newsletter-email"
              type="email"
              placeholder="Tu correo"
              className="min-w-0 flex-1 bg-transparent px-4 text-xs font-bold text-foreground outline-none placeholder:text-muted"
            />
            <button
              type="button"
              className="rounded-full bg-accent px-5 py-2 text-xs font-bold text-paper shadow-[0_8px_18px_rgba(255,96,55,0.2)]"
            >
              {landingCopy.finalCta.action}
            </button>
          </form>
        </div>
        <div className="max-w-sm lg:justify-self-end">
          <p className="text-sm font-extrabold">Webook para lectores curiosos</p>
          <p className="mt-3 text-xs font-medium leading-6 text-paper/72">
            {landingCopy.finalCta.description}
          </p>
        </div>
      </section>

      <div className="grid gap-10 border-b border-border px-2 py-12 sm:grid-cols-3">
        <div>
          <h3 className="text-base font-extrabold">Acerca de</h3>
          <ul className="mt-5 space-y-3 text-sm font-semibold text-muted">
            <li>Blog</li>
            <li>Conoce al equipo</li>
            <li>Contacto</li>
          </ul>
        </div>
        <div>
          <h3 className="text-base font-extrabold">Soporte</h3>
          <ul className="mt-5 space-y-3 text-sm font-semibold text-muted">
            <li>Contacto</li>
            <li>Envíos</li>
            <li>Devoluciones</li>
            <li>Preguntas frecuentes</li>
          </ul>
        </div>
        <div className="sm:justify-self-end sm:text-right">
          <h3 className="text-base font-extrabold">Redes sociales</h3>
          <div className="mt-5 flex gap-3 sm:justify-end">
            {["X", "f", "in", "ig"].map((label) => (
              <span
                key={label}
                className="flex h-9 min-w-9 items-center justify-center rounded-full bg-foreground px-2 text-xs font-black text-paper shadow-[0_8px_18px_rgba(53,30,28,0.12)]"
              >
                {label}
              </span>
            ))}
          </div>
        </div>
      </div>

      <div className="flex flex-col gap-4 px-2 pt-5 text-xs font-semibold text-muted sm:flex-row sm:items-center sm:justify-between">
        <p>Derechos de autor 2026 Webook. Todos los derechos reservados.</p>
        <div className="flex gap-8">
          <span>Términos de servicio</span>
          <span>Política de privacidad</span>
        </div>
      </div>
    </footer>
  );
}
