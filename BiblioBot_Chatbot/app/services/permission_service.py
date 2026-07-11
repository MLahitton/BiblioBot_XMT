class PermissionService:
    INTENT_PERMISSIONS = {
        "catalog_search": ["books.read", "books.search"],
        "refine_catalog_filter": ["books.read", "books.search"],
        "list_categories": ["books.read", "books.search"],
        "book_detail": ["books.read"],
        "stock_check": ["books.read", "books.search", "inventory.read"],
        "stock_context_query": ["books.read", "books.search", "inventory.read"],
        "stock_explicit_query": ["books.read", "books.search", "inventory.read"],
        "purchase_intent": ["cart.manage", "sales.create"],
        "checkout_cart": ["sales.create"],
        "confirm_sale": ["sales.confirm"],
        "invoice_query": ["invoices.read_own", "invoices.read_all"],
        "sales_query": ["sales.read_own", "sales.read_all"],
        "inventory_entry": ["inventory.entry"],
        "transfer_request": ["requests.transfer.create"],
        "purchase_request": ["requests.purchase.create"],
        "admin_inventory_adjustment": ["inventory.entry", "inventory.adjust", "inventory.write"],
        "admin_navigation": [
            "admin.users.read",
            "admin.users.write",
            "admin.users.create",
            "inventory.read",
            "inventory.entry",
            "books.write",
            "sales.read_all",
            "sales.confirm",
            "invoices.read_all",
            "reports.read",
            "requests.transfer.create",
            "requests.purchase.create",
        ],
        "page_navigation": ["chat.message"],
        "greeting": ["chat.message"],
        "identity_help": ["chat.message"],
        "general_help": ["chat.message"],
        "out_of_domain": ["chat.message"],
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
