BRANCHES = [
    {"id": "branch-north", "name": "Sede Norte"},
    {"id": "branch-center", "name": "Sede Centro"},
]

BOOKS = [
    {
        "id": "book-001",
        "title": "El Archivo de las Tormentas",
        "author": "Brandon Sanderson",
        "genre": "fantasia",
        "price": 92000,
        "description": "Novela de fantasia epica con mundos extensos y conflictos politicos.",
        "available": True,
        "stockByBranch": {"branch-north": 4, "branch-center": 2},
    },
    {
        "id": "book-002",
        "title": "La Ciudad de los Libros Perdidos",
        "author": "Marina Rios",
        "genre": "fantasia",
        "price": 58000,
        "description": "Aventura fantastica sobre bibliotecas secretas y mapas imposibles.",
        "available": True,
        "stockByBranch": {"branch-north": 1, "branch-center": 0},
    },
    {
        "id": "book-003",
        "title": "Python Practico",
        "author": "Laura Mendez",
        "genre": "programacion",
        "price": 75000,
        "description": "Guia practica para construir aplicaciones con Python moderno.",
        "available": True,
        "stockByBranch": {"branch-north": 6, "branch-center": 3},
    },
    {
        "id": "book-004",
        "title": "Arquitectura Limpia para APIs",
        "author": "Diego Salazar",
        "genre": "software",
        "price": 81000,
        "description": "Patrones para disenar APIs mantenibles y bien separadas.",
        "available": True,
        "stockByBranch": {"branch-north": 2, "branch-center": 2},
    },
    {
        "id": "book-005",
        "title": "Historia Breve de la Lectura",
        "author": "Clara Nieto",
        "genre": "historia",
        "price": 45000,
        "description": "Recorrido historico por los habitos de lectura y bibliotecas.",
        "available": False,
        "stockByBranch": {"branch-north": 0, "branch-center": 0},
    },
]

SALES = [
    {
        "id": "sale-001",
        "sessionId": "session-demo",
        "originCode": "CHATBOT",
        "status": "MOCK_ONLY",
        "total": 92000,
        "items": [{"bookId": "book-001", "quantity": 1, "unitPrice": 92000}],
        "invoiceId": "FAC-0001",
    }
]

INVOICES = [
    {
        "id": "FAC-0001",
        "saleId": "sale-001",
        "total": 92000,
        "status": "MOCK_ONLY",
        "issuedAt": "2026-07-06T12:00:00Z",
    }
]
