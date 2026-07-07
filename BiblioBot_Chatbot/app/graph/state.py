from typing import Any, TypedDict

from app.schemas.chat_contract import ChatLink, ChatProcessRequest


class ChatGraphState(TypedDict, total=False):
    request: ChatProcessRequest
    session_id: str
    user_id: str
    user_email: str | None
    roles: list[str]
    permissions: list[str]
    source: str
    message: str
    normalized_message: str
    intent: str
    state: str
    response: str
    ui_action: str
    links: list[ChatLink]
    context: dict[str, Any]
    metadata: dict[str, Any]
    requires_confirmation: bool
    action_ref: str | None
    pending_action: dict[str, Any] | None
    tool_result: dict[str, Any] | None
    error: str | None
    is_terminal: bool
    next_step: str | None
