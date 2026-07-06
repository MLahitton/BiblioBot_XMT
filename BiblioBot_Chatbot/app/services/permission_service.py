class PermissionService:
    INTENT_PERMISSIONS = {
        "catalog_search": ["books.read", "books.search"],
        "book_detail": ["books.read"],
        "stock_check": ["books.read", "inventory.read"],
        "purchase_intent": ["cart.manage", "sales.create"],
        "invoice_query": ["invoices.read_own", "invoices.read_all"],
        "sales_query": ["sales.read_own", "sales.read_all"],
        "inventory_entry": ["inventory.entry"],
        "transfer_request": ["requests.transfer.create"],
        "purchase_request": ["requests.purchase.create"],
        "general_help": ["chat.message"],
        "unknown": ["chat.message"],
    }

    def has_permission(self, permissions: list[str], permission: str) -> bool:
        return permission in permissions

    def has_any_permission(self, permissions: list[str], required: list[str]) -> bool:
        return any(permission in permissions for permission in required)

    def can_access_intent(self, intent: str, permissions: list[str]) -> bool:
        required = self.required_permissions_for_intent(intent)
        if not required:
            return True
        return self.has_any_permission(permissions, required)

    def required_permissions_for_intent(self, intent: str) -> list[str]:
        return self.INTENT_PERMISSIONS.get(intent, ["chat.message"])
