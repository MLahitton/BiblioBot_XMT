from typing import Protocol


class DotNetClientProtocol(Protocol):
    def search_books(self, query: str | None = None) -> list[dict]: ...

    def get_book_detail(self, book_id: str) -> dict | None: ...

    def check_stock(self, book_id: str, branch_id: str | None = None) -> dict | None: ...

    def get_cart(self, session_id: str) -> dict: ...

    def add_or_update_cart_item(
        self,
        session_id: str,
        book_id: str,
        quantity: int,
        branch_id: str | None = None,
    ) -> dict: ...

    def create_sale_from_cart(
        self,
        session_id: str,
        branch_id: str | None = None,
        origin_code: str = "CHATBOT",
    ) -> dict: ...

    def confirm_sale(self, sale_id: str) -> dict: ...

    def get_invoice(self, invoice_id: str | None = None, sale_id: str | None = None) -> dict | None: ...

    def query_sales(self, scope: str = "own") -> list[dict]: ...

    def query_inventory(self, branch_id: str | None = None) -> list[dict]: ...

    def register_inventory_entry(
        self,
        book_id: str,
        quantity: int,
        branch_id: str,
        reason: str | None = None,
    ) -> dict: ...

    def create_purchase_request(
        self,
        branch_id: str,
        book_id: str,
        quantity: int,
        notes: str | None = None,
    ) -> dict: ...

    def create_transfer_request(
        self,
        source_branch_id: str,
        destination_branch_id: str,
        book_id: str,
        quantity: int,
        notes: str | None = None,
    ) -> dict: ...

    def get_low_stock_books(self) -> list[dict]: ...

    def list_branches(self) -> list[dict]: ...

    def create_sale_draft(
        self,
        session_id: str,
        book_id: str | None = None,
        quantity: int | None = None,
        branch_id: str | None = None,
    ) -> dict: ...

    def simulate_inventory_entry(
        self,
        book_id: str,
        quantity: int,
        branch_id: str | None = None,
    ) -> dict: ...

    def simulate_transfer_request(
        self,
        book_id: str,
        quantity: int,
        from_branch_id: str | None = None,
        to_branch_id: str | None = None,
    ) -> dict: ...

    def simulate_purchase_request(
        self,
        book_id: str | None = None,
        quantity: int | None = None,
        reason: str | None = None,
    ) -> dict: ...
