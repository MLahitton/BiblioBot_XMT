from fastapi import APIRouter

from app.schemas.chat_contract import ChatProcessRequest, ChatProcessResponse
from app.services import ChatOrchestratorService


router = APIRouter(prefix="/chat", tags=["Chat"])
chat_orchestrator = ChatOrchestratorService()


@router.post("/process", response_model=ChatProcessResponse)
def process_chat_message(request: ChatProcessRequest) -> ChatProcessResponse:
    return chat_orchestrator.process(request)
