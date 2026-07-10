import copy
import hashlib
import unicodedata


class ConfirmationService:
    CONFIRMATION_REQUIRED_INTENTS = {
        "purchase_intent",
        "checkout_cart",
        "confirm_sale",
        "inventory_entry",
        "transfer_request",
        "purchase_request",
        "inventory_adjust",
        "sales_confirm",
    }

    CONFIRMATION_MESSAGES = {
        "si",
        "confirmo",
        "si confirmo",
        "acepto",
        "proceder",
        "confirmar",
    }

    CANCELLATION_MESSAGES = {
        "cancelar",
        "cancela",
        "no",
        "no confirmar",
        "detener",
        "anular",
    }

    def __init__(self):
        self.pending_actions_by_session: dict[str, dict] = {}

    def requires_confirmation(self, intent: str) -> bool:
        return intent in self.CONFIRMATION_REQUIRED_INTENTS

    def is_explicit_confirmation(self, message: str) -> bool:
        return self._normalize(message) in self.CONFIRMATION_MESSAGES

    def is_explicit_cancellation(self, message: str) -> bool:
        return self._normalize(message) in self.CANCELLATION_MESSAGES

    def build_action_ref(self, session_id: str, intent: str, summary: str) -> str:
        raw_value = f"{session_id}:{intent}:{summary}"
        digest = hashlib.sha256(raw_value.encode("utf-8")).hexdigest()[:12]
        return f"mock-action-{digest}"

    def build_pending_action(
        self,
        intent: str,
        action_ref: str,
        summary: str,
        details: dict | None = None,
    ) -> dict:
        return {
            "intent": intent,
            "actionRef": action_ref,
            "summary": summary,
            "details": details or {},
            "status": "PENDING_CONFIRMATION",
            "mockOnly": True,
        }

    def store_pending_action(self, session_id: str, pending_action: dict | None) -> None:
        session_key = self._session_key(session_id)
        if not session_key or not pending_action:
            return

        self.pending_actions_by_session[session_key] = copy.deepcopy(pending_action)

    def get_pending_action(self, session_id: str) -> dict | None:
        session_key = self._session_key(session_id)
        if not session_key:
            return None

        pending_action = self.pending_actions_by_session.get(session_key)
        return copy.deepcopy(pending_action) if pending_action else None

    def consume_pending_action(self, session_id: str) -> dict | None:
        session_key = self._session_key(session_id)
        if not session_key:
            return None

        pending_action = self.pending_actions_by_session.pop(session_key, None)
        return copy.deepcopy(pending_action) if pending_action else None

    def clear_pending_action(self, session_id: str) -> None:
        session_key = self._session_key(session_id)
        if session_key:
            self.pending_actions_by_session.pop(session_key, None)

    def _normalize(self, value: str) -> str:
        without_accents = "".join(
            char
            for char in unicodedata.normalize("NFD", value.lower())
            if unicodedata.category(char) != "Mn"
        )
        return " ".join(without_accents.split())

    def _session_key(self, session_id: str) -> str:
        return " ".join(str(session_id or "").split())
