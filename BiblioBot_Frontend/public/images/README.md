# Assets de imágenes de Webook

El proyecto usa imágenes realistas generadas como placeholders y queda listo
para recibir fotografía editorial real, licenciada y optimizada.

No agregues portadas, fotografías de stock ni imágenes de terceros sin licencia.

Carpetas recomendadas:

- `public/images/editorial/`: librerías, mesas de lectura y colecciones curadas.
- `public/images/library/`: estanterías, salas de lectura y espacios cálidos.
- `public/images/backgrounds/`: fondos amplios para hero y secciones.
- `public/images/books/`: portadas o mockups neutrales de libros.

Requisitos de optimización:

- Preferir `.avif` o `.webp`.
- Mantener imágenes hero cerca de 1800px de ancho o menos.
- Mantener portadas de producto cerca de 600px de ancho.
- Usar nombres descriptivos como `hero-biblioteca-luminosa.webp`.
- Renderizar imágenes importantes con `next/image`, `sizes` y `alt`.

Assets generados actuales:

- `public/images/generated/hero-library-realistic.webp`
- `public/images/generated/book-ivory-realistic.webp`
- `public/images/generated/book-charcoal-open-realistic.webp`
- `public/images/generated/book-stack-realistic.webp`
- `public/images/generated/book-sage-realistic.webp`

Los PNG originales permanecen en la cache de imágenes generadas de Codex. El
proyecto conserva solo los WebP optimizados.

Los componentes leen las rutas desde datos y servicios, así que una futura API
puede reemplazar los mocks sin cambiar los componentes visuales.
