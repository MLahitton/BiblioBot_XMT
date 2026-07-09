from app.graph.state import ChatGraphState


def route_after_terminal_check(state: ChatGraphState) -> str:
    return "final_safety" if state.get("is_terminal") else "continue"
