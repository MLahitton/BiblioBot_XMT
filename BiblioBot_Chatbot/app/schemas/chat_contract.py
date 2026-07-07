from datetime import datetime
from enum import StrEnum
from typing import Any
from uuid import UUID

from pydantic import BaseModel, Field


class ChatState(StrEnum):
    IDLE = "IDLE"
    INTENT_DETECTED = "INTENT_DETECTED"
    ASKING_DETAILS = "ASKING_DETAILS"
    WAITING_CONFIRMATION = "WAITING_CONFIRMATION"
    EXECUTING_ACTION = "EXECUTING_ACTION"
    DONE = "DONE"
    FAILED = "FAILED"
    NEEDS_CLARIFICATION = "NEEDS_CLARIFICATION"


class UiActionType(StrEnum):
    NAVIGATE_TO_CATALOG = "NAVIGATE_TO_CATALOG"
    NAVIGATE_TO_PRODUCT = "NAVIGATE_TO_PRODUCT"
    OPEN_CART = "OPEN_CART"
    SHOW_INVOICE = "SHOW_INVOICE"
    APPLY_FILTERS = "APPLY_FILTERS"
    NONE = "NONE"


class ChatProcessRequest(BaseModel):
    sessionId: str = ""
    message: str = Field(..., min_length=1)
    userId: UUID | None = None
    userEmail: str | None = None
    roles: list[str]
    permissions: list[str]
    source: str = "DOTNET_BACKEND"
    sentAt: datetime | None = None


class ChatLink(BaseModel):
    label: str = Field(..., min_length=1)
    url: str = Field(..., min_length=1)
    type: str | None = None


class ChatContext(BaseModel):
    intent: str | None = None
    requiresConfirmation: bool = False
    actionRef: str | None = None
    invoiceNumber: str | None = None
    saleOrigin: str | None = None
    nextAction: str | None = None
    selectedBookId: str | None = None
    saleId: str | None = None
    selectedBranchId: str | None = None
    metadata: dict[str, Any] = Field(default_factory=dict)


class ChatProcessResponse(BaseModel):
    response: str = Field(..., min_length=1)
    state: ChatState | str
    links: list[ChatLink] = Field(default_factory=list)
    uiAction: UiActionType | str = UiActionType.NONE
    context: ChatContext = Field(default_factory=ChatContext)
