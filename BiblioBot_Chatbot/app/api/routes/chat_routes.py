from fastapi import APIRouter

from app.schemas.chat_contract import (
    ChatContext,
    ChatProcessRequest,
    ChatProcessResponse,
    ChatState,
    UiActionType,
)


router = APIRouter(prefix="/chat", tags=["Chat"])


@router.post("/process", response_model=ChatProcessResponse)
def process_chat_message(request: ChatProcessRequest) -> ChatProcessResponse:
    return ChatProcessResponse(
        response=(
            "Contrato recibido desde ASP.NET Core. "
            "El procesamiento conversacional real se implementara en la siguiente fase."
        ),
        state=ChatState.INTENT_DETECTED,
        links=[],
        uiAction=UiActionType.NONE,
        context=ChatContext(
            intent="contract_validation",
            requiresConfirmation=False,
            saleOrigin="CHATBOT",
            nextAction="IMPLEMENT_PHASE_5",
            metadata={
                "sessionId": request.sessionId,
                "source": request.source,
                "roles": request.roles,
                "permissions": request.permissions,
            },
        ),
    )
