import re
import unicodedata

from app.agents import GeminiClient
from app.agents.prompts import BIBLIOBOT_SYSTEM_PROMPT


class LlmAssistantService:
    FORBIDDEN_RESPONSE_PATTERNS = (
        r"\b(confirme|confirmo|confirmada|confirmado)\s+(la\s+)?(venta|compra|factura)\b",
        r"\b(registre|registro|registrada|registrado)\s+(el\s+)?inventario\b",
        r"\b(cree|creo|creada|creado)\s+(la\s+)?solicitud\s+real\b",
        r"\b(autorice|autorizo|valide|valido)\s+(permisos|roles)\b",
        r"\bconsulte\s+(la\s+)?base\s+de\s+datos\b",
        r"\bllame\s+(al\s+)?backend\b",
    )

    def __init__(self, gemini_client: GeminiClient | None = None):
        self.gemini_client = gemini_client or GeminiClient()

    def is_available(self) -> bool:
        return self.gemini_client.is_available()

    def suggest_intent(self, message: str, allowed_intents: list[str]) -> str | None:
        normalized_allowed = [intent.strip() for intent in allowed_intents if intent.strip()]
        if not message.strip() or not normalized_allowed or not self.is_available():
            return None

        prompt = self._build_intent_prompt(message, normalized_allowed)
        generated = self.gemini_client.generate_text(prompt)
        if not generated:
            return None

        candidate = self._sanitize_intent(generated)
        return candidate if candidate in normalized_allowed else None

    def improve_response(self, base_response: str, user_message: str, intent: str) -> str:
        if not base_response.strip() or not self.is_available():
            return base_response

        prompt = self._build_response_prompt(base_response, user_message, intent)
        generated = self.gemini_client.generate_text(prompt)
        if not generated:
            return base_response

        improved = self._sanitize_response(generated)
        return improved if improved else base_response

    def _build_intent_prompt(self, message: str, allowed_intents: list[str]) -> str:
        intents = ", ".join(allowed_intents)
        return (
            f"{BIBLIOBOT_SYSTEM_PROMPT}\n\n"
            "Tarea: clasifica el mensaje del usuario en una unica intencion permitida.\n"
            f"Intenciones permitidas: {intents}.\n"
            "Responde solo con el nombre exacto de una intencion permitida o NONE.\n"
            f"Mensaje: {message}"
        )

    def _build_response_prompt(self, base_response: str, user_message: str, intent: str) -> str:
        return (
            f"{BIBLIOBOT_SYSTEM_PROMPT}\n\n"
            "Tarea: mejora solo la redaccion de la respuesta segura ya construida por el orquestador.\n"
            "No cambies el significado, no agregues acciones, no agregues datos, no prometas ejecuciones reales.\n"
            "Devuelve solo el texto final visible para el usuario.\n"
            f"Intencion detectada: {intent}\n"
            f"Mensaje del usuario: {user_message}\n"
            f"Respuesta base segura: {base_response}"
        )

    def _sanitize_intent(self, value: str) -> str | None:
        first_line = value.strip().splitlines()[0].strip().strip("`'\". ")
        if not first_line or first_line.upper() == "NONE":
            return None
        if not re.fullmatch(r"[a-z_]+", first_line):
            return None
        return first_line

    def _sanitize_response(self, value: str) -> str | None:
        text = " ".join(value.strip().split())
        if not text or len(text) > 600:
            return None

        normalized_text = self._normalize(text)
        if any(re.search(pattern, normalized_text, flags=re.IGNORECASE) for pattern in self.FORBIDDEN_RESPONSE_PATTERNS):
            return None
        return text

    def _normalize(self, value: str) -> str:
        without_accents = "".join(
            char
            for char in unicodedata.normalize("NFD", value.lower())
            if unicodedata.category(char) != "Mn"
        )
        return " ".join(without_accents.split())
