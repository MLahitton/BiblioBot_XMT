-- BiblioBot demo data insert
-- Run manually after applying migrations and starting the backend at least once,
-- so core roles/catalogs are available.
--
-- Example:
-- psql -h localhost -p 5432 -U postgres -d bibliobot -f bibliobot_Backend/Infrastructure/Persistence/Scripts/insert_demo_data.sql
--
-- Demo client password for every inserted client:
-- Cliente123!

BEGIN;

INSERT INTO roles (id, code, name, description, is_active, created_at, updated_at)
VALUES
    ('10000000-0000-0000-0000-000000000001', 'CLIENT', 'Cliente', 'Cliente final del sistema', true, NOW(), NULL)
ON CONFLICT (id) DO UPDATE
SET code = EXCLUDED.code,
    name = EXCLUDED.name,
    description = EXCLUDED.description,
    is_active = true,
    updated_at = NOW();

INSERT INTO branches (id, name, address, is_active, created_at, updated_at)
VALUES
    ('f0000000-0000-0000-0000-000000000001', 'Sucursal demo Webook', 'Inventario de prueba', true, NOW(), NULL)
ON CONFLICT (id) DO UPDATE
SET name = EXCLUDED.name,
    address = EXCLUDED.address,
    is_active = true,
    updated_at = NOW();

INSERT INTO categories (id, name, is_active, created_at, updated_at)
SELECT item.id::uuid, item.name, true, NOW(), NULL
FROM (VALUES
    ('c0000000-0000-0000-0000-000000000001', 'Ficción'),
    ('c0000000-0000-0000-0000-000000000002', 'Misterio'),
    ('c0000000-0000-0000-0000-000000000003', 'Thriller'),
    ('c0000000-0000-0000-0000-000000000004', 'Cómic y novela gráfica'),
    ('c0000000-0000-0000-0000-000000000005', 'Ciencia ficción'),
    ('c0000000-0000-0000-0000-000000000006', 'Infantil'),
    ('c0000000-0000-0000-0000-000000000007', 'Fantasía épica'),
    ('c0000000-0000-0000-0000-000000000008', 'Salud'),
    ('c0000000-0000-0000-0000-000000000009', 'Arte'),
    ('c0000000-0000-0000-0000-000000000010', 'No ficción'),
    ('c0000000-0000-0000-0000-000000000011', 'Historia'),
    ('c0000000-0000-0000-0000-000000000012', 'Idiomas'),
    ('c0000000-0000-0000-0000-000000000013', 'Cuentos ilustrados')
) AS item(id, name)
WHERE NOT EXISTS (
    SELECT 1
    FROM categories existing
    WHERE LOWER(existing.name) = LOWER(item.name)
);

UPDATE categories
SET is_active = true,
    updated_at = NOW()
WHERE name IN (
    'Ficción',
    'Misterio',
    'Thriller',
    'Cómic y novela gráfica',
    'Ciencia ficción',
    'Infantil',
    'Fantasía épica',
    'Salud',
    'Arte',
    'No ficción',
    'Historia',
    'Idiomas',
    'Cuentos ilustrados'
);

INSERT INTO publishers (id, name, is_active, created_at, updated_at)
VALUES
    ('e0000000-0000-0000-0000-000000000001', 'Planeta', true, NOW(), NULL),
    ('e0000000-0000-0000-0000-000000000002', 'Nexus Comics', true, NOW(), NULL),
    ('e0000000-0000-0000-0000-000000000003', 'Ancla Editorial', true, NOW(), NULL),
    ('e0000000-0000-0000-0000-000000000004', 'Ediciones Horizonte', true, NOW(), NULL),
    ('e0000000-0000-0000-0000-000000000005', 'Ediciones Luz', true, NOW(), NULL),
    ('e0000000-0000-0000-0000-000000000006', 'Ediciones Historia', true, NOW(), NULL),
    ('e0000000-0000-0000-0000-000000000007', 'LunaKids', true, NOW(), NULL)
ON CONFLICT (id) DO UPDATE
SET name = EXCLUDED.name,
    is_active = true,
    updated_at = NOW();

INSERT INTO authors (id, full_name, is_active, created_at, updated_at)
VALUES
    ('d0000000-0000-0000-0000-000000000001', 'Elena Montes', true, NOW(), NULL),
    ('d0000000-0000-0000-0000-000000000002', 'Clara Varela', true, NOW(), NULL),
    ('d0000000-0000-0000-0000-000000000003', 'Valeria Cruz', true, NOW(), NULL),
    ('d0000000-0000-0000-0000-000000000004', 'Lucía Valverde', true, NOW(), NULL),
    ('d0000000-0000-0000-0000-000000000005', 'Dr. Andrés Portillo', true, NOW(), NULL),
    ('d0000000-0000-0000-0000-000000000006', 'Mateo Arias', true, NOW(), NULL),
    ('d0000000-0000-0000-0000-000000000007', 'Sofía Marín', true, NOW(), NULL),
    ('d0000000-0000-0000-0000-000000000008', 'Alejandro Ferrer', true, NOW(), NULL),
    ('d0000000-0000-0000-0000-000000000009', 'Marta Gómez', true, NOW(), NULL),
    ('d0000000-0000-0000-0000-000000000010', 'Sofía Martínez', true, NOW(), NULL)
ON CONFLICT (id) DO UPDATE
SET full_name = EXCLUDED.full_name,
    is_active = true,
    updated_at = NOW();

INSERT INTO books (
    id,
    title,
    isbn,
    description,
    publisher_id,
    publication_year,
    language,
    image_url,
    price,
    is_active,
    is_deleted,
    created_at,
    updated_at,
    deleted_at
)
VALUES
    (
        'b0000000-0000-0000-0000-000000000001',
        'El eco de las sombras',
        'DEMO-978-958-000001',
        'Una mujer vuelve a un valle remoto donde los secretos familiares despiertan con cada tormenta. Un misterio íntimo sobre memoria, pérdida y aquello que el pasado insiste en susurrar.',
        'e0000000-0000-0000-0000-000000000001',
        2026,
        'es',
        'https://i.postimg.cc/KvfHCzHD/Chat-GPT-Image-8-jul-2026-08-27-55.png',
        64000,
        true,
        false,
        NOW(),
        NULL,
        NULL
    ),
    (
        'b0000000-0000-0000-0000-000000000002',
        'La casa del faro',
        'DEMO-978-958-000002',
        'Clara llega a la casa del faro buscando empezar de nuevo, pero allí descubre recuerdos y secretos que la obligan a enfrentar su pasado. En ese lugar aislado, encuentra poco a poco fuerza, calma y esperanza.',
        'e0000000-0000-0000-0000-000000000001',
        2026,
        'es',
        'https://i.postimg.cc/52qLsY7G/Chat-GPT-Image-8-jul-2026-08-35-51.png',
        58000,
        true,
        false,
        NOW(),
        NULL,
        NULL
    ),
    (
        'b0000000-0000-0000-0000-000000000003',
        'La centinela de neón',
        'DEMO-978-958-000003',
        'En una megaciudad que nunca duerme, una vigilante descubre que la tecnología que protege a la población también oculta una amenaza mayor. Acción, ciencia ficción y conspiración urbana en clave de cómic.',
        'e0000000-0000-0000-0000-000000000002',
        2024,
        'es',
        'https://i.postimg.cc/2SFh0bcM/Chat-GPT-Image-8-jul-2026-08-43-18.png',
        42000,
        true,
        false,
        NOW(),
        NULL,
        NULL
    ),
    (
        'b0000000-0000-0000-0000-000000000004',
        'La ciudad de las nubes',
        'DEMO-978-958-000004',
        'Una niña encuentra un puente hacia una ciudad suspendida entre estrellas, dragones pequeños y castillos imposibles. Una aventura fantástica sobre imaginación, valentía y sueños que aprenden a volar.',
        'e0000000-0000-0000-0000-000000000003',
        2026,
        'es',
        'https://i.postimg.cc/wxZPhvGs/Chat-GPT-Image-8-jul-2026-08-44-20.png',
        52000,
        true,
        false,
        NOW(),
        NULL,
        NULL
    ),
    (
        'b0000000-0000-0000-0000-000000000005',
        'Cuerpo y mente',
        'DEMO-978-958-000005',
        'Una guía práctica de nutrición inteligente, bienestar mental y ejercicio efectivo. Propone hábitos simples respaldados por ciencia para lograr cambios pequeños con resultados duraderos.',
        'e0000000-0000-0000-0000-000000000001',
        2026,
        'es',
        'https://i.postimg.cc/9QZKJh70/Chat-GPT-Image-8-jul-2026-09-21-07.png',
        69000,
        true,
        false,
        NOW(),
        NULL,
        NULL
    ),
    (
        'b0000000-0000-0000-0000-000000000006',
        'El último andén del tiempo',
        'DEMO-978-958-000006',
        'Un tren imposible aparece en una estación perdida y ofrece a sus pasajeros una segunda oportunidad. Una novela sobre recuerdos, destinos cruzados y las decisiones que cambian una vida.',
        'e0000000-0000-0000-0000-000000000004',
        2026,
        'es',
        'https://i.postimg.cc/vZfk3sVZ/Chat-GPT-Image-8-jul-2026-09-24-56.png',
        61000,
        true,
        false,
        NOW(),
        NULL,
        NULL
    ),
    (
        'b0000000-0000-0000-0000-000000000007',
        'El lenguaje del arte',
        'DEMO-978-958-000007',
        'Una introducción visual a la creatividad, la forma y la emoción. Recorre estilos, composición y lectura de imágenes para entender cómo el arte comunica antes que las palabras.',
        'e0000000-0000-0000-0000-000000000005',
        2026,
        'es',
        'https://i.postimg.cc/HkDPJhPN/Chat-GPT-Image-8-jul-2026-09-26-19.png',
        74000,
        true,
        false,
        NOW(),
        NULL,
        NULL
    ),
    (
        'b0000000-0000-0000-0000-000000000008',
        'Sombras de la Segunda Guerra Mundial',
        'DEMO-978-958-000008',
        'Una mirada documentada a batallas, memoria y costo humano del conflicto. Combina contexto histórico, mapas y relatos civiles para comprender las huellas de la guerra.',
        'e0000000-0000-0000-0000-000000000006',
        2026,
        'es',
        'https://i.postimg.cc/W1Y8GY9J/Chat-GPT-Image-8-jul-2026-09-29-16.png',
        82000,
        true,
        false,
        NOW(),
        NULL,
        NULL
    ),
    (
        'b0000000-0000-0000-0000-000000000009',
        'Lenguaje sin fronteras',
        'DEMO-978-958-000009',
        'Una ruta práctica para aprender idiomas, conectar culturas y transformar la comunicación diaria. Incluye vocabulario esencial, estrategias de estudio y hábitos para avanzar con confianza.',
        'e0000000-0000-0000-0000-000000000004',
        2026,
        'es',
        'https://i.postimg.cc/50B8Rsm0/Chat-GPT-Image-8-jul-2026-09-32-18.png',
        56000,
        true,
        false,
        NOW(),
        NULL,
        NULL
    ),
    (
        'b0000000-0000-0000-0000-000000000010',
        'Pequeños sueños, grandes aventuras',
        'DEMO-978-958-000010',
        'Historias tiernas para leer, soñar y crecer feliz. Cada cuento invita a imaginar mundos amables mientras acompaña a los niños en emociones, amistad y descubrimiento.',
        'e0000000-0000-0000-0000-000000000007',
        2026,
        'es',
        'https://i.postimg.cc/MGx6YtXL/Chat-GPT-Image-8-jul-2026-09-35-12.png',
        39000,
        true,
        false,
        NOW(),
        NULL,
        NULL
    )
ON CONFLICT (id) DO UPDATE
SET title = EXCLUDED.title,
    isbn = EXCLUDED.isbn,
    description = EXCLUDED.description,
    publisher_id = EXCLUDED.publisher_id,
    publication_year = EXCLUDED.publication_year,
    language = EXCLUDED.language,
    image_url = EXCLUDED.image_url,
    price = EXCLUDED.price,
    is_active = true,
    is_deleted = false,
    updated_at = NOW(),
    deleted_at = NULL;

INSERT INTO book_authors (book_id, author_id)
VALUES
    ('b0000000-0000-0000-0000-000000000001', 'd0000000-0000-0000-0000-000000000001'),
    ('b0000000-0000-0000-0000-000000000002', 'd0000000-0000-0000-0000-000000000002'),
    ('b0000000-0000-0000-0000-000000000003', 'd0000000-0000-0000-0000-000000000003'),
    ('b0000000-0000-0000-0000-000000000004', 'd0000000-0000-0000-0000-000000000004'),
    ('b0000000-0000-0000-0000-000000000005', 'd0000000-0000-0000-0000-000000000005'),
    ('b0000000-0000-0000-0000-000000000006', 'd0000000-0000-0000-0000-000000000006'),
    ('b0000000-0000-0000-0000-000000000007', 'd0000000-0000-0000-0000-000000000007'),
    ('b0000000-0000-0000-0000-000000000008', 'd0000000-0000-0000-0000-000000000008'),
    ('b0000000-0000-0000-0000-000000000009', 'd0000000-0000-0000-0000-000000000009'),
    ('b0000000-0000-0000-0000-000000000010', 'd0000000-0000-0000-0000-000000000010')
ON CONFLICT DO NOTHING;

INSERT INTO book_categories (book_id, category_id)
SELECT item.book_id::uuid, category.id
FROM (VALUES
    ('b0000000-0000-0000-0000-000000000001', 'Ficción'),
    ('b0000000-0000-0000-0000-000000000001', 'Misterio'),
    ('b0000000-0000-0000-0000-000000000002', 'Ficción'),
    ('b0000000-0000-0000-0000-000000000002', 'Thriller'),
    ('b0000000-0000-0000-0000-000000000003', 'Cómic y novela gráfica'),
    ('b0000000-0000-0000-0000-000000000003', 'Ciencia ficción'),
    ('b0000000-0000-0000-0000-000000000004', 'Infantil'),
    ('b0000000-0000-0000-0000-000000000004', 'Fantasía épica'),
    ('b0000000-0000-0000-0000-000000000005', 'Salud'),
    ('b0000000-0000-0000-0000-000000000006', 'Ficción'),
    ('b0000000-0000-0000-0000-000000000006', 'Ciencia ficción'),
    ('b0000000-0000-0000-0000-000000000007', 'Arte'),
    ('b0000000-0000-0000-0000-000000000008', 'No ficción'),
    ('b0000000-0000-0000-0000-000000000008', 'Historia'),
    ('b0000000-0000-0000-0000-000000000009', 'Idiomas'),
    ('b0000000-0000-0000-0000-000000000010', 'Infantil'),
    ('b0000000-0000-0000-0000-000000000010', 'Cuentos ilustrados')
) AS item(book_id, category_name)
JOIN categories category ON LOWER(category.name) = LOWER(item.category_name)
ON CONFLICT DO NOTHING;

INSERT INTO inventory_stocks (id, book_id, branch_id, current_stock, min_stock, updated_at)
VALUES
    ('a0000000-0000-0000-0000-000000000001', 'b0000000-0000-0000-0000-000000000001', 'f0000000-0000-0000-0000-000000000001', 18, 3, NOW()),
    ('a0000000-0000-0000-0000-000000000002', 'b0000000-0000-0000-0000-000000000002', 'f0000000-0000-0000-0000-000000000001', 14, 3, NOW()),
    ('a0000000-0000-0000-0000-000000000003', 'b0000000-0000-0000-0000-000000000003', 'f0000000-0000-0000-0000-000000000001', 25, 5, NOW()),
    ('a0000000-0000-0000-0000-000000000004', 'b0000000-0000-0000-0000-000000000004', 'f0000000-0000-0000-0000-000000000001', 21, 4, NOW()),
    ('a0000000-0000-0000-0000-000000000005', 'b0000000-0000-0000-0000-000000000005', 'f0000000-0000-0000-0000-000000000001', 17, 3, NOW()),
    ('a0000000-0000-0000-0000-000000000006', 'b0000000-0000-0000-0000-000000000006', 'f0000000-0000-0000-0000-000000000001', 13, 3, NOW()),
    ('a0000000-0000-0000-0000-000000000007', 'b0000000-0000-0000-0000-000000000007', 'f0000000-0000-0000-0000-000000000001', 10, 2, NOW()),
    ('a0000000-0000-0000-0000-000000000008', 'b0000000-0000-0000-0000-000000000008', 'f0000000-0000-0000-0000-000000000001', 12, 2, NOW()),
    ('a0000000-0000-0000-0000-000000000009', 'b0000000-0000-0000-0000-000000000009', 'f0000000-0000-0000-0000-000000000001', 18, 3, NOW()),
    ('a0000000-0000-0000-0000-000000000010', 'b0000000-0000-0000-0000-000000000010', 'f0000000-0000-0000-0000-000000000001', 30, 5, NOW())
ON CONFLICT (id) DO UPDATE
SET current_stock = EXCLUDED.current_stock,
    min_stock = EXCLUDED.min_stock,
    updated_at = NOW();

INSERT INTO users (
    id,
    full_name,
    email,
    password_hash,
    phone,
    document_number,
    is_active,
    is_deleted,
    created_at,
    updated_at,
    deleted_at
)
VALUES
    ('91000000-0000-0000-0000-000000000001', 'Camila Rojas', 'camila.rojas.demo@webook.test', 'PBKDF2:120000:QmlibGlvQm90U2FsdCEhIQ==:XVfJvmvxrxyc3KZ5n8kH8Czx3N8aTB6HeTxf1LN+9l0=', '3001001001', '1001001001', true, false, NOW(), NULL, NULL),
    ('91000000-0000-0000-0000-000000000002', 'Juan Sebastián Mora', 'juan.mora.demo@webook.test', 'PBKDF2:120000:QmlibGlvQm90U2FsdCEhIQ==:XVfJvmvxrxyc3KZ5n8kH8Czx3N8aTB6HeTxf1LN+9l0=', '3001001002', '1001001002', true, false, NOW(), NULL, NULL),
    ('91000000-0000-0000-0000-000000000003', 'Laura Méndez', 'laura.mendez.demo@webook.test', 'PBKDF2:120000:QmlibGlvQm90U2FsdCEhIQ==:XVfJvmvxrxyc3KZ5n8kH8Czx3N8aTB6HeTxf1LN+9l0=', '3001001003', '1001001003', true, false, NOW(), NULL, NULL),
    ('91000000-0000-0000-0000-000000000004', 'Andrés Castillo', 'andres.castillo.demo@webook.test', 'PBKDF2:120000:QmlibGlvQm90U2FsdCEhIQ==:XVfJvmvxrxyc3KZ5n8kH8Czx3N8aTB6HeTxf1LN+9l0=', '3001001004', '1001001004', true, false, NOW(), NULL, NULL),
    ('91000000-0000-0000-0000-000000000005', 'Valentina Torres', 'valentina.torres.demo@webook.test', 'PBKDF2:120000:QmlibGlvQm90U2FsdCEhIQ==:XVfJvmvxrxyc3KZ5n8kH8Czx3N8aTB6HeTxf1LN+9l0=', '3001001005', '1001001005', true, false, NOW(), NULL, NULL),
    ('91000000-0000-0000-0000-000000000006', 'Felipe Navarro', 'felipe.navarro.demo@webook.test', 'PBKDF2:120000:QmlibGlvQm90U2FsdCEhIQ==:XVfJvmvxrxyc3KZ5n8kH8Czx3N8aTB6HeTxf1LN+9l0=', '3001001006', '1001001006', true, false, NOW(), NULL, NULL),
    ('91000000-0000-0000-0000-000000000007', 'Isabella Pardo', 'isabella.pardo.demo@webook.test', 'PBKDF2:120000:QmlibGlvQm90U2FsdCEhIQ==:XVfJvmvxrxyc3KZ5n8kH8Czx3N8aTB6HeTxf1LN+9l0=', '3001001007', '1001001007', true, false, NOW(), NULL, NULL),
    ('91000000-0000-0000-0000-000000000008', 'Mateo Hernández', 'mateo.hernandez.demo@webook.test', 'PBKDF2:120000:QmlibGlvQm90U2FsdCEhIQ==:XVfJvmvxrxyc3KZ5n8kH8Czx3N8aTB6HeTxf1LN+9l0=', '3001001008', '1001001008', true, false, NOW(), NULL, NULL),
    ('91000000-0000-0000-0000-000000000009', 'Natalia Ruiz', 'natalia.ruiz.demo@webook.test', 'PBKDF2:120000:QmlibGlvQm90U2FsdCEhIQ==:XVfJvmvxrxyc3KZ5n8kH8Czx3N8aTB6HeTxf1LN+9l0=', '3001001009', '1001001009', true, false, NOW(), NULL, NULL),
    ('91000000-0000-0000-0000-000000000010', 'Tomás Vega', 'tomas.vega.demo@webook.test', 'PBKDF2:120000:QmlibGlvQm90U2FsdCEhIQ==:XVfJvmvxrxyc3KZ5n8kH8Czx3N8aTB6HeTxf1LN+9l0=', '3001001010', '1001001010', true, false, NOW(), NULL, NULL)
ON CONFLICT (email) DO UPDATE
SET full_name = EXCLUDED.full_name,
    password_hash = EXCLUDED.password_hash,
    phone = EXCLUDED.phone,
    document_number = EXCLUDED.document_number,
    is_active = true,
    is_deleted = false,
    updated_at = NOW(),
    deleted_at = NULL;

INSERT INTO user_roles (user_id, role_id, created_at)
SELECT users.id, roles.id, NOW()
FROM users
JOIN roles ON roles.code = 'CLIENT'
WHERE users.email IN (
    'camila.rojas.demo@webook.test',
    'juan.mora.demo@webook.test',
    'laura.mendez.demo@webook.test',
    'andres.castillo.demo@webook.test',
    'valentina.torres.demo@webook.test',
    'felipe.navarro.demo@webook.test',
    'isabella.pardo.demo@webook.test',
    'mateo.hernandez.demo@webook.test',
    'natalia.ruiz.demo@webook.test',
    'tomas.vega.demo@webook.test'
)
ON CONFLICT DO NOTHING;

COMMIT;
