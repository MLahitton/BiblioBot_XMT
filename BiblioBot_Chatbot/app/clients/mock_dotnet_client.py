import copy
import unicodedata

from app.clients.mock_data import BOOKS, BRANCHES, INVOICES, SALES


class MockDotNetClient:
    def search_books(self, query: str | None = None) -> list[dict]:
        if not query:
            return self._copy(BOOKS)

        normalized_query = self._normalize(query)
        query_words = [word for word in normalized_query.split() if len(word) > 1]
        matches = [
            book
            for book in BOOKS
            if self._matches_book_query(book, normalized_query, query_words)
        ]
        return self._copy(matches)

    def get_book_detail(self, book_id: str) -> dict | None:
        normalized_id = self._normalize(book_id)
        for book in BOOKS:
            if normalized_id in {self._normalize(book["id"]), self._normalize(book["title"])}:
                return self._copy(book)
        return None

    def check_stock(self, book_id: str, branch_id: str | None = None) -> dict | None:
        book = self.get_book_detail(book_id)
        if not book:
            return None

        stock_by_branch = book["stockByBranch"]
        if branch_id:
            stock = stock_by_branch.get(branch_id)
            if stock is None:
                return {
                    "bookId": book["id"],
                    "title": book["title"],
                    "branchId": branch_id,
                    "stock": 0,
                    "available": False,
                    "status": "MOCK_ONLY",
                }
            return {
                "bookId": book["id"],
                "title": book["title"],
                "branchId": branch_id,
                "stock": stock,
                "available": stock > 0,
                "status": "MOCK_ONLY",
            }

        total_stock = sum(stock_by_branch.values())
        return {
            "bookId": book["id"],
            "title": book["title"],
            "totalStock": total_stock,
            "stockByBranch": self._copy(stock_by_branch),
            "available": total_stock > 0,
            "status": "MOCK_ONLY",
        }

    def get_cart(self, session_id: str) -> dict:
        return {
            "sessionId": session_id,
            "items": [],
            "status": "MOCK_ONLY",
            "message": "Carrito simulado sin persistencia.",
        }

    def create_sale_draft(
        self,
        session_id: str,
        book_id: str | None = None,
        quantity: int | None = None,
        branch_id: str | None = None,
    ) -> dict:
        return {
            "sessionId": session_id,
            "bookId": book_id,
            "quantity": quantity,
            "branchId": branch_id,
            "status": "PENDING_CONFIRMATION",
            "mockOnly": True,
        }

    def get_invoice(self, invoice_id: str) -> dict | None:
        normalized_id = self._normalize(invoice_id)
        for invoice in INVOICES:
            if self._normalize(invoice["id"]) == normalized_id:
                return self._copy(invoice)
        return None

    def query_sales(self, scope: str = "own") -> list[dict]:
        sales = self._copy(SALES)
        for sale in sales:
            sale["scope"] = scope
        return sales

    def get_low_stock_books(self) -> list[dict]:
        low_stock_books = []
        for book in BOOKS:
            total_stock = sum(book["stockByBranch"].values())
            if 0 < total_stock <= 2:
                low_stock_books.append({**book, "totalStock": total_stock})
        return self._copy(low_stock_books)

    def simulate_inventory_entry(
        self,
        book_id: str,
        quantity: int,
        branch_id: str | None = None,
    ) -> dict:
        return {
            "bookId": book_id,
            "quantity": quantity,
            "branchId": branch_id,
            "status": "PENDING_CONFIRMATION",
            "mockOnly": True,
        }

    def simulate_transfer_request(
        self,
        book_id: str,
        quantity: int,
        from_branch_id: str | None = None,
        to_branch_id: str | None = None,
    ) -> dict:
        return {
            "bookId": book_id,
            "quantity": quantity,
            "fromBranchId": from_branch_id,
            "toBranchId": to_branch_id,
            "status": "PENDING_CONFIRMATION",
            "mockOnly": True,
        }

    def simulate_purchase_request(
        self,
        book_id: str | None = None,
        quantity: int | None = None,
        reason: str | None = None,
    ) -> dict:
        return {
            "bookId": book_id,
            "quantity": quantity,
            "reason": reason,
            "status": "PENDING_CONFIRMATION",
            "mockOnly": True,
        }

    def list_branches(self) -> list[dict]:
        return self._copy(BRANCHES)

    def _normalize(self, value: str) -> str:
        without_accents = "".join(
            char
            for char in unicodedata.normalize("NFD", value.lower())
            if unicodedata.category(char) != "Mn"
        )
        return " ".join(without_accents.split())

    def _matches_book_query(self, book: dict, normalized_query: str, query_words: list[str]) -> bool:
        searchable_text = self._normalize(
            " ".join(
                str(value or "")
                for value in (
                    book["title"],
                    book["author"],
                    book["genre"],
                    book["description"],
                )
            )
        )
        return normalized_query in searchable_text or all(word in searchable_text for word in query_words)

    def _copy(self, value):
        return copy.deepcopy(value)
