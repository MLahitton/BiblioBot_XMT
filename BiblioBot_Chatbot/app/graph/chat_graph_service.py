from app.clients import DotNetClientProtocol, get_dotnet_client
from app.graph.builder import build_chat_graph
from app.schemas.chat_contract import ChatContext, ChatProcessRequest, ChatProcessResponse, ChatState, UiActionType
from app.services.confirmation_service import ConfirmationService
from app.services.llm_assistant_service import LlmAssistantService
from app.services.permission_service import PermissionService
from app.tools.bibliobot_tools import BiblioBotToolService


class ChatGraphService:
    def __init__(
        self,
        mock_client: DotNetClientProtocol | None = None,
        permission_service: PermissionService | None = None,
        confirmation_service: ConfirmationService | None = None,
        llm_assistant_service: LlmAssistantService | None = None,
        tool_service: BiblioBotToolService | None = None,
        compiled_graph=None,
    ):
        self.mock_client = mock_client or get_dotnet_client()
        self.permission_service = permission_service or PermissionService()
        self.confirmation_service = confirmation_service or ConfirmationService()
        self.llm_assistant_service = llm_assistant_service or LlmAssistantService()
        self.tool_service = tool_service or BiblioBotToolService(
            mock_client=self.mock_client,
            permission_service=self.permission_service,
            confirmation_service=self.confirmation_service,
        )
        self.compiled_graph = compiled_graph or build_chat_graph(
            permission_service=self.permission_service,
            confirmation_service=self.confirmation_service,
            llm_assistant_service=self.llm_assistant_service,
            tool_service=self.tool_service,
        )

    def process(self, request: ChatProcessRequest) -> ChatProcessResponse:
        try:
            final_state = self.compiled_graph.invoke({"request": request})
            return ChatProcessResponse(
                response=final_state.get("response") or "No pude preparar una respuesta segura.",
                state=final_state.get("state", ChatState.FAILED.value),
                links=final_state.get("links", []),
                uiAction=final_state.get("ui_action", UiActionType.NONE.value),
                context=ChatContext(**final_state.get("context", {})),
            )
        except Exception:
            return ChatProcessResponse(
                response="Ocurrio un error controlado al procesar el flujo conversacional. Intenta nuevamente.",
                state=ChatState.FAILED,
                links=[],
                uiAction=UiActionType.NONE,
                context=ChatContext(
                    intent="graph_error",
                    requiresConfirmation=False,
                    saleOrigin="CHATBOT",
                    nextAction="RETRY_OR_CONTACT_SUPPORT",
                    metadata={
                        "sessionId": request.sessionId,
                        "source": request.source,
                        "detectedIntent": "graph_error",
                    },
                ),
            )
