from typing import Any

from langchain_core.messages import HumanMessage
from langchain_google_genai import ChatGoogleGenerativeAI

from app.core.config import settings


class GeminiClient:
    def __init__(
        self,
        api_key: str | None = None,
        model: str | None = None,
        llm: Any | None = None,
    ):
        self._api_key = api_key if api_key is not None else settings.gemini_api_key
        self._model = model or settings.gemini_model
        self._llm = llm

    def is_available(self) -> bool:
        return bool(self._api_key and self._api_key.strip())

    def generate_text(self, prompt: str) -> str | None:
        if not self.is_available() or not prompt.strip():
            return None

        try:
            llm = self._get_llm()
            result = llm.invoke([HumanMessage(content=prompt)])
            content = getattr(result, "content", None)
            if isinstance(content, str):
                return content.strip() or None
            if isinstance(content, list):
                parts = [
                    item.get("text", "")
                    for item in content
                    if isinstance(item, dict) and isinstance(item.get("text"), str)
                ]
                text = " ".join(part.strip() for part in parts if part.strip())
                return text or None
            return None
        except Exception:
            return None

    def _get_llm(self):
        if self._llm is None:
            self._llm = ChatGoogleGenerativeAI(
                model=self._model,
                api_key=self._api_key,
                temperature=0.2,
                max_tokens=300,
                retries=0,
                request_timeout=10,
            )
        return self._llm
