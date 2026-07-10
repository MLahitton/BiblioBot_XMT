from langgraph.graph import END, START, StateGraph

from app.graph.nodes import (
    final_safety_node,
    make_base_validation_node,
    make_confirmation_control_node,
    make_intent_detection_node,
    make_permission_check_node,
    make_response_builder_node,
    make_tool_dispatch_node,
    normalize_input_node,
)
from app.graph.routing import route_after_terminal_check
from app.graph.state import ChatGraphState
from app.services.confirmation_service import ConfirmationService
from app.services.llm_assistant_service import LlmAssistantService
from app.services.permission_service import PermissionService
from app.tools.bibliobot_tools import BiblioBotToolService


def build_chat_graph(
    permission_service: PermissionService,
    confirmation_service: ConfirmationService,
    llm_assistant_service: LlmAssistantService,
    tool_service: BiblioBotToolService,
):
    graph = StateGraph(ChatGraphState)
    graph.add_node("normalize_input", normalize_input_node)
    graph.add_node("base_validation", make_base_validation_node(permission_service))
    graph.add_node("confirmation_control", make_confirmation_control_node(confirmation_service))
    graph.add_node("intent_detection", make_intent_detection_node(permission_service, llm_assistant_service))
    graph.add_node("permission_check", make_permission_check_node(permission_service))
    graph.add_node("tool_dispatch", make_tool_dispatch_node(tool_service, confirmation_service))
    graph.add_node("response_builder", make_response_builder_node(llm_assistant_service))
    graph.add_node("final_safety", final_safety_node)

    graph.add_edge(START, "normalize_input")
    graph.add_edge("normalize_input", "base_validation")
    graph.add_conditional_edges(
        "base_validation",
        route_after_terminal_check,
        {"final_safety": "final_safety", "continue": "confirmation_control"},
    )
    graph.add_conditional_edges(
        "confirmation_control",
        route_after_terminal_check,
        {"final_safety": "final_safety", "continue": "intent_detection"},
    )
    graph.add_edge("intent_detection", "permission_check")
    graph.add_conditional_edges(
        "permission_check",
        route_after_terminal_check,
        {"final_safety": "final_safety", "continue": "tool_dispatch"},
    )
    graph.add_edge("tool_dispatch", "response_builder")
    graph.add_edge("response_builder", "final_safety")
    graph.add_edge("final_safety", END)
    return graph.compile()
