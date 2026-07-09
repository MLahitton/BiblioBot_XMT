# Assets de imagenes de Webook

El proyecto usa imagenes propias para el hero, el logo, BiblioBot y portadas
editoriales neutrales. No agregues fotografias de stock ni imagenes de terceros
sin licencia.

Carpetas recomendadas:

- `public/images/editorial/`: librerias, mesas de lectura y colecciones curadas.
- `public/images/library/`: estanterias, salas de lectura y espacios calidos.
- `public/images/backgrounds/`: fondos amplios para hero y secciones.
- `public/images/books/`: portadas o mockups neutrales de libros.
- `public/images/biblioBot/`: logo, icono y recortes transparentes del asistente.

Requisitos de optimizacion:

- Preferir `.avif`, `.webp` o SVG ligero segun el tipo de asset.
- Mantener imagenes hero cerca de 1800px de ancho o menos.
- Mantener portadas de producto cerca de 600px de ancho si son raster.
- Usar nombres descriptivos como `hero-biblioteca-luminosa.webp`.
- Renderizar imagenes importantes con `next/image`, `sizes` y `alt`.

Assets activos:

- `public/images/generated/hero-library-realistic.webp`
- `public/images/generated/hero-reading-lounge.webp`
- `public/images/generated/hero-bookstore-corner.webp`
- `public/images/generated/hero-private-library.webp`
- `public/images/books/book-01.svg` a `book-08.svg`
- `public/images/biblioBot/cutouts/Logo_Webook-cutout.png`
- `public/images/biblioBot/cutouts/icono_bibliobot-cutout.png`

Los componentes leen las rutas desde datos y servicios, asi que una futura API
puede reemplazar los mocks sin cambiar los componentes visuales.
