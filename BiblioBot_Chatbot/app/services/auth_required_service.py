from uuid import UUID

from app.schemas.chat_contract import ChatLink
from app.services.frontend_action_service import FrontendActionService


class AuthRequiredService:
    AUTHENTICATED_INTENTS = {
        "purchase_intent",
        "checkout_cart",
        "invoice_query",
        "sales_query",
        "inventory_entry",
        "transfer_request",
        "purchase_request",
        "cart_manage",
        "cart_read",
        "create_sale",
        "confirm_sale",
        "inventory_query",
    }
    GUEST_PUBLIC_PERMISSIONS = {"chat.message", "books.read", "books.search"}

    def __init__(self, frontend_action_service: FrontendActionService | None = None):
        self.frontend_action_service = frontend_action_service or FrontendActionService()

    def is_guest(self, roles: list[str], user_id: UUID | str | None) -> bool:
        normalized_roles = {role.strip().upper() for role in roles if role.strip()}
        if "GUEST" in normalized_roles:
            return True
        return user_id is None

    def is_guest_context(self, roles: list[str], user_id: UUID | str | None, permissions: list[str]) -> bool:
        normalized_roles = {role.strip().upper() for role in roles if role.strip()}
        if "GUEST" in normalized_roles:
            return True
        return user_id is None and self.is_limited_guest_permissions(permissions)

    def is_limited_guest_permissions(self, permissions: list[str]) -> bool:
        return set(permissions).issubset(self.GUEST_PUBLIC_PERMISSIONS)

    def requires_authenticated_user(self, intent: str) -> bool:
        return intent in self.AUTHENTICATED_INTENTS

    def build_auth_links(self) -> list[ChatLink]:
        return self.frontend_action_service.build_login_links()
