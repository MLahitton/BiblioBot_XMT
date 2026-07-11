from typing import Any

from pydantic import BaseModel, Field


class ToolExecutionContext(BaseModel):
    session_id: str = Field(..., min_length=1)
    user_id: str | None = None
    roles: list[str] = Field(default_factory=list)
    permissions: list[str] = Field(default_factory=list)
    page_context: Any | None = None
    source: str = "DOTNET_BACKEND"
