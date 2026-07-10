from typing import Literal

from pydantic import BaseModel, Field, model_validator


class SearchBooksInput(BaseModel):
    query: str | None = None


class GetBookDetailInput(BaseModel):
    book_id: str = Field(..., min_length=1)


class CheckStockInput(BaseModel):
    book_id: str = Field(..., min_length=1)
    branch_id: str | None = None


class GetCartInput(BaseModel):
    session_id: str = Field(..., min_length=1)


class AddOrUpdateCartItemInput(BaseModel):
    session_id: str = Field(..., min_length=1)
    book_id: str = Field(..., min_length=1)
    quantity: int = Field(..., gt=0)
    branch_id: str | None = None


class CreateSaleFromCartInput(BaseModel):
    session_id: str = Field(..., min_length=1)
    branch_id: str | None = None
    origin_code: str = "CHATBOT"


class ConfirmSaleInput(BaseModel):
    sale_id: str = Field(..., min_length=1)


class GetInvoiceInput(BaseModel):
    invoice_id: str | None = None
    sale_id: str | None = None

    @model_validator(mode="after")
    def require_invoice_or_sale(self):
        if not self.invoice_id and not self.sale_id:
            raise ValueError("invoice_id or sale_id is required")
        return self


class QuerySalesInput(BaseModel):
    scope: Literal["own", "all"] = "own"


class QueryInventoryInput(BaseModel):
    branch_id: str | None = None
    only_low_stock: bool = False


class RegisterInventoryEntryInput(BaseModel):
    book_id: str = Field(..., min_length=1)
    branch_id: str = Field(..., min_length=1)
    quantity: int = Field(..., gt=0)
    reason: str | None = None
    min_stock: int | None = Field(default=None, ge=0)


class CreatePurchaseRequestInput(BaseModel):
    branch_id: str = Field(..., min_length=1)
    book_id: str = Field(..., min_length=1)
    quantity: int = Field(..., gt=0)
    notes: str | None = None


class CreateTransferRequestInput(BaseModel):
    source_branch_id: str = Field(..., min_length=1)
    destination_branch_id: str = Field(..., min_length=1)
    book_id: str = Field(..., min_length=1)
    quantity: int = Field(..., gt=0)
    notes: str | None = None
