import hashlib
import unicodedata


class ConfirmationService:
    CONFIRMATION_REQUIRED_INTENTS = {
        "purchase_intent",
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

    def _normalize(self, value: str) -> str:
        without_accents = "".join(
            char
            for char in unicodedata.normalize("NFD", value.lower())
            if unicodedata.category(char) != "Mn"
        )
        return " ".join(without_accents.split())
