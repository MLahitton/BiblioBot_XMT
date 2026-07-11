from datetime import datetime
from enum import StrEnum
from typing import Any
from uuid import UUID

from pydantic import BaseModel, Field


class ChatContextBook(BaseModel):
    id: str | None = None
    title: str | None = None
    authors: list[str] = Field(default_factory=list)
    categories: list[str] = Field(default_factory=list)
    price: float | int | None = None
    available: bool | None = None


class ChatContextCartSummary(BaseModel):
    itemCount: int | None = None
    totalItems: int | None = None
    subtotal: float | int | None = None


class ChatPageContext(BaseModel):
    route: str | None = None
    pageTitle: str | None = None
    searchQuery: str | None = None
    activeCategory: str | None = None
    activeFilters: dict[str, str] = Field(default_factory=dict)
    visibleBooks: list[ChatContextBook] = Field(default_factory=list)
    selectedBook: ChatContextBook | None = None
    cartSummary: ChatContextCartSummary | None = None


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
    NAVIGATE_TO_ADMIN_USERS = "NAVIGATE_TO_ADMIN_USERS"
    NAVIGATE_TO_ADMIN_CREATE_USER = "NAVIGATE_TO_ADMIN_CREATE_USER"
    NAVIGATE_TO_ADMIN_BOOKS = "NAVIGATE_TO_ADMIN_BOOKS"
    NAVIGATE_TO_ADMIN_CREATE_BOOK = "NAVIGATE_TO_ADMIN_CREATE_BOOK"
    NAVIGATE_TO_ADMIN_INVENTORY = "NAVIGATE_TO_ADMIN_INVENTORY"
    NAVIGATE_TO_INVENTORY_ADJUSTMENT = "NAVIGATE_TO_INVENTORY_ADJUSTMENT"
    NAVIGATE_TO_ADMIN_SALES = "NAVIGATE_TO_ADMIN_SALES"
    NAVIGATE_TO_ADMIN_INVOICES = "NAVIGATE_TO_ADMIN_INVOICES"
    NAVIGATE_TO_ADMIN_REPORTS = "NAVIGATE_TO_ADMIN_REPORTS"
    NAVIGATE_TO_ADMIN_REQUESTS = "NAVIGATE_TO_ADMIN_REQUESTS"
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
    pageContext: ChatPageContext | None = None


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
    pageContext: dict[str, Any] | None = None
    metadata: dict[str, Any] = Field(default_factory=dict)


class ChatProcessResponse(BaseModel):
    response: str = Field(..., min_length=1)
    state: ChatState | str
    links: list[ChatLink] = Field(default_factory=list)
    uiAction: UiActionType | str = UiActionType.NONE
    context: ChatContext = Field(default_factory=ChatContext)
